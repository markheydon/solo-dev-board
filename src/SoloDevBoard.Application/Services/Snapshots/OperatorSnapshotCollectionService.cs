using Microsoft.Extensions.Logging;
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
    private readonly IScheduledCollectionEnvironment _environment;
    private readonly AccountScanStatusTransitionService _accountScanStatus;
    private readonly ILogger<OperatorSnapshotCollectionService> _logger;

    /// <summary>Delay between polls while a hand-started hosted scan is working.</summary>
    internal TimeSpan MonitorPollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Polls between liveness marks (about every ten seconds at the default interval).</summary>
    internal int LivenessEveryPolls { get; init; } = 10;

    /// <summary>Initialises the service.</summary>
    public OperatorSnapshotCollectionService(
        ICurrentUserContext userContext,
        IOperatorSnapshotStore store,
        OperatorSnapshotCollectionCoordinator coordinator,
        IOptions<OperatorSnapshotDurabilityOptions> durabilityOptions,
        TimeProvider timeProvider,
        IOperatorSnapshotScheduleFactsService scheduleFacts,
        IHostedSignInRecordStore signInRecords,
        IScheduledCollectionEnvironment environment,
        AccountScanStatusTransitionService accountScanStatus,
        ILogger<OperatorSnapshotCollectionService> logger)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _durabilityOptions = durabilityOptions?.Value ?? throw new ArgumentNullException(nameof(durabilityOptions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _scheduleFacts = scheduleFacts ?? throw new ArgumentNullException(nameof(scheduleFacts));
        _signInRecords = signInRecords ?? throw new ArgumentNullException(nameof(signInRecords));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _accountScanStatus = accountScanStatus ?? throw new ArgumentNullException(nameof(accountScanStatus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        var hosted = _environment.IsHostedScheduleEnabled;
        var generation = 0;
        AccountScanStatusDocument? previousStatus = null;
        if (hosted)
        {
            // The status record is the authority in hosted sign-in: refuse while told scheduled or running.
            await _accountScanStatus.EnsureNeverRunIfAbsentAsync(ownerLogin, cancellationToken).ConfigureAwait(false);
            previousStatus = await _accountScanStatus.GetDocumentAsync(ownerLogin, cancellationToken).ConfigureAwait(false);
            var win = await _accountScanStatus
                .TryWinRunningAsync(ownerLogin, OperatorSnapshotCollectionTrigger.Operator, fromHandStart: true, cancellationToken)
                .ConfigureAwait(false);
            if (win is not AccountScanRunningWinResult.Won won)
            {
                return OperatorSnapshotStartResult.AlreadyInProgress;
            }

            generation = won.Generation;
        }
        else if (get is OperatorSnapshotGetResult.Found foundBeforeStart
                 && foundBeforeStart.Document.Run.Status == OperatorSnapshotRunStatus.InProgress)
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
            await RestoreStatusAsync(ownerLogin, generation, previousStatus, cancellationToken).ConfigureAwait(false);
            return OperatorSnapshotStartResult.UnsafeOperatorLogin;
        }

        if (save is OperatorSnapshotSaveResult.ConcurrencyConflict)
        {
            await RestoreStatusAsync(ownerLogin, generation, previousStatus, cancellationToken).ConfigureAwait(false);
            return OperatorSnapshotStartResult.AlreadyInProgress;
        }

        var started = _coordinator.TryBegin(
            sanitised,
            ownerLogin,
            accessToken,
            new OperatorSnapshotConcurrencyToken(inProgress.Revision, token.BlobEtag),
            ReadForCoordinatorAsync,
            (login, document, writeToken, ct) => PersistTerminalAsync(login, document, writeToken, hosted ? generation : null, ct));

        if (!started)
        {
            await RestoreStatusAsync(ownerLogin, generation, previousStatus, cancellationToken).ConfigureAwait(false);
            await TryRevertStrayInProgressStartAsync(
                    ownerLogin,
                    baseDocument,
                    get is OperatorSnapshotGetResult.Found,
                    cancellationToken)
                .ConfigureAwait(false);
            return OperatorSnapshotStartResult.AlreadyInProgress;
        }

        if (hosted)
        {
            _ = MonitorHandStartedScanAsync(sanitised, ownerLogin, generation);
        }

        return OperatorSnapshotStartResult.Started;
    }

    private async Task RestoreStatusAsync(
        string ownerLogin,
        int generation,
        AccountScanStatusDocument? previousStatus,
        CancellationToken cancellationToken)
    {
        if (previousStatus is null)
        {
            return;
        }

        await _accountScanStatus.RestoreAfterFailedStartAsync(ownerLogin, generation, previousStatus, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes liveness marks while a hand-started hosted walk is live, then records the outcome on the status record.
    /// A stopped site leaves <c>running</c> in place; readers tell <c>stalled</c> after five minutes.
    /// </summary>
    private async Task MonitorHandStartedScanAsync(string sanitisedLogin, string ownerLogin, int generation)
    {
        try
        {
            for (var poll = 1; ; poll++)
            {
                await Task.Delay(MonitorPollInterval, _timeProvider).ConfigureAwait(false);

                if (_coordinator.HasLiveTask(sanitisedLogin))
                {
                    if (poll % LivenessEveryPolls == 0)
                    {
                        await _accountScanStatus.TouchLivenessIfDueAsync(ownerLogin, generation).ConfigureAwait(false);
                    }

                    continue;
                }

                var read = await _store.GetAsync(ownerLogin).ConfigureAwait(false);
                if (read is not OperatorSnapshotGetResult.Found found)
                {
                    return;
                }

                var run = found.Document.Run;
                if (run.Status == OperatorSnapshotRunStatus.Succeeded)
                {
                    await _accountScanStatus.RecordSuccessAsync(ownerLogin, generation).ConfigureAwait(false);
                }
                else if (run.Status == OperatorSnapshotRunStatus.Failed)
                {
                    var comment = AccountScanResultComment.ForFailure(run.Reason, run.Detail);
                    if (run.Reason == OperatorSnapshotRunReason.SignInOrToken)
                    {
                        await _accountScanStatus.RecordTokenFailureAsync(ownerLogin, generation, comment).ConfigureAwait(false);
                    }
                    else
                    {
                        await _accountScanStatus.RecordExhaustedFailureAsync(ownerLogin, generation, comment).ConfigureAwait(false);
                    }
                }

                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hand-started scan monitor stopped for {Login}.", sanitisedLogin);
        }
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
        AccountScanStatusReport? scanReport = null;
        if (_environment.IsHostedScheduleEnabled)
        {
            var read = await _signInRecords.GetAsync(ownerLogin, cancellationToken).ConfigureAwait(false);
            if (read is HostedSignInRecordReadResult.Found found)
            {
                signIn = new HostedSignInRecordForSchedule(
                    found.Record.OperatorLogin,
                    found.Record.CompletedAt,
                    found.Record.CredentialUnusable,
                    found.Record.HasWorkerRefreshToken,
                    found.Record.OrganisationLogins);
            }

            scanReport = await _accountScanStatus
                .GetReportAsync(ownerLogin, hostedSignInRecordExists: signIn is not null, cancellationToken)
                .ConfigureAwait(false);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var facts = _scheduleFacts.Compute(
            document ?? new OperatorSnapshotDocument(),
            signIn,
            scanReport?.Reported,
            utcNow);

        var nextOnFollowingDay = facts.NextOccasionAtUtc is { } next
            && next.UtcDateTime.Date > utcNow.UtcDateTime.Date;

        // Hosted: the trigger is only a fact while a scan is running or has finished.
        var trigger = scanReport?.Reported is null
            ? facts.Trigger ?? document?.Trigger
            : scanReport.Reported is ReportedAccountScanStatus.Running
                or ReportedAccountScanStatus.Stalled
                or ReportedAccountScanStatus.Finished
                ? document?.Trigger
                : null;

        var runtime = new OperatorSnapshotStatusDto
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
            Trigger = trigger,
            SchedulePosture = facts.SchedulePosture,
            AccountScanStatus = scanReport?.StoredStatus,
            ReportedScanStatus = scanReport?.Reported,
            LastScanAt = scanReport?.LastScanAt,
            LastScanResult = scanReport?.LastResult,
            ResultComment = scanReport?.ResultComment,
            ScanSuspensionReason = scanReport?.SuspensionReason ?? AccountScanSuspensionReason.None,
            NextOccasionAtUtc = facts.NextOccasionAtUtc,
            NextOccasionOnFollowingUtcDay = nextOnFollowingDay,
            NoScheduleReason = facts.NoScheduleReason,
            NextAskIncludesAccount = facts.NextAskIncludesAccount,
            WorkerTokenRefreshOffered = facts.WorkerTokenRefreshOffered,
        };

        var (statusSentence, scheduleSentence) = AccountScanStatusSentenceFormatter.Format(
            facts.SchedulePosture,
            facts.NoScheduleReason,
            scanReport?.Reported,
            scanReport,
            runtime,
            signIn,
            facts.NextAskIncludesAccount,
            facts.WorkerTokenRefreshOffered);

        return new OperatorSnapshotStatusDto
        {
            Kind = runtime.Kind,
            RunStartedAt = runtime.RunStartedAt,
            RunFinishedAt = runtime.RunFinishedAt,
            RepositoriesRecordedThisRun = runtime.RepositoriesRecordedThisRun,
            LastSuccessfulCollectionAt = runtime.LastSuccessfulCollectionAt,
            SnapshotRepositoryCount = runtime.SnapshotRepositoryCount,
            Reason = runtime.Reason,
            Detail = runtime.Detail,
            DurabilityKind = runtime.DurabilityKind,
            Trigger = runtime.Trigger,
            SchedulePosture = runtime.SchedulePosture,
            AccountScanStatus = runtime.AccountScanStatus,
            ReportedScanStatus = runtime.ReportedScanStatus,
            LastScanAt = runtime.LastScanAt,
            LastScanResult = runtime.LastScanResult,
            ResultComment = runtime.ResultComment,
            ScanSuspensionReason = runtime.ScanSuspensionReason,
            NextOccasionAtUtc = runtime.NextOccasionAtUtc,
            NextOccasionOnFollowingUtcDay = runtime.NextOccasionOnFollowingUtcDay,
            NoScheduleReason = runtime.NoScheduleReason,
            NextAskIncludesAccount = runtime.NextAskIncludesAccount,
            WorkerTokenRefreshOffered = runtime.WorkerTokenRefreshOffered,
            StatusSentence = statusSentence,
            ScheduleSentence = scheduleSentence,
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
        int? hostedGeneration,
        CancellationToken cancellationToken)
    {
        // A scan from an earlier generation must not overwrite the snapshot once a newer scan has won.
        if (hostedGeneration is { } generation
            && !await _accountScanStatus.IsGenerationCurrentAsync(sanitisedLogin, generation, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        var result = await _store.SaveAsync(sanitisedLogin, document, token, cancellationToken).ConfigureAwait(false);
        return result is OperatorSnapshotSaveResult.Succeeded;
    }

    private static OperatorSnapshotStatusKind ResolveStatusKind(OperatorSnapshotDocument document) =>
        document.Run.Status switch
        {
            OperatorSnapshotRunStatus.InProgress => OperatorSnapshotStatusKind.InProgress,
            OperatorSnapshotRunStatus.Succeeded => OperatorSnapshotStatusKind.Succeeded,
            OperatorSnapshotRunStatus.Failed => OperatorSnapshotStatusKind.Failed,
            OperatorSnapshotRunStatus.Interrupted => OperatorSnapshotStatusKind.Interrupted,
            _ => OperatorSnapshotStatusKind.NeverRun,
        };
}
