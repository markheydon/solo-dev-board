using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Application service for starting and reading operator snapshot collection.</summary>
public sealed class OperatorSnapshotCollectionService : IOperatorSnapshotCollectionService
{
    private readonly ICurrentUserContext _userContext;
    private readonly IOperatorSnapshotStore _store;
    private readonly OperatorSnapshotCollectionCoordinator _coordinator;
    private readonly IDailyOccasionQueueStore _queueStore;
    private readonly OperatorSnapshotScheduleStatusBuilder _scheduleStatusBuilder;
    private readonly OperatorSnapshotDurabilityOptions _durabilityOptions;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initialises the service.</summary>
    public OperatorSnapshotCollectionService(
        ICurrentUserContext userContext,
        IOperatorSnapshotStore store,
        OperatorSnapshotCollectionCoordinator coordinator,
        IDailyOccasionQueueStore queueStore,
        OperatorSnapshotScheduleStatusBuilder scheduleStatusBuilder,
        IOptions<OperatorSnapshotDurabilityOptions> durabilityOptions,
        TimeProvider timeProvider)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
        _scheduleStatusBuilder = scheduleStatusBuilder ?? throw new ArgumentNullException(nameof(scheduleStatusBuilder));
        _durabilityOptions = durabilityOptions?.Value ?? throw new ArgumentNullException(nameof(durabilityOptions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public async Task<OperatorSnapshotStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        var ownerLogin = _userContext.OwnerLogin;
        var sanitised = OperatorLoginSanitiser.Sanitise(ownerLogin);
        if (sanitised is null)
        {
            return OperatorSnapshotStartResult.UnsafeOperatorLogin;
        }

        var get = await _store.GetAsync(ownerLogin, cancellationToken).ConfigureAwait(false);
        if (_coordinator.HasLiveTask(sanitised))
        {
            return OperatorSnapshotStartResult.AlreadyInProgress;
        }

        if (_durabilityOptions.DurabilityKind == OperatorSnapshotDurabilityKind.HostedBlob)
        {
            var queue = await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false);
            var entry = queue?.FindEntry(sanitised);
            if (entry is { State: QueueEntryState.Waiting or QueueEntryState.RetryWaiting })
            {
                return OperatorSnapshotStartResult.Queued;
            }
        }

        var accessToken = _userContext.GetAccessToken();
        var now = _timeProvider.GetUtcNow();
        var token = get switch
        {
            OperatorSnapshotGetResult.Found found => found.Token,
            _ => OperatorSnapshotConcurrencyToken.None,
        };

        var baseDocument = get switch
        {
            OperatorSnapshotGetResult.Found found => found.Document,
            _ => new OperatorSnapshotDocument
            {
                SchemaVersion = OperatorSnapshotDocumentConstants.CurrentSchemaVersion,
                OperatorLogin = sanitised,
                Revision = 0,
                LastSuccessfulCollectionAt = null,
                Repositories = [],
                LaterFacts = [],
                Run = new OperatorSnapshotRun(),
            },
        };

        var inProgress = new OperatorSnapshotDocument
        {
            SchemaVersion = baseDocument.SchemaVersion,
            OperatorLogin = sanitised,
            Revision = baseDocument.Revision + 1,
            LastSuccessfulCollectionAt = baseDocument.LastSuccessfulCollectionAt,
            Repositories = baseDocument.Repositories,
            LaterFacts = baseDocument.LaterFacts,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = now,
                FinishedAt = null,
                RepositoriesRecorded = 0,
                Reason = null,
                Detail = null,
                StartedBy = OperatorSnapshotRunStartedBy.Operator,
            },
        };

        var save = await _store.SaveAsync(ownerLogin, inProgress, token, cancellationToken).ConfigureAwait(false);
        if (save is OperatorSnapshotSaveResult.UnsafeKey)
        {
            return OperatorSnapshotStartResult.UnsafeOperatorLogin;
        }

        if (save is OperatorSnapshotSaveResult.ConcurrencyConflict)
        {
            return OperatorSnapshotStartResult.AlreadyInProgress;
        }

        var started = _coordinator.TryBegin(
            sanitised,
            ownerLogin,
            accessToken,
            new OperatorSnapshotConcurrencyToken(inProgress.Revision, token.BlobEtag),
            ReadForCoordinatorAsync,
            PersistTerminalAsync);

        if (!started)
        {
            await TryRevertStrayInProgressStartAsync(
                    ownerLogin,
                    baseDocument,
                    get is OperatorSnapshotGetResult.Found,
                    cancellationToken)
                .ConfigureAwait(false);
            return OperatorSnapshotStartResult.AlreadyInProgress;
        }

        return OperatorSnapshotStartResult.Started;
    }

    /// <inheritdoc/>
    public async Task<OperatorSnapshotStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var ownerLogin = _userContext.OwnerLogin;
        var get = await _store.GetAsync(ownerLogin, cancellationToken).ConfigureAwait(false);
        var sanitised = OperatorLoginSanitiser.Sanitise(ownerLogin);

        return get switch
        {
            OperatorSnapshotGetResult.NotFound => await BuildStatusDtoAsync(null, sanitised, cancellationToken).ConfigureAwait(false),
            OperatorSnapshotGetResult.Found found => await MapFoundAsync(found.Document, sanitised, cancellationToken).ConfigureAwait(false),
            _ => await BuildStatusDtoAsync(null, sanitised, cancellationToken).ConfigureAwait(false),
        };
    }

    private async Task<OperatorSnapshotStatusDto> MapFoundAsync(
        OperatorSnapshotDocument document,
        string? sanitised,
        CancellationToken cancellationToken)
    {
        var liveOverlay = false;
        var startedAt = default(DateTimeOffset);
        var recorded = 0;
        if (sanitised is not null
            && document.Run.Status == OperatorSnapshotRunStatus.InProgress
            && _coordinator.TryGetLiveProgress(sanitised, out startedAt, out recorded))
        {
            liveOverlay = true;
        }

        var queue = sanitised is not null && _durabilityOptions.DurabilityKind == OperatorSnapshotDurabilityKind.HostedBlob
            ? await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false)
            : null;
        var retryWaiting = sanitised is not null && queue?.FindEntry(sanitised)?.State == QueueEntryState.RetryWaiting;

        var kind = document.Run.Status switch
        {
            OperatorSnapshotRunStatus.InProgress => OperatorSnapshotStatusKind.InProgress,
            OperatorSnapshotRunStatus.Succeeded => OperatorSnapshotStatusKind.Succeeded,
            OperatorSnapshotRunStatus.Failed => OperatorSnapshotStatusKind.Failed,
            OperatorSnapshotRunStatus.Interrupted => retryWaiting && document.Run.StartedBy == OperatorSnapshotRunStartedBy.DailyOccasion
                ? OperatorSnapshotStatusKind.InProgress
                : OperatorSnapshotStatusKind.Interrupted,
            _ => OperatorSnapshotStatusKind.NeverRun,
        };

        if (document.LastSuccessfulCollectionAt is null &&
            document.Run.Status is OperatorSnapshotRunStatus.Failed or OperatorSnapshotRunStatus.Interrupted)
        {
            // Failed or interrupted with no successful snapshot kept.
        }
        else if (document.LastSuccessfulCollectionAt is not null &&
                 document.Run.Status is OperatorSnapshotRunStatus.Failed or OperatorSnapshotRunStatus.Interrupted)
        {
            // Show last success alongside current failed/interrupted run.
        }

        var snapshotCount = document.Repositories.Count;
        var runStartedAt = document.Run.StartedAt;
        var repositoriesRecordedThisRun = document.Run.RepositoriesRecorded;
        if (liveOverlay)
        {
            runStartedAt = startedAt;
            repositoriesRecordedThisRun = recorded;
        }

        var schedule = sanitised is not null
            ? await _scheduleStatusBuilder.BuildAsync(sanitised, document, cancellationToken).ConfigureAwait(false)
            : (OperatorSnapshotScheduleKind.NoneLean, null, false, document.Run.StartedBy);

        if (schedule.QueuedBeforeStart && kind != OperatorSnapshotStatusKind.InProgress)
        {
            kind = document.LastSuccessfulCollectionAt is null && document.Run.Status == OperatorSnapshotRunStatus.InProgress
                ? OperatorSnapshotStatusKind.InProgress
                : kind;
        }

        return new OperatorSnapshotStatusDto
        {
            Kind = kind,
            RunStartedAt = runStartedAt,
            RunFinishedAt = kind == OperatorSnapshotStatusKind.InProgress ? null : document.Run.FinishedAt,
            RepositoriesRecordedThisRun = repositoriesRecordedThisRun,
            LastSuccessfulCollectionAt = document.LastSuccessfulCollectionAt,
            SnapshotRepositoryCount = snapshotCount,
            Reason = document.Run.Reason,
            Detail = document.Run.Detail,
            DurabilityKind = _durabilityOptions.DurabilityKind,
            StartedBy = schedule.StartedBy ?? document.Run.StartedBy,
            QueuedBeforeStart = schedule.QueuedBeforeStart,
            Schedule = schedule.Schedule,
            NextOccasionAt = schedule.NextOccasionAt,
        };
    }

    private async Task<OperatorSnapshotStatusDto> BuildStatusDtoAsync(
        OperatorSnapshotDocument? document,
        string? sanitised,
        CancellationToken cancellationToken)
    {
        if (document is not null && sanitised is not null)
        {
            return await MapFoundAsync(document, sanitised, cancellationToken).ConfigureAwait(false);
        }

        var schedule = sanitised is not null
            ? await _scheduleStatusBuilder.BuildAsync(sanitised, document, cancellationToken).ConfigureAwait(false)
            : (OperatorSnapshotScheduleKind.NoneLean, null, false, (OperatorSnapshotRunStartedBy?)null);

        return new OperatorSnapshotStatusDto
        {
            Kind = OperatorSnapshotStatusKind.NeverRun,
            DurabilityKind = _durabilityOptions.DurabilityKind,
            Schedule = schedule.Schedule,
            NextOccasionAt = schedule.NextOccasionAt,
            QueuedBeforeStart = schedule.QueuedBeforeStart,
            StartedBy = schedule.StartedBy,
        };
    }

    private async Task TryRevertStrayInProgressStartAsync(
        string ownerLogin,
        OperatorSnapshotDocument priorDocument,
        bool hadPriorDocument,
        CancellationToken cancellationToken)
    {
        if (!hadPriorDocument)
        {
            return;
        }

        var current = await _store.GetAsync(ownerLogin, cancellationToken).ConfigureAwait(false);
        if (current is not OperatorSnapshotGetResult.Found found
            || found.Document.Run.Status != OperatorSnapshotRunStatus.InProgress)
        {
            return;
        }

        var restored = new OperatorSnapshotDocument
        {
            SchemaVersion = priorDocument.SchemaVersion,
            OperatorLogin = priorDocument.OperatorLogin,
            LastSuccessfulCollectionAt = priorDocument.LastSuccessfulCollectionAt,
            Repositories = priorDocument.Repositories,
            LaterFacts = priorDocument.LaterFacts,
            Run = new OperatorSnapshotRun
            {
                Status = priorDocument.Run.Status,
                StartedAt = priorDocument.Run.StartedAt,
                FinishedAt = priorDocument.Run.FinishedAt,
                RepositoriesRecorded = priorDocument.Run.RepositoriesRecorded,
                Reason = priorDocument.Run.Reason,
                Detail = priorDocument.Run.Detail,
            },
        };

        await _store.SaveAsync(ownerLogin, restored, found.Token, cancellationToken).ConfigureAwait(false);
    }

    private Task<OperatorSnapshotGetResult> ReadForCoordinatorAsync(string sanitisedLogin, CancellationToken cancellationToken)
        => _store.GetAsync(sanitisedLogin, cancellationToken);

    private async Task<bool> PersistTerminalAsync(
        string sanitisedLogin,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken token,
        CancellationToken cancellationToken)
    {
        var result = await _store.SaveAsync(sanitisedLogin, document, token, cancellationToken);
        return result is OperatorSnapshotSaveResult.Succeeded;
    }
}
