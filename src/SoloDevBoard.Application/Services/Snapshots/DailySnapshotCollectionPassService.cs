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
    OperatorSnapshotCollectionCoordinator coordinator,
    TimeProvider timeProvider,
    ILogger<DailySnapshotCollectionPassService> logger) : IDailySnapshotCollectionPassService
{
    private static readonly TimeSpan VisibilityRenew = TimeSpan.FromSeconds(10);

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

        var get = await snapshotStore.GetAsync(message.OperatorLogin, cancellationToken).ConfigureAwait(false);
        if (get is not OperatorSnapshotGetResult.Found found)
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (found.Document.PassExecuting)
        {
            var nowUtc = timeProvider.GetUtcNow();
            if (found.Document.PassLeaseUntil is { } activeLease && activeLease > nowUtc)
            {
                await queuePublisher.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
                await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await WriteScheduledInterruptionAsync(found, cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (found.Document.Run.Status == OperatorSnapshotRunStatus.InProgress
            && !(string.Equals(message.Kind, "retry", StringComparison.Ordinal)
                && IsDailyRetryWaitState(found.Document)))
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await using var runner = await runnerLease.TryAcquireAsync(TimeSpan.FromMinutes(30), cancellationToken).ConfigureAwait(false);
        if (runner is null)
        {
            await queuePublisher.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var signIn = await signInRecords.GetAsync(message.OperatorLogin, cancellationToken).ConfigureAwait(false);
        if (signIn is not HostedSignInRecordReadResult.Found record)
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        string accessToken;
        try
        {
            var exchange = await tokenExchange.RefreshAccessTokenAsync(record.Record.RefreshToken, cancellationToken).ConfigureAwait(false);
            accessToken = exchange.AccessToken;
            record.Record.RefreshToken = exchange.RefreshToken;
            record.Record.RefreshTokenExpiresAt = exchange.RefreshTokenExpiresAtUtc;
            var upsert = await signInRecords.UpsertAsync(record.Record, record.Etag, cancellationToken).ConfigureAwait(false);
            if (upsert is HostedSignInRecordWriteResult.ConcurrencyConflict)
            {
                logger.LogWarning("Daily pass could not persist refreshed token for {Login}.", message.OperatorLogin);
                await WriteSignInFailureAsync(found, cancellationToken).ConfigureAwait(false);
                await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Daily pass token exchange failed for {Login}.", message.OperatorLogin);
            await WriteSignInFailureAsync(found, cancellationToken).ConfigureAwait(false);
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var now = timeProvider.GetUtcNow();
        var attempts = found.Document.AttemptsUsed ?? 0;
        attempts = string.Equals(message.Kind, "retry", StringComparison.Ordinal) ? attempts + 1 : 1;

        var inProgress = Clone(found.Document);
        inProgress.Trigger = OperatorSnapshotCollectionTrigger.Daily;
        inProgress.QueuedOccasion = null;
        inProgress.PassExecuting = true;
        inProgress.PassLeaseUntil = now.Add(VisibilityRenew);
        inProgress.AttemptsUsed = attempts;
        inProgress.Run = new OperatorSnapshotRun
        {
            Status = OperatorSnapshotRunStatus.InProgress,
            StartedAt = inProgress.Run.Status == OperatorSnapshotRunStatus.InProgress
                ? inProgress.Run.StartedAt
                : now,
            FinishedAt = null,
            RepositoriesRecorded = inProgress.Run.RepositoriesRecorded,
        };

        var saveStart = await snapshotStore.SaveAsync(message.OperatorLogin, inProgress, found.Token, cancellationToken).ConfigureAwait(false);
        if (saveStart is not OperatorSnapshotSaveResult.Succeeded)
        {
            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var startToken = new OperatorSnapshotConcurrencyToken(inProgress.Revision + 1, found.Token.BlobEtag);
        var started = coordinator.TryBegin(
            found.Document.OperatorLogin,
            message.OperatorLogin,
            accessToken,
            startToken,
            ReadAsync,
            PersistAsync);

        if (!started)
        {
            var afterStart = await snapshotStore.GetAsync(message.OperatorLogin, cancellationToken).ConfigureAwait(false);
            if (afterStart is OperatorSnapshotGetResult.Found afterFound)
            {
                await TryRevertFailedPassStartAsync(found.Document, afterFound, cancellationToken).ConfigureAwait(false);
            }

            await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await WaitForTerminalOrRetryAsync(message, invocation, cancellationToken).ConfigureAwait(false);
        await invocation.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WaitForTerminalOrRetryAsync(
        DailySnapshotQueueMessage message,
        DailySnapshotQueueInvocation invocation,
        CancellationToken cancellationToken)
    {
        for (var i = 0; ; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            var read = await snapshotStore.GetAsync(message.OperatorLogin, cancellationToken).ConfigureAwait(false);
            if (read is not OperatorSnapshotGetResult.Found found)
            {
                return;
            }

            if (found.Document.Run.Status is OperatorSnapshotRunStatus.Succeeded)
            {
                found.Document.PassExecuting = false;
                found.Document.PassLeaseUntil = null;
                found.Document.AttemptsUsed = null;
                found.Document.Trigger = OperatorSnapshotCollectionTrigger.Daily;
                await snapshotStore.SaveAsync(message.OperatorLogin, found.Document, found.Token, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (found.Document.Trigger == OperatorSnapshotCollectionTrigger.Daily
                && found.Document.Run.Status == OperatorSnapshotRunStatus.InProgress
                && !found.Document.PassExecuting
                && !coordinator.HasLiveTask(message.OperatorLogin)
                && found.Document.Run.Reason is OperatorSnapshotRunReason.RateLimit or OperatorSnapshotRunReason.Unexpected
                && (found.Document.AttemptsUsed ?? 1) < 4)
            {
                await queuePublisher.EnqueueAsync(
                    new DailySnapshotQueueMessage
                    {
                        OccasionDate = message.OccasionDate,
                        OperatorLogin = message.OperatorLogin,
                        Kind = "retry",
                    },
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            if (found.Document.Run.Status is OperatorSnapshotRunStatus.Failed)
            {
                found.Document.PassExecuting = false;
                found.Document.PassLeaseUntil = null;
                await snapshotStore.SaveAsync(message.OperatorLogin, found.Document, found.Token, cancellationToken).ConfigureAwait(false);
                if (found.Document.Run.Reason == OperatorSnapshotRunReason.SignInOrToken)
                {
                    await MarkCredentialUnusableAsync(message.OperatorLogin, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            if (found.Document.Run.Status is OperatorSnapshotRunStatus.Interrupted)
            {
                if (found.Document.PassExecuting)
                {
                    found.Document.PassExecuting = false;
                    found.Document.PassLeaseUntil = null;
                    await snapshotStore.SaveAsync(message.OperatorLogin, found.Document, found.Token, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            if (found.Document.PassExecuting && i % 10 == 0)
            {
                found.Document.PassLeaseUntil = timeProvider.GetUtcNow().Add(VisibilityRenew);
                await snapshotStore.SaveAsync(message.OperatorLogin, found.Document, found.Token, cancellationToken).ConfigureAwait(false);
                await invocation.UpdateVisibilityAsync(VisibilityRenew + VisibilityRenew, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsDailyRetryWaitState(OperatorSnapshotDocument document) =>
        document.Trigger == OperatorSnapshotCollectionTrigger.Daily
        && document.Run.Status == OperatorSnapshotRunStatus.InProgress
        && !document.PassExecuting
        && document.Run.Reason is OperatorSnapshotRunReason.RateLimit or OperatorSnapshotRunReason.Unexpected;

    private async Task TryRevertFailedPassStartAsync(
        OperatorSnapshotDocument priorDocument,
        OperatorSnapshotGetResult.Found current,
        CancellationToken cancellationToken)
    {
        if (current.Document.Run.Status != OperatorSnapshotRunStatus.InProgress
            || !current.Document.PassExecuting)
        {
            return;
        }

        var restored = Clone(priorDocument);
        await snapshotStore.SaveAsync(current.Document.OperatorLogin, restored, current.Token, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteScheduledInterruptionAsync(OperatorSnapshotGetResult.Found found, CancellationToken cancellationToken)
    {
        var document = Clone(found.Document);
        document.PassExecuting = false;
        document.PassLeaseUntil = null;
        document.QueuedOccasion = null;
        document.Run = new OperatorSnapshotRun
        {
            Status = OperatorSnapshotRunStatus.Interrupted,
            StartedAt = found.Document.Run.StartedAt,
            FinishedAt = timeProvider.GetUtcNow(),
            RepositoriesRecorded = found.Document.Run.RepositoriesRecorded,
            Reason = OperatorSnapshotRunReason.ScheduledCollectionStopped,
        };
        await snapshotStore.SaveAsync(found.Document.OperatorLogin, document, found.Token, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteSignInFailureAsync(OperatorSnapshotGetResult.Found found, CancellationToken cancellationToken)
    {
        var document = Clone(found.Document);
        document.PassExecuting = false;
        document.PassLeaseUntil = null;
        document.QueuedOccasion = null;
        document.Run = new OperatorSnapshotRun
        {
            Status = OperatorSnapshotRunStatus.Failed,
            StartedAt = found.Document.Run.StartedAt,
            FinishedAt = timeProvider.GetUtcNow(),
            RepositoriesRecorded = found.Document.Run.RepositoriesRecorded,
            Reason = OperatorSnapshotRunReason.SignInOrToken,
        };
        await snapshotStore.SaveAsync(found.Document.OperatorLogin, document, found.Token, cancellationToken).ConfigureAwait(false);
        await MarkCredentialUnusableAsync(found.Document.OperatorLogin, cancellationToken).ConfigureAwait(false);
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

    private async Task<bool> PersistAsync(
        string login,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken token,
        CancellationToken cancellationToken)
    {
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
        QueuedOccasion = source.QueuedOccasion,
        PassExecuting = source.PassExecuting,
        PassLeaseUntil = source.PassLeaseUntil,
        AttemptsUsed = source.AttemptsUsed,
        Run = new OperatorSnapshotRun
        {
            Status = source.Run.Status,
            StartedAt = source.Run.StartedAt,
            FinishedAt = source.Run.FinishedAt,
            RepositoriesRecorded = source.Run.RepositoriesRecorded,
            Reason = source.Run.Reason,
            Detail = source.Run.Detail,
        },
    };
}
