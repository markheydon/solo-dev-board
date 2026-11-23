using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <inheritdoc/>
public sealed class DailyCollectionScheduler(
    IDailyOccasionQueueStore queueStore,
    IOperatorSignInRecordStore signInStore,
    IOperatorSnapshotStore snapshotStore,
    ISignInRecordRefreshTokenProtector protector,
    IHostedRefreshTokenExchanger tokenExchanger,
    OperatorSnapshotCollectionCoordinator coordinator,
    OperatorSnapshotEligibility eligibility,
    IOptions<OperatorSnapshotDurabilityOptions> durabilityOptions,
    TimeProvider timeProvider,
    ILogger<DailyCollectionScheduler> logger) : IDailyCollectionScheduler
{
    private readonly IDailyOccasionQueueStore _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
    private readonly IOperatorSignInRecordStore _signInStore = signInStore ?? throw new ArgumentNullException(nameof(signInStore));
    private readonly IOperatorSnapshotStore _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
    private readonly ISignInRecordRefreshTokenProtector _protector = protector ?? throw new ArgumentNullException(nameof(protector));
    private readonly IHostedRefreshTokenExchanger _tokenExchanger = tokenExchanger ?? throw new ArgumentNullException(nameof(tokenExchanger));
    private readonly OperatorSnapshotCollectionCoordinator _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    private readonly OperatorSnapshotEligibility _eligibility = eligibility ?? throw new ArgumentNullException(nameof(eligibility));
    private readonly OperatorSnapshotDurabilityOptions _durabilityOptions = durabilityOptions?.Value ?? throw new ArgumentNullException(nameof(durabilityOptions));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<DailyCollectionScheduler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task TickAsync(string? replicaLogin, CancellationToken cancellationToken = default)
    {
        if (_durabilityOptions.DurabilityKind != OperatorSnapshotDurabilityKind.HostedBlob)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        var queue = await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false);
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        if (queue is not null && queue.OccasionDate < today && queue.HasWork())
        {
            await DrainQueueAsync(queue, replicaLogin, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (ShouldFillToday(now, today, queue))
        {
            await FillTodayAsync(today, now, cancellationToken).ConfigureAwait(false);
            queue = await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false);
        }

        if (queue is not null && queue.HasWork())
        {
            await DrainQueueAsync(queue, replicaLogin, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task InterruptActivePassAsync(string sanitisedLogin, CancellationToken cancellationToken = default)
    {
        var queue = await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false);
        if (queue is null || !string.Equals(queue.ActiveLogin, sanitisedLogin, StringComparison.Ordinal))
        {
            return;
        }

        queue.Entries.RemoveAll(entry => string.Equals(entry.Login, sanitisedLogin, StringComparison.Ordinal));
        queue.ActiveLogin = null;
        queue.LeaseUntil = null;
        if (queue.BlobEtag is not null)
        {
            await _queueStore.TryUpdateAsync(queue, queue.BlobEtag, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool ShouldFillToday(DateTimeOffset now, DateOnly today, DailyOccasionQueue? queue)
    {
        if (queue is not null && queue.OccasionDate == today)
        {
            return false;
        }

        var occasionStart = OperatorSnapshotEligibility.OccasionInstant(today);
        var graceEnd = occasionStart.AddMinutes(IDailyCollectionScheduler.Constants.FillGraceMinuteUtc);

        if (now < occasionStart)
        {
            return false;
        }

        return now <= graceEnd;
    }

    private async Task FillTodayAsync(DateOnly today, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var occasionInstant = OperatorSnapshotEligibility.OccasionInstant(today);
        var records = await _signInStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var entries = new List<DailyOccasionQueueEntry>();

        foreach (var record in records)
        {
            OperatorSnapshotDocument? snapshot = null;
            var get = await _snapshotStore.GetAsync(record.OperatorLogin, cancellationToken).ConfigureAwait(false);
            if (get is OperatorSnapshotGetResult.Found found)
            {
                snapshot = found.Document;
            }

            if (!_eligibility.IsEligibleAtFill(record, occasionInstant, queue: null, snapshot))
            {
                continue;
            }

            if (entries.Any(entry => string.Equals(entry.Login, record.OperatorLogin, StringComparison.Ordinal)))
            {
                continue;
            }

            entries.Add(new DailyOccasionQueueEntry
            {
                Login = record.OperatorLogin,
                State = QueueEntryState.Waiting,
                AttemptsCompleted = 0,
            });
        }

        var document = new DailyOccasionQueue
        {
            OccasionDate = today,
            FilledAt = now,
            Entries = entries,
        };

        await _queueStore.TryCreateAsync(document, cancellationToken).ConfigureAwait(false);
    }

    private async Task DrainQueueAsync(DailyOccasionQueue queue, string? replicaLogin, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        if (!string.IsNullOrWhiteSpace(queue.ActiveLogin))
        {
            if (queue.LeaseUntil is { } leaseUntil && leaseUntil <= now)
            {
                await InterruptExpiredLeaseAsync(queue, cancellationToken).ConfigureAwait(false);
            }
            else if (string.Equals(queue.ActiveLogin, replicaLogin, StringComparison.Ordinal))
            {
                await RenewLeaseAsync(queue, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (queue.Entries.Count == 0)
        {
            return;
        }

        var head = queue.Entries[0];
        var leaseUntilTime = now + IDailyCollectionScheduler.Constants.LeaseDuration;
        queue.ActiveLogin = head.Login;
        queue.LeaseUntil = leaseUntilTime;

        if (queue.BlobEtag is null || !await _queueStore.TryUpdateAsync(queue, queue.BlobEtag, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await RunPassAsync(head, queue, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunPassAsync(DailyOccasionQueueEntry entry, DailyOccasionQueue queue, CancellationToken cancellationToken)
    {
        var login = entry.Login;
        var record = await _signInStore.GetByLoginAsync(login, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
            return;
        }

        string accessToken;
        try
        {
            var refreshToken = _protector.Unprotect(record.ProtectedRefreshToken);
            var exchange = await _tokenExchanger.ExchangeAsync(refreshToken, cancellationToken).ConfigureAwait(false);
            if (!exchange.Succeeded)
            {
                await HandleSignInOrTokenFailureAsync(queue, login, record, cancellationToken).ConfigureAwait(false);
                return;
            }

            accessToken = exchange.AccessToken;
            if (record.BlobEtag is not null)
            {
                var protectedRefresh = _protector.Protect(exchange.RefreshToken);
                await _signInStore.TryUpdateProtectedRefreshTokenAsync(
                        login,
                        protectedRefresh,
                        exchange.RefreshTokenExpiresAtUtc,
                        record.BlobEtag,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Daily collection token exchange failed for {Login}.", login);
            await HandleSignInOrTokenFailureAsync(queue, login, record, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (entry.State == QueueEntryState.Waiting)
        {
            var started = await TryMarkInProgressAsync(login, cancellationToken).ConfigureAwait(false);
            if (!started)
            {
                await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
                return;
            }
        }

        var get = await _snapshotStore.GetAsync(login, cancellationToken).ConfigureAwait(false);
        var token = get is OperatorSnapshotGetResult.Found found
            ? found.Token
            : OperatorSnapshotConcurrencyToken.None;

        var began = _coordinator.TryBegin(
            login,
            login,
            accessToken,
            token,
            ReadForCoordinatorAsync,
            PersistTerminalAsync);

        if (!began)
        {
            await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
            return;
        }

        while (_coordinator.HasLiveTask(login))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
        }

        await FinalisePassAsync(entry, queue, login, cancellationToken).ConfigureAwait(false);
    }

    private async Task FinalisePassAsync(
        DailyOccasionQueueEntry entry,
        DailyOccasionQueue queue,
        string login,
        CancellationToken cancellationToken)
    {
        var get = await _snapshotStore.GetAsync(login, cancellationToken).ConfigureAwait(false);
        if (get is not OperatorSnapshotGetResult.Found found)
        {
            await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
            return;
        }

        var run = found.Document.Run;
        if (run.Status == OperatorSnapshotRunStatus.Succeeded)
        {
            await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (run.Status == OperatorSnapshotRunStatus.Failed)
        {
            if (run.Reason is OperatorSnapshotRunReason.SignInOrToken)
            {
                await HandleSignInOrTokenFailureAsync(queue, login, await _signInStore.GetByLoginAsync(login, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                return;
            }

            if (run.Reason is OperatorSnapshotRunReason.RateLimit or OperatorSnapshotRunReason.Unexpected)
            {
                entry.AttemptsCompleted++;
                if (entry.AttemptsCompleted > IDailyCollectionScheduler.Constants.MaxFurtherTries)
                {
                    await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await RequeueAtBackAsync(queue, login, QueueEntryState.RetryWaiting, entry.AttemptsCompleted, cancellationToken).ConfigureAwait(false);
                }

                return;
            }
        }

        await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleSignInOrTokenFailureAsync(
        DailyOccasionQueue queue,
        string login,
        OperatorSignInRecord? record,
        CancellationToken cancellationToken)
    {
        if (record?.BlobEtag is not null)
        {
            await _signInStore.TryUpdateScheduleHoldAsync(login, scheduleHold: true, record.BlobEtag, cancellationToken).ConfigureAwait(false);
        }

        await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TryMarkInProgressAsync(string login, CancellationToken cancellationToken)
    {
        var get = await _snapshotStore.GetAsync(login, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();
        var baseDocument = get switch
        {
            OperatorSnapshotGetResult.Found found => found.Document,
            _ => new OperatorSnapshotDocument
            {
                SchemaVersion = OperatorSnapshotDocumentConstants.CurrentSchemaVersion,
                OperatorLogin = login,
                Revision = 0,
                Repositories = [],
                LaterFacts = [],
                Run = new OperatorSnapshotRun(),
            },
        };

        var token = get switch
        {
            OperatorSnapshotGetResult.Found found => found.Token,
            _ => OperatorSnapshotConcurrencyToken.None,
        };

        var inProgress = new OperatorSnapshotDocument
        {
            SchemaVersion = baseDocument.SchemaVersion,
            OperatorLogin = login,
            Revision = baseDocument.Revision + 1,
            LastSuccessfulCollectionAt = baseDocument.LastSuccessfulCollectionAt,
            Repositories = baseDocument.Repositories,
            LaterFacts = baseDocument.LaterFacts,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = now,
                StartedBy = OperatorSnapshotRunStartedBy.DailyOccasion,
                RepositoriesRecorded = baseDocument.Run.Status == OperatorSnapshotRunStatus.InProgress
                    ? baseDocument.Run.RepositoriesRecorded
                    : 0,
            },
        };

        var save = await _snapshotStore.SaveAsync(login, inProgress, token, cancellationToken).ConfigureAwait(false);
        return save is OperatorSnapshotSaveResult.Succeeded;
    }

    private async Task RequeueAtBackAsync(
        DailyOccasionQueue queue,
        string login,
        QueueEntryState state,
        int attemptsCompleted,
        CancellationToken cancellationToken)
    {
        queue = await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false) ?? queue;
        queue.Entries.RemoveAll(entry => string.Equals(entry.Login, login, StringComparison.Ordinal));
        queue.Entries.Add(new DailyOccasionQueueEntry
        {
            Login = login,
            State = state,
            AttemptsCompleted = attemptsCompleted,
        });
        queue.ActiveLogin = null;
        queue.LeaseUntil = null;
        if (queue.BlobEtag is not null)
        {
            await _queueStore.TryUpdateAsync(queue, queue.BlobEtag, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RemoveEntryAndClearLeaseAsync(DailyOccasionQueue queue, string login, CancellationToken cancellationToken)
    {
        queue = await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false) ?? queue;
        queue.Entries.RemoveAll(entry => string.Equals(entry.Login, login, StringComparison.Ordinal));
        if (string.Equals(queue.ActiveLogin, login, StringComparison.Ordinal))
        {
            queue.ActiveLogin = null;
            queue.LeaseUntil = null;
        }

        if (queue.BlobEtag is not null)
        {
            await _queueStore.TryUpdateAsync(queue, queue.BlobEtag, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RenewLeaseAsync(DailyOccasionQueue queue, CancellationToken cancellationToken)
    {
        queue.LeaseUntil = _timeProvider.GetUtcNow() + IDailyCollectionScheduler.Constants.LeaseDuration;
        if (queue.BlobEtag is not null)
        {
            await _queueStore.TryUpdateAsync(queue, queue.BlobEtag, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InterruptExpiredLeaseAsync(DailyOccasionQueue queue, CancellationToken cancellationToken)
    {
        var login = queue.ActiveLogin;
        if (string.IsNullOrWhiteSpace(login))
        {
            return;
        }

        await RemoveEntryAndClearLeaseAsync(queue, login, cancellationToken).ConfigureAwait(false);
    }

    private Task<OperatorSnapshotGetResult> ReadForCoordinatorAsync(string sanitisedLogin, CancellationToken cancellationToken)
        => _snapshotStore.GetAsync(sanitisedLogin, cancellationToken);

    private async Task<bool> PersistTerminalAsync(
        string sanitisedLogin,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken token,
        CancellationToken cancellationToken)
    {
        var save = await _snapshotStore.SaveAsync(sanitisedLogin, document, token, cancellationToken).ConfigureAwait(false);
        return save is OperatorSnapshotSaveResult.Succeeded;
    }
}
