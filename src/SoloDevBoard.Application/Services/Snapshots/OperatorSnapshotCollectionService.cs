using Microsoft.Extensions.Options;
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

    /// <summary>Initialises the service.</summary>
    public OperatorSnapshotCollectionService(
        ICurrentUserContext userContext,
        IOperatorSnapshotStore store,
        OperatorSnapshotCollectionCoordinator coordinator,
        IOptions<OperatorSnapshotDurabilityOptions> durabilityOptions,
        TimeProvider timeProvider)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
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
            return OperatorSnapshotStartResult.AlreadyInProgress;
        }

        var get = await _store.GetAsync(ownerLogin, cancellationToken).ConfigureAwait(false);
        if (get is OperatorSnapshotGetResult.Found { Document.Run.Status: OperatorSnapshotRunStatus.InProgress }
            && _coordinator.HasLiveTask(sanitised))
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
        if (save is OperatorSnapshotSaveResult.ConcurrencyConflict or OperatorSnapshotSaveResult.UnsafeKey)
        {
            return OperatorSnapshotStartResult.AlreadyInProgress;
        }

        var started = _coordinator.TryBegin(
            sanitised,
            ownerLogin,
            accessToken,
            new OperatorSnapshotConcurrencyToken(inProgress.Revision, token.BlobEtag),
            PersistInProgressAsync,
            ReadForCoordinatorAsync,
            PersistTerminalAsync);

        return started ? OperatorSnapshotStartResult.Started : OperatorSnapshotStartResult.AlreadyInProgress;
    }

    /// <inheritdoc/>
    public async Task<OperatorSnapshotStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var ownerLogin = _userContext.OwnerLogin;
        var get = await _store.GetAsync(ownerLogin, cancellationToken).ConfigureAwait(false);

        return get switch
        {
            OperatorSnapshotGetResult.NotFound => new OperatorSnapshotStatusDto
            {
                Kind = OperatorSnapshotStatusKind.NeverRun,
                DurabilityKind = _durabilityOptions.DurabilityKind,
            },
            OperatorSnapshotGetResult.Found found => MapFound(found.Document, ownerLogin),
            _ => new OperatorSnapshotStatusDto
            {
                Kind = OperatorSnapshotStatusKind.NeverRun,
                DurabilityKind = _durabilityOptions.DurabilityKind,
            },
        };
    }

    private OperatorSnapshotStatusDto MapFound(OperatorSnapshotDocument document, string ownerLogin)
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

        var kind = document.Run.Status switch
        {
            OperatorSnapshotRunStatus.InProgress => OperatorSnapshotStatusKind.InProgress,
            OperatorSnapshotRunStatus.Succeeded => OperatorSnapshotStatusKind.Succeeded,
            OperatorSnapshotRunStatus.Failed => OperatorSnapshotStatusKind.Failed,
            OperatorSnapshotRunStatus.Interrupted => OperatorSnapshotStatusKind.Interrupted,
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

        return new OperatorSnapshotStatusDto
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
    }

    private Task<OperatorSnapshotGetResult> ReadForCoordinatorAsync(string sanitisedLogin, CancellationToken cancellationToken)
        => _store.GetAsync(sanitisedLogin, cancellationToken);

    private async Task<bool> PersistInProgressAsync(
        string sanitisedLogin,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken token,
        CancellationToken cancellationToken)
    {
        var result = await _store.SaveAsync(sanitisedLogin, document, token, cancellationToken).ConfigureAwait(false);
        return result is OperatorSnapshotSaveResult.Succeeded;
    }

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
