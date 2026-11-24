using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Application service for starting and reading operator snapshot collection.</summary>
public sealed class OperatorSnapshotCollectionService : IOperatorSnapshotCollectionService
{
    private readonly ICurrentUserContext _userContext;
    private readonly IOperatorSnapshotStore _store;
    private readonly OperatorSnapshotCollectionCoordinator _coordinator;
    private readonly OperatorSnapshotDurabilityOptions _durabilityOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IOperatorSnapshotScheduleFactsService _scheduleFacts;
    private readonly IHostedSignInRecordStore _signInRecords;

    /// <summary>Initialises the service.</summary>
    public OperatorSnapshotCollectionService(
        ICurrentUserContext userContext,
        IOperatorSnapshotStore store,
        OperatorSnapshotCollectionCoordinator coordinator,
        IOptions<OperatorSnapshotDurabilityOptions> durabilityOptions,
        TimeProvider timeProvider,
        IOperatorSnapshotScheduleFactsService scheduleFacts,
        IHostedSignInRecordStore signInRecords)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _durabilityOptions = durabilityOptions?.Value ?? throw new ArgumentNullException(nameof(durabilityOptions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _scheduleFacts = scheduleFacts ?? throw new ArgumentNullException(nameof(scheduleFacts));
        _signInRecords = signInRecords ?? throw new ArgumentNullException(nameof(signInRecords));
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

        if (get is OperatorSnapshotGetResult.Found foundBeforeStart
            && (!string.IsNullOrEmpty(foundBeforeStart.Document.QueuedOccasion)
                || foundBeforeStart.Document.Run.Status == OperatorSnapshotRunStatus.InProgress))
        {
            return OperatorSnapshotStartResult.AlreadyInProgress;
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
            Trigger = OperatorSnapshotCollectionTrigger.Operator,
            QueuedOccasion = null,
            PassExecuting = false,
            PassLeaseUntil = null,
            AttemptsUsed = null,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.InProgress,
                StartedAt = now,
                FinishedAt = null,
                RepositoriesRecorded = 0,
                Reason = null,
                Detail = null,
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

        return get switch
        {
            OperatorSnapshotGetResult.NotFound => await MapScheduleAsync(
                new OperatorSnapshotStatusDto
                {
                    Kind = OperatorSnapshotStatusKind.NeverRun,
                    DurabilityKind = _durabilityOptions.DurabilityKind,
                },
                null,
                ownerLogin,
                cancellationToken).ConfigureAwait(false),
            OperatorSnapshotGetResult.Found found => await MapFoundAsync(found.Document, ownerLogin, cancellationToken).ConfigureAwait(false),
            _ => await MapScheduleAsync(
                new OperatorSnapshotStatusDto
                {
                    Kind = OperatorSnapshotStatusKind.NeverRun,
                    DurabilityKind = _durabilityOptions.DurabilityKind,
                },
                null,
                ownerLogin,
                cancellationToken).ConfigureAwait(false),
        };
    }

    private async Task<OperatorSnapshotStatusDto> MapFoundAsync(
        OperatorSnapshotDocument document,
        string ownerLogin,
        CancellationToken cancellationToken)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(ownerLogin);
        var liveOverlay = false;
        var startedAt = default(DateTimeOffset);
        var recorded = 0;
        if (sanitised is not null
            && document.Run.Status == OperatorSnapshotRunStatus.InProgress
            && _coordinator.TryGetLiveProgress(sanitised, out startedAt, out recorded))
        {
            liveOverlay = true;
        }

        var kind = ResolveStatusKind(document);

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

        var dto = new OperatorSnapshotStatusDto
        {
            Kind = kind,
            RunStartedAt = runStartedAt,
            RunFinishedAt = document.Run.FinishedAt,
            RepositoriesRecordedThisRun = repositoriesRecordedThisRun,
            LastSuccessfulCollectionAt = document.LastSuccessfulCollectionAt,
            SnapshotRepositoryCount = snapshotCount,
            Reason = document.Run.Reason,
            Detail = document.Run.Detail,
            DurabilityKind = _durabilityOptions.DurabilityKind,
        };

        return await MapScheduleAsync(dto, document, ownerLogin, cancellationToken).ConfigureAwait(false);
    }

    private async Task<OperatorSnapshotStatusDto> MapScheduleAsync(
        OperatorSnapshotStatusDto dto,
        OperatorSnapshotDocument? document,
        string ownerLogin,
        CancellationToken cancellationToken)
    {
        HostedSignInRecordForSchedule? signIn = null;
        if (_durabilityOptions.DurabilityKind == OperatorSnapshotDurabilityKind.HostedBlob)
        {
            var read = await _signInRecords.GetAsync(ownerLogin, cancellationToken).ConfigureAwait(false);
            if (read is HostedSignInRecordReadResult.Found found)
            {
                signIn = new HostedSignInRecordForSchedule(
                    found.Record.OperatorLogin,
                    found.Record.CompletedAt,
                    found.Record.CredentialUnusable,
                    found.Record.OrganisationLogins);
            }
        }

        var utcNow = _timeProvider.GetUtcNow();
        var facts = _scheduleFacts.Compute(
            document ?? new OperatorSnapshotDocument(),
            signIn,
            utcNow);

        var nextOnFollowingDay = facts.NextOccasionAtUtc is { } next
            && next.UtcDateTime.Date > utcNow.UtcDateTime.Date;

        return new OperatorSnapshotStatusDto
        {
            Kind = dto.Kind,
            RunStartedAt = dto.RunStartedAt,
            RunFinishedAt = dto.RunFinishedAt,
            RepositoriesRecordedThisRun = dto.RepositoriesRecordedThisRun,
            LastSuccessfulCollectionAt = dto.LastSuccessfulCollectionAt,
            SnapshotRepositoryCount = dto.SnapshotRepositoryCount,
            Reason = dto.Reason,
            Detail = dto.Detail,
            DurabilityKind = dto.DurabilityKind,
            Trigger = facts.Trigger ?? document?.Trigger,
            SchedulePosture = facts.SchedulePosture,
            QueuedForOccasion = facts.QueuedForOccasion,
            NextOccasionAtUtc = facts.NextOccasionAtUtc,
            NextOccasionOnFollowingUtcDay = nextOnFollowingDay,
            NoScheduleReason = facts.NoScheduleReason,
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
            Revision = found.Document.Revision,
            LastSuccessfulCollectionAt = priorDocument.LastSuccessfulCollectionAt,
            Repositories = priorDocument.Repositories,
            LaterFacts = priorDocument.LaterFacts,
            Trigger = priorDocument.Trigger,
            QueuedOccasion = priorDocument.QueuedOccasion,
            PassExecuting = priorDocument.PassExecuting,
            PassLeaseUntil = priorDocument.PassLeaseUntil,
            AttemptsUsed = priorDocument.AttemptsUsed,
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

    private static OperatorSnapshotStatusKind ResolveStatusKind(OperatorSnapshotDocument document)
    {
        if (!string.IsNullOrEmpty(document.QueuedOccasion) && !document.PassExecuting)
        {
            return document.Run.Status switch
            {
                OperatorSnapshotRunStatus.Succeeded => OperatorSnapshotStatusKind.Succeeded,
                OperatorSnapshotRunStatus.Failed when document.LastSuccessfulCollectionAt is not null
                    || document.Run.FinishedAt is not null
                    => OperatorSnapshotStatusKind.Failed,
                OperatorSnapshotRunStatus.Interrupted => OperatorSnapshotStatusKind.Interrupted,
                _ => OperatorSnapshotStatusKind.NeverRun,
            };
        }

        return document.Run.Status switch
        {
            OperatorSnapshotRunStatus.InProgress => OperatorSnapshotStatusKind.InProgress,
            OperatorSnapshotRunStatus.Succeeded => OperatorSnapshotStatusKind.Succeeded,
            OperatorSnapshotRunStatus.Failed => OperatorSnapshotStatusKind.Failed,
            OperatorSnapshotRunStatus.Interrupted => OperatorSnapshotStatusKind.Interrupted,
            _ => OperatorSnapshotStatusKind.NeverRun,
        };
    }
}
