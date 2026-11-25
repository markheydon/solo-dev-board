using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <inheritdoc/>
public sealed class DailySnapshotCollectionPassService(
    IScheduledCollectionEnvironment environment,
    IOperatorSnapshotStore snapshotStore,
    IHostedSignInRecordStore signInRecords,
    IHostedGitHubOAuthTokenExchange tokenExchange,
    IDailySnapshotQueuePublisher queuePublisher,
    IDailyCollectionRunnerLease runnerLease,
    IDailyOccasionService dailyOccasion,
    OperatorSnapshotCollectionCoordinator coordinator,
    HostedCollectionEligibilityEvaluator eligibility,
    AccountScanStatusTransitionService accountScanStatus,
    TimeProvider timeProvider,
    ILogger<DailySnapshotCollectionPassService> logger) : IDailySnapshotCollectionPassService
{
    private static readonly TimeSpan UnmetFillDelay = TimeSpan.FromMinutes(15);

    /// <summary>Delay between polls of the snapshot document while a pass is working.</summary>
    internal TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Polls between liveness checks (about every ten seconds at the default interval).</summary>
    internal int LivenessEveryPolls { get; init; } = 10;

    /// <inheritdoc/>
    public async Task HandleQueueMessageAsync(
        DailySnapshotQueueInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        var message = invocation.Message;
        ArgumentNullException.ThrowIfNull(message);

        if (!environment.IsHostedScheduleEnabled)
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (string.Equals(message.Kind, "unmetFill", StringComparison.Ordinal))
        {
            if (await TryDeferForNotBeforeAsync(message, invocation, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await dailyOccasion.TryFillDailyOccasionAsync(cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var login = message.OperatorLogin;
        await accountScanStatus.EnsureNeverRunIfAbsentAsync(login, cancellationToken).ConfigureAwait(false);
        var status = await accountScanStatus.GetDocumentAsync(login, cancellationToken).ConfigureAwait(false);
        if (status is null)
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (string.Equals(status.Status, AccountScanStatusDocumentConstants.StatusRunning, StringComparison.Ordinal))
        {
            // The owning process died or this is a duplicate: complete, leave running, clear pending for that generation only.
            await accountScanStatus.ClearQueuePendingForGenerationAsync(login, status.ScanGeneration, cancellationToken)
                .ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!string.Equals(status.Status, AccountScanStatusDocumentConstants.StatusScheduled, StringComparison.Ordinal))
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (await TryDeferForNotBeforeAsync(message, invocation, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var generation = status.ScanGeneration;
        var get = await snapshotStore.GetAsync(login, cancellationToken).ConfigureAwait(false);
        var found = get as OperatorSnapshotGetResult.Found;

        var signIn = await signInRecords.GetAsync(login, cancellationToken).ConfigureAwait(false);
        if (signIn is not HostedSignInRecordReadResult.Found record)
        {
            await FailSignInAsync(login, generation, found, cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!eligibility.IsEligibleAtRun(record.Record, timeProvider.GetUtcNow()))
        {
            await accountScanStatus.RecordNotAllowedSuspensionAsync(login, generation, cancellationToken).ConfigureAwait(false);
            if (found is not null && string.Equals(message.Kind, "retry", StringComparison.Ordinal))
            {
                await RestoreRetryWaitAsync(found, cancellationToken).ConfigureAwait(false);
            }

            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (record.Record.CredentialUnusable)
        {
            await FailSignInAsync(login, generation, found, cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await using var runner = await runnerLease.TryAcquireAsync(TimeSpan.FromSeconds(60), cancellationToken).ConfigureAwait(false);
        if (runner is null)
        {
            await queuePublisher.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var win = await accountScanStatus
            .TryWinRunningAsync(login, OperatorSnapshotCollectionTrigger.Daily, fromHandStart: false, cancellationToken)
            .ConfigureAwait(false);
        if (win is not AccountScanRunningWinResult.Won)
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        string accessToken;
        try
        {
            var exchange = await tokenExchange.RefreshAccessTokenAsync(record.Record.RefreshToken, cancellationToken).ConfigureAwait(false);
            accessToken = exchange.AccessToken;
            if (!await TryPersistRefreshTokenAsync(login, record, exchange, cancellationToken).ConfigureAwait(false))
            {
                logger.LogWarning(
                    "Daily pass persisted refreshed token for {Login} only after conflict; continuing with exchanged access token.",
                    login);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Daily pass token exchange failed for {Login}.", login);
            await FailSignInAsync(login, generation, found, cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var now = timeProvider.GetUtcNow();
        var baseDocument = found?.Document ?? NewBaseDocument(login);
        var token = found?.Token ?? OperatorSnapshotConcurrencyToken.None;
        var isRetry = string.Equals(message.Kind, "retry", StringComparison.Ordinal);
        var attempts = isRetry ? (baseDocument.AttemptsUsed ?? 0) + 1 : 1;

        var inProgress = Clone(baseDocument);
        if (!isRetry && IsTerminalRun(inProgress.Run))
        {
            inProgress.RetainedRun = new OperatorSnapshotRetainedRun
            {
                Trigger = inProgress.Trigger,
                Run = CloneRun(inProgress.Run),
            };
        }

        inProgress.Trigger = OperatorSnapshotCollectionTrigger.Daily;
        inProgress.AttemptsUsed = attempts;
        inProgress.PendingRetryNotBeforeUtc = null;
        inProgress.Run = new OperatorSnapshotRun
        {
            Status = OperatorSnapshotRunStatus.InProgress,
            StartedAt = now,
            FinishedAt = null,
            RepositoriesRecorded = 0,
        };

        var saveStart = await snapshotStore.SaveAsync(login, inProgress, token, cancellationToken).ConfigureAwait(false);
        if (saveStart is not OperatorSnapshotSaveResult.Succeeded)
        {
            await accountScanStatus.ScheduleFurtherTryAsync(login, generation, now, cancellationToken).ConfigureAwait(false);
            await queuePublisher.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var startToken = new OperatorSnapshotConcurrencyToken(inProgress.Revision + 1, token.BlobEtag);
        var started = coordinator.TryBegin(
            OperatorLoginSanitiser.Sanitise(login) ?? login,
            login,
            accessToken,
            startToken,
            ReadAsync,
            (loginKey, document, writeToken, ct) => PersistGuardedAsync(loginKey, document, writeToken, generation, ct),
            dailyPass: true);

        if (!started)
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await WaitForTerminalOrRetryAsync(message, generation, runner, cancellationToken).ConfigureAwait(false);
        await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WaitForTerminalOrRetryAsync(
        DailySnapshotQueueMessage message,
        int generation,
        IDailyCollectionRunnerLeaseHandle runner,
        CancellationToken cancellationToken)
    {
        var login = message.OperatorLogin;
        for (var i = 1; ; i++)
        {
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);

            if (i % LivenessEveryPolls == 0)
            {
                if (!await accountScanStatus.IsGenerationCurrentAsync(login, generation, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                await accountScanStatus.TouchLivenessIfDueAsync(login, generation, cancellationToken).ConfigureAwait(false);
                await runner.TryRenewAsync(cancellationToken).ConfigureAwait(false);
            }

            var read = await snapshotStore.GetAsync(login, cancellationToken).ConfigureAwait(false);
            if (read is not OperatorSnapshotGetResult.Found found)
            {
                return;
            }

            if (found.Document.Run.Status is OperatorSnapshotRunStatus.Succeeded)
            {
                if (!await accountScanStatus.IsGenerationCurrentAsync(login, generation, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                found.Document.AttemptsUsed = null;
                found.Document.RetainedRun = null;
                found.Document.PendingRetryNotBeforeUtc = null;
                found.Document.Trigger = OperatorSnapshotCollectionTrigger.Daily;
                await snapshotStore.SaveAsync(login, found.Document, found.Token, cancellationToken).ConfigureAwait(false);
                await accountScanStatus.RecordSuccessAsync(login, generation, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (found.Document.Trigger == OperatorSnapshotCollectionTrigger.Daily
                && found.Document.Run.Status == OperatorSnapshotRunStatus.InProgress
                && !coordinator.HasLiveTask(login)
                && found.Document.Run.Reason is OperatorSnapshotRunReason.RateLimit or OperatorSnapshotRunReason.Unexpected
                && (found.Document.AttemptsUsed ?? 1) < 4)
            {
                if (!await accountScanStatus.IsGenerationCurrentAsync(login, generation, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                var notBeforeUtc = found.Document.PendingRetryNotBeforeUtc ?? timeProvider.GetUtcNow();
                var notBeforeIso = DailySnapshotRetryNotBefore.ToIso8601Utc(notBeforeUtc);
                found.Document.PendingRetryNotBeforeUtc = null;
                RestoreRunForRetryWait(found.Document);
                await snapshotStore.SaveAsync(login, found.Document, found.Token, cancellationToken).ConfigureAwait(false);
                await accountScanStatus.ScheduleFurtherTryAsync(login, generation, notBeforeUtc, cancellationToken)
                    .ConfigureAwait(false);
                await queuePublisher.EnqueueAsync(
                    new DailySnapshotQueueMessage
                    {
                        OccasionDate = message.OccasionDate,
                        OperatorLogin = login,
                        Kind = "retry",
                        NotBefore = notBeforeIso,
                    },
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            if (found.Document.Run.Status is OperatorSnapshotRunStatus.Failed)
            {
                if (!await accountScanStatus.IsGenerationCurrentAsync(login, generation, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                found.Document.RetainedRun = null;
                found.Document.AttemptsUsed = null;
                found.Document.PendingRetryNotBeforeUtc = null;
                await snapshotStore.SaveAsync(login, found.Document, found.Token, cancellationToken).ConfigureAwait(false);
                var comment = AccountScanResultComment.ForFailure(found.Document.Run.Reason, found.Document.Run.Detail);
                if (found.Document.Run.Reason == OperatorSnapshotRunReason.SignInOrToken)
                {
                    await accountScanStatus.RecordTokenFailureAsync(login, generation, comment, cancellationToken).ConfigureAwait(false);
                    await MarkCredentialUnusableAsync(login, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await accountScanStatus.RecordExhaustedFailureAsync(login, generation, comment, cancellationToken)
                        .ConfigureAwait(false);
                    await EnqueueUnmetFillAsync(cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            if (found.Document.Run.Status is OperatorSnapshotRunStatus.Interrupted)
            {
                // A stopped host leaves the status as running; readers tell stalled after five minutes.
                return;
            }
        }
    }

    private static void RestoreRunForRetryWait(OperatorSnapshotDocument document)
    {
        if (document.RetainedRun is { } retained)
        {
            document.Trigger = retained.Trigger;
            document.Run = CloneRun(retained.Run);
        }
        else
        {
            document.Run = new OperatorSnapshotRun();
        }
    }

    private static OperatorSnapshotDocument NewBaseDocument(string login) => new()
    {
        SchemaVersion = OperatorSnapshotDocumentConstants.CurrentSchemaVersion,
        OperatorLogin = OperatorLoginSanitiser.Sanitise(login) ?? login,
        Revision = 0,
        Repositories = [],
        LaterFacts = [],
        Run = new OperatorSnapshotRun(),
    };

    private static bool IsTerminalRun(OperatorSnapshotRun run) =>
        run.Status is OperatorSnapshotRunStatus.Succeeded
            or OperatorSnapshotRunStatus.Failed
            or OperatorSnapshotRunStatus.Interrupted;

    private async Task EnqueueUnmetFillAsync(CancellationToken cancellationToken)
    {
        var notBefore = timeProvider.GetUtcNow().Add(UnmetFillDelay);
        await queuePublisher.EnqueueAsync(
            new DailySnapshotQueueMessage
            {
                Kind = "unmetFill",
                NotBefore = DailySnapshotRetryNotBefore.ToIso8601Utc(notBefore),
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TryDeferForNotBeforeAsync(
        DailySnapshotQueueMessage message,
        DailySnapshotQueueInvocation invocation,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(message.Kind, "retry", StringComparison.Ordinal)
            && !string.Equals(message.Kind, "unmetFill", StringComparison.Ordinal))
        {
            return false;
        }

        if (!DailySnapshotRetryNotBefore.TryParseIso8601Utc(message.NotBefore, out var notBefore))
        {
            return false;
        }

        if (notBefore <= timeProvider.GetUtcNow())
        {
            return false;
        }

        await queuePublisher.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task RestoreRetryWaitAsync(OperatorSnapshotGetResult.Found found, CancellationToken cancellationToken)
    {
        var document = Clone(found.Document);
        if (document.RetainedRun is { } retained)
        {
            document.Trigger = retained.Trigger;
            document.Run = CloneRun(retained.Run);
        }
        else
        {
            document.Trigger = null;
            document.Run = new OperatorSnapshotRun();
        }

        document.RetainedRun = null;
        document.AttemptsUsed = null;
        document.PendingRetryNotBeforeUtc = null;
        await snapshotStore.SaveAsync(found.Document.OperatorLogin, document, found.Token, cancellationToken).ConfigureAwait(false);
    }

    private async Task FailSignInAsync(
        string login,
        int generation,
        OperatorSnapshotGetResult.Found? found,
        CancellationToken cancellationToken)
    {
        if (found is not null)
        {
            var document = Clone(found.Document);
            document.RetainedRun = null;
            document.AttemptsUsed = null;
            document.PendingRetryNotBeforeUtc = null;
            document.Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Failed,
                StartedAt = found.Document.Run.StartedAt,
                FinishedAt = timeProvider.GetUtcNow(),
                RepositoriesRecorded = found.Document.Run.RepositoriesRecorded,
                Reason = OperatorSnapshotRunReason.SignInOrToken,
            };
            await snapshotStore.SaveAsync(found.Document.OperatorLogin, document, found.Token, cancellationToken).ConfigureAwait(false);
        }

        await accountScanStatus
            .RecordTokenFailureAsync(
                login,
                generation,
                AccountScanResultComment.ForFailure(OperatorSnapshotRunReason.SignInOrToken, null),
                cancellationToken)
            .ConfigureAwait(false);
        await MarkCredentialUnusableAsync(login, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TryPersistRefreshTokenAsync(
        string login,
        HostedSignInRecordReadResult.Found record,
        HostedGitHubRefreshTokenExchangeResult exchange,
        CancellationToken cancellationToken)
    {
        record.Record.RefreshToken = exchange.RefreshToken;
        record.Record.RefreshTokenExpiresAt = exchange.RefreshTokenExpiresAtUtc;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var upsert = await signInRecords.UpsertAsync(record.Record, record.Etag, cancellationToken).ConfigureAwait(false);
            if (upsert is HostedSignInRecordWriteResult.Succeeded)
            {
                return true;
            }

            if (upsert is not HostedSignInRecordWriteResult.ConcurrencyConflict)
            {
                return false;
            }

            var reread = await signInRecords.GetAsync(login, cancellationToken).ConfigureAwait(false);
            if (reread is not HostedSignInRecordReadResult.Found retry)
            {
                return false;
            }

            record = retry;
            record.Record.RefreshToken = exchange.RefreshToken;
            record.Record.RefreshTokenExpiresAt = exchange.RefreshTokenExpiresAtUtc;
            record.Record.CredentialUnusable = false;
        }

        return false;
    }

    private async Task MarkCredentialUnusableAsync(string login, CancellationToken cancellationToken)
    {
        var read = await signInRecords.GetAsync(login, cancellationToken).ConfigureAwait(false);
        if (read is not HostedSignInRecordReadResult.Found found)
        {
            return;
        }

        found.Record.CredentialUnusable = true;
        var upsert = await signInRecords.UpsertAsync(found.Record, found.Etag, cancellationToken).ConfigureAwait(false);
        if (upsert is HostedSignInRecordWriteResult.ConcurrencyConflict)
        {
            var reread = await signInRecords.GetAsync(login, cancellationToken).ConfigureAwait(false);
            if (reread is HostedSignInRecordReadResult.Found retry)
            {
                retry.Record.CredentialUnusable = true;
                await signInRecords.UpsertAsync(retry.Record, retry.Etag, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Task<OperatorSnapshotGetResult> ReadAsync(string login, CancellationToken cancellationToken)
        => snapshotStore.GetAsync(login, cancellationToken);

    private async Task<bool> PersistGuardedAsync(
        string login,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken token,
        int generation,
        CancellationToken cancellationToken)
    {
        // An earlier pass must not overwrite the snapshot once a newer scan has won.
        if (!await accountScanStatus.IsGenerationCurrentAsync(login, generation, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        var result = await snapshotStore.SaveAsync(login, document, token, cancellationToken).ConfigureAwait(false);
        return result is OperatorSnapshotSaveResult.Succeeded;
    }

    private static OperatorSnapshotDocument Clone(OperatorSnapshotDocument source) => new()
    {
        SchemaVersion = source.SchemaVersion,
        OperatorLogin = source.OperatorLogin,
        Revision = source.Revision,
        LastSuccessfulCollectionAt = source.LastSuccessfulCollectionAt,
        Repositories = source.Repositories,
        LaterFacts = source.LaterFacts,
        Trigger = source.Trigger,
        AttemptsUsed = source.AttemptsUsed,
        RetainedRun = source.RetainedRun is null
            ? null
            : new OperatorSnapshotRetainedRun
            {
                Trigger = source.RetainedRun.Trigger,
                Run = CloneRun(source.RetainedRun.Run),
            },
        PendingRetryNotBeforeUtc = source.PendingRetryNotBeforeUtc,
        Run = CloneRun(source.Run),
    };

    private static OperatorSnapshotRun CloneRun(OperatorSnapshotRun source) => new()
    {
        Status = source.Status,
        StartedAt = source.StartedAt,
        FinishedAt = source.FinishedAt,
        RepositoriesRecorded = source.RepositoriesRecorded,
        Reason = source.Reason,
        Detail = source.Detail,
    };
}
