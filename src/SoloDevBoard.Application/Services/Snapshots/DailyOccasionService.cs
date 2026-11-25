using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <inheritdoc />
public sealed class DailyOccasionService(
    IScheduledCollectionEnvironment environment,
    TimeProvider timeProvider,
    ICollectionOccasionMarkerStore markerStore,
    IDailyCollectionRunnerLease runnerLease,
    IDailySnapshotQueuePublisher queuePublisher,
    IHostedSignInRecordStore signInRecordStore,
    IAccountScanStatusStore accountScanStatusStore,
    AccountScanStatusTransitionService accountScanStatus,
    HostedCollectionEligibilityEvaluator eligibility,
    ILogger<DailyOccasionService> logger) : IDailyOccasionService
{
    private readonly IScheduledCollectionEnvironment _environment =
        environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ICollectionOccasionMarkerStore _markerStore =
        markerStore ?? throw new ArgumentNullException(nameof(markerStore));
    private readonly IDailyCollectionRunnerLease _runnerLease =
        runnerLease ?? throw new ArgumentNullException(nameof(runnerLease));
    private readonly IDailySnapshotQueuePublisher _queuePublisher =
        queuePublisher ?? throw new ArgumentNullException(nameof(queuePublisher));
    private readonly IHostedSignInRecordStore _signInRecordStore =
        signInRecordStore ?? throw new ArgumentNullException(nameof(signInRecordStore));
    private readonly IAccountScanStatusStore _accountScanStatusStore =
        accountScanStatusStore ?? throw new ArgumentNullException(nameof(accountScanStatusStore));
    private readonly AccountScanStatusTransitionService _accountScanStatus =
        accountScanStatus ?? throw new ArgumentNullException(nameof(accountScanStatus));
    private readonly HostedCollectionEligibilityEvaluator _eligibility =
        eligibility ?? throw new ArgumentNullException(nameof(eligibility));
    private readonly ILogger<DailyOccasionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<DailyOccasionFillResult> TryFillDailyOccasionAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsHostedScheduleEnabled)
        {
            return DailyOccasionFillResult.Skipped;
        }

        var utcNow = _timeProvider.GetUtcNow();
        var occasionDate = utcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var markerRead = await _markerStore.GetAsync(occasionDate, cancellationToken).ConfigureAwait(false);
        if (markerRead is OccasionMarkerReadResult.Unreadable)
        {
            _logger.LogWarning(
                "Occasion marker for {OccasionDate} is unreadable; skipping fill until storage is repaired.",
                occasionDate);
            return DailyOccasionFillResult.AlreadyFilled;
        }

        await using var runner = await _runnerLease.TryAcquireAsync(TimeSpan.FromSeconds(60), cancellationToken)
            .ConfigureAwait(false);
        if (runner is null)
        {
            return DailyOccasionFillResult.LostRace;
        }

        markerRead = await _markerStore.GetAsync(occasionDate, cancellationToken).ConfigureAwait(false);
        if (markerRead is OccasionMarkerReadResult.Unreadable)
        {
            _logger.LogWarning("Occasion marker for {OccasionDate} is unreadable after lease; skipping fill.", occasionDate);
            return DailyOccasionFillResult.AlreadyFilled;
        }

        var priorOperators = markerRead is OccasionMarkerReadResult.Found foundMarker
            ? foundMarker.Marker.Operators
            : [];
        var occasionInstant = utcNow;
        var signInRecords = await _signInRecordStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var enqueued = new List<string>(signInRecords.Count);

        foreach (var record in signInRecords)
        {
            await runner.TryRenewAsync(cancellationToken).ConfigureAwait(false);

            await _accountScanStatus.EnsureNeverRunIfAbsentAsync(record.OperatorLogin, cancellationToken).ConfigureAwait(false);
            await _accountScanStatus.RestoreOrphanScheduledAsync(record.OperatorLogin, cancellationToken).ConfigureAwait(false);

            var report = await _accountScanStatus.GetReportAsync(record.OperatorLogin, hostedSignInRecordExists: true, cancellationToken)
                .ConfigureAwait(false);

            if (!_eligibility.IsEligibleAtFill(record, report.Reported, occasionInstant))
            {
                continue;
            }

            var win = await _accountScanStatus.TryWinScheduleFromFillAsync(record.OperatorLogin, cancellationToken)
                .ConfigureAwait(false);
            if (win is not AccountScanScheduleWinResult.Won won)
            {
                continue;
            }

            try
            {
                await _queuePublisher.EnqueueAsync(
                        new DailySnapshotQueueMessage
                        {
                            OccasionDate = occasionDate,
                            OperatorLogin = record.OperatorLogin,
                            Kind = "initial",
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enqueue daily collection for {Login}.", record.OperatorLogin);
                continue;
            }

            await _accountScanStatus.MarkQueueMessagePendingAsync(record.OperatorLogin, won.Generation, cancellationToken)
                .ConfigureAwait(false);
            enqueued.Add(record.OperatorLogin);
        }

        var unionOperators = priorOperators
            .Concat(enqueued)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var marker = new OccasionMarker
        {
            OccasionDate = occasionDate,
            FilledAt = utcNow,
            Operators = unionOperators,
        };

        var persist = await PersistMarkerAsync(marker, markerRead, cancellationToken).ConfigureAwait(false);
        return persist switch
        {
            MarkerPersistOutcome.LostRace => DailyOccasionFillResult.LostRace,
            MarkerPersistOutcome.Unreadable => DailyOccasionFillResult.AlreadyFilled,
            _ => DailyOccasionFillResult.Filled,
        };
    }

    private async Task<MarkerPersistOutcome> PersistMarkerAsync(
        OccasionMarker marker,
        OccasionMarkerReadResult markerRead,
        CancellationToken cancellationToken)
    {
        if (markerRead is OccasionMarkerReadResult.NotFound)
        {
            var created = await _markerStore.TryCreateAsync(marker, cancellationToken).ConfigureAwait(false);
            if (created is OccasionMarkerWriteResult.Created)
            {
                return MarkerPersistOutcome.Ok;
            }

            var reread = await _markerStore.GetAsync(marker.OccasionDate, cancellationToken).ConfigureAwait(false);
            return await PersistMarkerAsync(marker, reread, cancellationToken).ConfigureAwait(false);
        }

        if (markerRead is not OccasionMarkerReadResult.Found found)
        {
            return MarkerPersistOutcome.Unreadable;
        }

        var replace = await _markerStore.TryReplaceAsync(marker, found.Etag, cancellationToken).ConfigureAwait(false);
        return replace switch
        {
            OccasionMarkerWriteResult.Replaced => MarkerPersistOutcome.Ok,
            OccasionMarkerWriteResult.ConcurrencyConflict => MarkerPersistOutcome.LostRace,
            _ => MarkerPersistOutcome.Ok,
        };
    }

    private enum MarkerPersistOutcome
    {
        Ok,
        LostRace,
        Unreadable,
    }

    /// <summary>Enqueues missing <c>initial</c> messages after a worker restart.</summary>
    public async Task RepairMissingInitialMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsHostedScheduleEnabled)
        {
            return;
        }

        var utcNow = _timeProvider.GetUtcNow();
        var occasionDate = utcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var signInRecords = await _signInRecordStore.ListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var record in signInRecords)
        {
            await TryRepairQueueMessageForLoginAsync(occasionDate, record.OperatorLogin, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task TryRepairQueueMessageForLoginAsync(
        string occasionDate,
        string login,
        CancellationToken cancellationToken)
    {
        var read = await _accountScanStatusStore.GetAsync(login, cancellationToken).ConfigureAwait(false);
        if (read is not AccountScanStatusReadResult.Found found)
        {
            return;
        }

        if (!string.Equals(found.Document.Status, AccountScanStatusDocumentConstants.StatusScheduled, StringComparison.Ordinal)
            || !found.Document.QueueMessagePending)
        {
            return;
        }

        if (found.Document.QueueVisibleAfter is { } visibleAfter && visibleAfter > _timeProvider.GetUtcNow())
        {
            return;
        }

        try
        {
            if (await _queuePublisher.HasVisibleMessageAsync(login, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await _queuePublisher.EnqueueAsync(
                    new DailySnapshotQueueMessage
                    {
                        OccasionDate = occasionDate,
                        OperatorLogin = login,
                        Kind = "initial",
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to repair queue message for {Login}.", login);
        }
    }
}
