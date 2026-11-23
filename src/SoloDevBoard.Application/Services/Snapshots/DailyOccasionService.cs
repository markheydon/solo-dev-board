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
    IOperatorSnapshotStore snapshotStore,
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
    private readonly IOperatorSnapshotStore _snapshotStore =
        snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
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
        if (!IsInFillWindow(utcNow))
        {
            return DailyOccasionFillResult.Skipped;
        }

        var occasionDate = utcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var existing = await _markerStore.GetAsync(occasionDate, cancellationToken).ConfigureAwait(false);
        if (existing is OccasionMarkerReadResult.Found)
        {
            return DailyOccasionFillResult.AlreadyFilled;
        }

        await using var runner = await _runnerLease.TryAcquireAsync(TimeSpan.FromSeconds(45), cancellationToken)
            .ConfigureAwait(false);
        if (runner is null)
        {
            return DailyOccasionFillResult.LostRace;
        }

        existing = await _markerStore.GetAsync(occasionDate, cancellationToken).ConfigureAwait(false);
        if (existing is OccasionMarkerReadResult.Found)
        {
            return DailyOccasionFillResult.AlreadyFilled;
        }

        var occasionInstant = GetOccasionInstant(utcNow);
        var signInRecords = await _signInRecordStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var enqueued = new List<string>(signInRecords.Count);

        foreach (var record in signInRecords)
        {
            var snapshotGet = await _snapshotStore.GetAsync(record.OperatorLogin, cancellationToken).ConfigureAwait(false);
            OperatorSnapshotDocument? snapshot = snapshotGet is OperatorSnapshotGetResult.Found found
                ? found.Document
                : null;

            if (!_eligibility.IsEligibleAtFill(record, snapshot, occasionInstant))
            {
                continue;
            }

            var saveOk = await TrySetQueuedOccasionAsync(
                    record.OperatorLogin,
                    occasionDate,
                    snapshot,
                    snapshotGet,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!saveOk)
            {
                continue;
            }

            await _queuePublisher.EnqueueAsync(
                    new DailySnapshotQueueMessage
                    {
                        OccasionDate = occasionDate,
                        OperatorLogin = record.OperatorLogin,
                        Kind = "initial",
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            enqueued.Add(record.OperatorLogin);
        }

        var marker = new OccasionMarker
        {
            OccasionDate = occasionDate,
            FilledAt = utcNow,
            Operators = enqueued,
        };

        var created = await _markerStore.TryCreateAsync(marker, cancellationToken).ConfigureAwait(false);
        return created switch
        {
            OccasionMarkerWriteResult.AlreadyExists => DailyOccasionFillResult.AlreadyFilled,
            _ => DailyOccasionFillResult.Filled,
        };
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
        var existing = await _markerStore.GetAsync(occasionDate, cancellationToken).ConfigureAwait(false);
        if (existing is not OccasionMarkerReadResult.Found found)
        {
            return;
        }

        foreach (var login in found.Marker.Operators)
        {
            var snapshotGet = await _snapshotStore.GetAsync(login, cancellationToken).ConfigureAwait(false);
            if (snapshotGet is not OperatorSnapshotGetResult.Found doc
                || !string.Equals(doc.Document.QueuedOccasion, occasionDate, StringComparison.Ordinal)
                || doc.Document.PassExecuting
                || doc.Document.Run.Status == OperatorSnapshotRunStatus.InProgress)
            {
                continue;
            }

            try
            {
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

    private async Task<bool> TrySetQueuedOccasionAsync(
        string operatorLogin,
        string occasionDate,
        OperatorSnapshotDocument? snapshot,
        OperatorSnapshotGetResult snapshotGet,
        CancellationToken cancellationToken)
    {
        var token = snapshotGet is OperatorSnapshotGetResult.Found foundGet
            ? foundGet.Token
            : OperatorSnapshotConcurrencyToken.None;

        var document = snapshot ?? new OperatorSnapshotDocument
        {
            OperatorLogin = OperatorLoginSanitiser.Sanitise(operatorLogin) ?? operatorLogin,
            SchemaVersion = OperatorSnapshotDocumentConstants.CurrentSchemaVersion,
            Repositories = [],
            LaterFacts = [],
            Run = new OperatorSnapshotRun(),
        };

        document.QueuedOccasion = occasionDate;
        document.PassExecuting = false;
        document.PassLeaseUntil = null;
        document.Trigger = OperatorSnapshotCollectionTrigger.Daily;

        var save = await _snapshotStore.SaveAsync(operatorLogin, document, token, cancellationToken).ConfigureAwait(false);
        return save is OperatorSnapshotSaveResult.Succeeded;
    }

    private static bool IsInFillWindow(DateTimeOffset utcNow)
    {
        var occasion = GetTodayOccasionAt(utcNow);
        return utcNow >= occasion && utcNow <= occasion.AddMinutes(15);
    }

    private static DateTimeOffset GetOccasionInstant(DateTimeOffset utcNow)
    {
        var occasion = GetTodayOccasionAt(utcNow);
        return utcNow > occasion ? utcNow : occasion;
    }

    private static DateTimeOffset GetTodayOccasionAt(DateTimeOffset utcNow)
    {
        var date = utcNow.UtcDateTime.Date;
        return new DateTimeOffset(date.Year, date.Month, date.Day, 5, 0, 0, TimeSpan.Zero);
    }
}
