using Microsoft.Extensions.Logging;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Etag-safe transitions for account scan status blobs.</summary>
public sealed class AccountScanStatusTransitionService(
    IAccountScanStatusStore store,
    AccountScanStatusReporter reporter,
    TimeProvider timeProvider,
    ILogger<AccountScanStatusTransitionService> logger)
{
    private static readonly TimeSpan LivenessCadence = TimeSpan.FromSeconds(30);

    private readonly IAccountScanStatusStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly AccountScanStatusReporter _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<AccountScanStatusTransitionService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Creates <c>neverRun</c> when the blob is absent.</summary>
    public async Task EnsureNeverRunIfAbsentAsync(string operatorLogin, CancellationToken cancellationToken = default)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin);
        if (sanitised is null)
        {
            return;
        }

        var read = await _store.GetAsync(sanitised, cancellationToken).ConfigureAwait(false);
        if (read is not AccountScanStatusReadResult.NotFound)
        {
            return;
        }

        var document = AccountScanStatusDocumentConstants.CreateNeverRun(sanitised);
        await _store.TryCreateAsync(document, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reports status for an operator at the current clock.</summary>
    public async Task<AccountScanStatusReport> GetReportAsync(
        string operatorLogin,
        bool hostedSignInRecordExists,
        CancellationToken cancellationToken = default)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin);
        if (sanitised is null)
        {
            return AccountScanStatusReport.Lean();
        }

        var read = await _store.GetAsync(sanitised, cancellationToken).ConfigureAwait(false);
        if (read is AccountScanStatusReadResult.NotFound)
        {
            var reported = hostedSignInRecordExists
                ? _reporter.ReportMissingBlob()
                : ReportedAccountScanStatus.NeverRun;
            return AccountScanStatusReport.FromReported(reported);
        }

        if (read is not AccountScanStatusReadResult.Found found)
        {
            return AccountScanStatusReport.Lean();
        }

        var status = _reporter.Report(found.Document);
        return AccountScanStatusReport.FromDocument(found.Document, status);
    }

    /// <summary>Restores orphan <c>scheduled</c> rows with no pending queue message before a fill.</summary>
    public async Task RestoreOrphanScheduledAsync(string operatorLogin, CancellationToken cancellationToken = default)
    {
        var read = await _store.GetAsync(operatorLogin, cancellationToken).ConfigureAwait(false);
        if (read is not AccountScanStatusReadResult.Found found)
        {
            return;
        }

        if (!string.Equals(found.Document.Status, AccountScanStatusDocumentConstants.StatusScheduled, StringComparison.Ordinal)
            || found.Document.QueueMessagePending)
        {
            return;
        }

        var next = Clone(found.Document);
        next.Status = found.Document.LastResult is null
            ? AccountScanStatusDocumentConstants.StatusNeverRun
            : AccountScanStatusDocumentConstants.StatusFinished;

        await _store.TryReplaceAsync(next, found.Etag, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Wins <c>scheduled</c> from a fill and returns the new generation on success.
    /// The first etag wins; a lost etag never retries.
    /// </summary>
    public async Task<AccountScanScheduleWinResult> TryWinScheduleFromFillAsync(
        string operatorLogin,
        CancellationToken cancellationToken = default)
    {
        var read = await _store.GetAsync(operatorLogin, cancellationToken).ConfigureAwait(false);
        if (read is not AccountScanStatusReadResult.Found found)
        {
            return AccountScanScheduleWinResults.LostRace;
        }

        var reported = _reporter.Report(found.Document);
        if (reported is ReportedAccountScanStatus.Scheduled or ReportedAccountScanStatus.Running)
        {
            return AccountScanScheduleWinResults.LostRace;
        }

        var next = Clone(found.Document);
        next.Status = AccountScanStatusDocumentConstants.StatusScheduled;
        next.SuspensionReason = null;
        next.QueueVisibleAfter = null;
        next.LivenessAt = null;
        next.RunningSince = null;
        next.ScanGeneration = found.Document.ScanGeneration + 1;
        next.QueueMessagePending = false;

        var write = await _store.TryReplaceAsync(next, found.Etag, cancellationToken).ConfigureAwait(false);
        return write is AccountScanStatusWriteResult.Succeeded scheduled
            ? new AccountScanScheduleWinResult.Won(next.ScanGeneration, scheduled.Etag)
            : AccountScanScheduleWinResults.LostRace;
    }

    /// <summary>Reads the stored document, or null when absent or unsafe.</summary>
    public async Task<AccountScanStatusDocument?> GetDocumentAsync(
        string operatorLogin,
        CancellationToken cancellationToken = default)
    {
        var read = await _store.GetAsync(operatorLogin, cancellationToken).ConfigureAwait(false);
        return read is AccountScanStatusReadResult.Found found ? found.Document : null;
    }

    /// <summary>Writes a previous document back at the current generation after a failed start.</summary>
    public async Task RestoreAfterFailedStartAsync(
        string operatorLogin,
        int generation,
        AccountScanStatusDocument previous,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previous);
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                generation,
                document =>
                {
                    document.Status = previous.Status;
                    document.SuspensionReason = previous.SuspensionReason;
                    document.LivenessAt = previous.LivenessAt;
                    document.RunningSince = previous.RunningSince;
                    document.QueueMessagePending = previous.QueueMessagePending;
                    document.QueueVisibleAfter = previous.QueueVisibleAfter;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Marks queue accepted after a successful enqueue.</summary>
    public async Task MarkQueueMessagePendingAsync(
        string operatorLogin,
        int expectedGeneration,
        CancellationToken cancellationToken = default)
    {
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                expectedGeneration,
                document =>
                {
                    document.QueueMessagePending = true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Starts <c>running</c> from a dequeue or a hand start. A hand start takes a new generation and only the
    /// first etag wins. A dequeue keeps the generation set by the fill and re-reads once on a lost etag.
    /// </summary>
    public async Task<AccountScanRunningWinResult> TryWinRunningAsync(
        string operatorLogin,
        OperatorSnapshotCollectionTrigger trigger,
        bool fromHandStart,
        CancellationToken cancellationToken = default)
    {
        var attempts = fromHandStart ? 1 : 2;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var read = await _store.GetAsync(operatorLogin, cancellationToken).ConfigureAwait(false);
            if (read is not AccountScanStatusReadResult.Found found)
            {
                return AccountScanRunningWinResults.LostRace;
            }

            var reported = _reporter.Report(found.Document);
            if (fromHandStart && reported is ReportedAccountScanStatus.Scheduled or ReportedAccountScanStatus.Running)
            {
                return AccountScanRunningWinResults.Refused;
            }

            if (!fromHandStart
                && string.Equals(found.Document.Status, AccountScanStatusDocumentConstants.StatusRunning, StringComparison.Ordinal))
            {
                return new AccountScanRunningWinResult.AlreadyRunning(found.Document.ScanGeneration);
            }

            var now = _timeProvider.GetUtcNow();
            var next = Clone(found.Document);
            next.Status = AccountScanStatusDocumentConstants.StatusRunning;
            next.SuspensionReason = null;
            next.LivenessAt = now;
            next.RunningSince = now;
            next.QueueVisibleAfter = null;
            next.QueueMessagePending = !fromHandStart;
            if (fromHandStart)
            {
                next.ScanGeneration = found.Document.ScanGeneration + 1;
            }

            var write = await _store.TryReplaceAsync(next, found.Etag, cancellationToken).ConfigureAwait(false);
            if (write is AccountScanStatusWriteResult.Succeeded succeeded)
            {
                return new AccountScanRunningWinResult.Won(next.ScanGeneration, trigger, succeeded.Etag);
            }
        }

        return AccountScanRunningWinResults.LostRace;
    }

    /// <summary>Writes another liveness mark when the cadence elapses.</summary>
    public async Task TouchLivenessIfDueAsync(
        string operatorLogin,
        int capturedGeneration,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                capturedGeneration,
                document =>
                {
                    if (!string.Equals(document.Status, AccountScanStatusDocumentConstants.StatusRunning, StringComparison.Ordinal))
                    {
                        return;
                    }

                    if (document.LivenessAt is { } last && now - last < LivenessCadence)
                    {
                        return;
                    }

                    document.LivenessAt = now;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Records success for the active generation.</summary>
    public async Task RecordSuccessAsync(string operatorLogin, int generation, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                generation,
                document =>
                {
                    document.Status = AccountScanStatusDocumentConstants.StatusFinished;
                    document.LastScanAt = now;
                    document.LastResult = AccountScanStatusDocumentConstants.LastResultSuccess;
                    document.ResultComment = AccountScanStatusDocumentConstants.CommentSuccess;
                    document.SuspensionReason = null;
                    document.LivenessAt = null;
                    document.RunningSince = null;
                    document.QueueMessagePending = false;
                    document.QueueVisibleAfter = null;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Records not-allowed suspension without starting a scan.</summary>
    public async Task RecordNotAllowedSuspensionAsync(string operatorLogin, int generation, CancellationToken cancellationToken = default)
    {
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                generation,
                document =>
                {
                    document.Status = AccountScanStatusDocumentConstants.StatusSuspended;
                    document.SuspensionReason = AccountScanStatusDocumentConstants.SuspensionNotAllowed;
                    document.QueueMessagePending = false;
                    document.QueueVisibleAfter = null;
                    document.LivenessAt = null;
                    document.RunningSince = null;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Records token failure suspension.</summary>
    public async Task RecordTokenFailureAsync(string operatorLogin, int generation, string comment, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                generation,
                document =>
                {
                    document.Status = AccountScanStatusDocumentConstants.StatusSuspended;
                    document.LastScanAt = now;
                    document.LastResult = AccountScanStatusDocumentConstants.LastResultFailure;
                    document.ResultComment = comment;
                    document.SuspensionReason = null;
                    document.QueueMessagePending = false;
                    document.QueueVisibleAfter = null;
                    document.LivenessAt = null;
                    document.RunningSince = null;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Records token failure as finished when a newer worker token is already stored.</summary>
    public async Task RecordTokenFailureFinishedAsync(
        string operatorLogin,
        int generation,
        string comment,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                generation,
                document =>
                {
                    document.Status = AccountScanStatusDocumentConstants.StatusFinished;
                    document.LastScanAt = now;
                    document.LastResult = AccountScanStatusDocumentConstants.LastResultFailure;
                    document.ResultComment = comment;
                    document.SuspensionReason = null;
                    document.QueueMessagePending = false;
                    document.QueueVisibleAfter = null;
                    document.LivenessAt = null;
                    document.RunningSince = null;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// After a kept worker refresh token, lifts a suspended token wait to finished without changing the failure comment.
    /// </summary>
    public async Task LiftSuspendedTokenWaitAfterWorkerRefreshAsync(
        string operatorLogin,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var read = await _store.GetAsync(operatorLogin, cancellationToken).ConfigureAwait(false);
            if (read is not AccountScanStatusReadResult.Found found)
            {
                return;
            }

            if (!string.Equals(found.Document.Status, AccountScanStatusDocumentConstants.StatusSuspended, StringComparison.Ordinal)
                || found.Document.SuspensionReason is not null)
            {
                return;
            }

            var next = Clone(found.Document);
            next.Status = AccountScanStatusDocumentConstants.StatusFinished;
            next.SuspensionReason = null;
            var write = await _store.TryReplaceAsync(next, found.Etag, cancellationToken).ConfigureAwait(false);
            if (write is AccountScanStatusWriteResult.Succeeded)
            {
                return;
            }
        }
    }

    /// <summary>Queues a further try as <c>scheduled</c>.</summary>
    public async Task ScheduleFurtherTryAsync(
        string operatorLogin,
        int generation,
        DateTimeOffset queueVisibleAfter,
        CancellationToken cancellationToken = default)
    {
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                generation,
                document =>
                {
                    document.Status = AccountScanStatusDocumentConstants.StatusScheduled;
                    document.QueueMessagePending = true;
                    document.QueueVisibleAfter = queueVisibleAfter;
                    document.LivenessAt = null;
                    document.RunningSince = null;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Records terminal failure after retries and clears the queue flag.</summary>
    public async Task RecordExhaustedFailureAsync(
        string operatorLogin,
        int generation,
        string comment,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                generation,
                document =>
                {
                    document.Status = AccountScanStatusDocumentConstants.StatusFinished;
                    document.LastScanAt = now;
                    document.LastResult = AccountScanStatusDocumentConstants.LastResultFailure;
                    document.ResultComment = comment;
                    document.QueueMessagePending = false;
                    document.QueueVisibleAfter = null;
                    document.LivenessAt = null;
                    document.RunningSince = null;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Clears queue pending after worker redelivery without changing stored running status.</summary>
    public async Task ClearQueuePendingForGenerationAsync(
        string operatorLogin,
        int generation,
        CancellationToken cancellationToken = default)
    {
        await MutateWhenGenerationMatchesAsync(
                operatorLogin,
                generation,
                document => document.QueueMessagePending = false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Returns true when the stored generation is still active.</summary>
    public async Task<bool> IsGenerationCurrentAsync(
        string operatorLogin,
        int generation,
        CancellationToken cancellationToken = default)
    {
        var read = await _store.GetAsync(operatorLogin, cancellationToken).ConfigureAwait(false);
        return read is AccountScanStatusReadResult.Found found && found.Document.ScanGeneration == generation;
    }

    private async Task<bool> MutateWhenGenerationMatchesAsync(
        string operatorLogin,
        int expectedGeneration,
        Action<AccountScanStatusDocument> mutate,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var read = await _store.GetAsync(operatorLogin, cancellationToken).ConfigureAwait(false);
            if (read is not AccountScanStatusReadResult.Found found)
            {
                return false;
            }

            if (found.Document.ScanGeneration != expectedGeneration)
            {
                return false;
            }

            var next = Clone(found.Document);
            mutate(next);
            var write = await _store.TryReplaceAsync(next, found.Etag, cancellationToken).ConfigureAwait(false);
            if (write is AccountScanStatusWriteResult.Succeeded)
            {
                return true;
            }
        }

        _logger.LogDebug("Lost etag updating account scan status for {Login}.", operatorLogin);
        return false;
    }

    private static AccountScanStatusDocument Clone(AccountScanStatusDocument source) => new()
    {
        SchemaVersion = source.SchemaVersion,
        OperatorLogin = source.OperatorLogin,
        Status = source.Status,
        LastScanAt = source.LastScanAt,
        LastResult = source.LastResult,
        ResultComment = source.ResultComment,
        SuspensionReason = source.SuspensionReason,
        LivenessAt = source.LivenessAt,
        RunningSince = source.RunningSince,
        ScanGeneration = source.ScanGeneration,
        QueueMessagePending = source.QueueMessagePending,
        QueueVisibleAfter = source.QueueVisibleAfter,
    };
}

/// <summary>Fact bundle for status DTO mapping.</summary>
public sealed record AccountScanStatusReport(
    ReportedAccountScanStatus? Reported,
    string? StoredStatus,
    DateTimeOffset? LastScanAt,
    string? LastResult,
    string? ResultComment,
    AccountScanSuspensionReason SuspensionReason)
{
    /// <summary>Lean posture has no account scan status.</summary>
    public static AccountScanStatusReport Lean() => new(null, null, null, null, null, AccountScanSuspensionReason.None);

    /// <summary>Builds from stored document.</summary>
    public static AccountScanStatusReport FromDocument(AccountScanStatusDocument document, ReportedAccountScanStatus reported)
    {
        var suspension = string.Equals(
            document.SuspensionReason,
            AccountScanStatusDocumentConstants.SuspensionNotAllowed,
            StringComparison.Ordinal)
            ? AccountScanSuspensionReason.NotAllowed
            : AccountScanSuspensionReason.None;

        return new(
            reported,
            document.Status,
            document.LastScanAt,
            document.LastResult,
            document.ResultComment,
            suspension);
    }

    /// <summary>Builds when blob is absent.</summary>
    public static AccountScanStatusReport FromReported(ReportedAccountScanStatus reported) =>
        new(reported, null, null, null, null, AccountScanSuspensionReason.None);
}

/// <summary>Outcome of winning a schedule write.</summary>
public abstract record AccountScanScheduleWinResult
{
    /// <summary>Schedule won.</summary>
    public sealed record Won(int Generation, string Etag) : AccountScanScheduleWinResult;

    /// <summary>Another writer won.</summary>
    public sealed record LostRace : AccountScanScheduleWinResult;
}

/// <summary>Singleton schedule win outcomes.</summary>
public static class AccountScanScheduleWinResults
{
    /// <summary>Lost race singleton.</summary>
    public static AccountScanScheduleWinResult.LostRace LostRace { get; } = new();
}

/// <summary>Outcome of winning a running write.</summary>
public abstract record AccountScanRunningWinResult
{
    /// <summary>Running won.</summary>
    public sealed record Won(
        int Generation,
        OperatorSnapshotCollectionTrigger Trigger,
        string Etag) : AccountScanRunningWinResult;

    /// <summary>A dequeue found status already <c>running</c> (redelivery or duplicate).</summary>
    public sealed record AlreadyRunning(int Generation) : AccountScanRunningWinResult;

    /// <summary>Hand start refused.</summary>
    public sealed record Refused : AccountScanRunningWinResult;

    /// <summary>Lost race.</summary>
    public sealed record LostRace : AccountScanRunningWinResult;
}

/// <summary>Singleton running win outcomes.</summary>
public static class AccountScanRunningWinResults
{
    /// <summary>Lost race singleton.</summary>
    public static AccountScanRunningWinResult.LostRace LostRace { get; } = new();

    /// <summary>Refused singleton.</summary>
    public static AccountScanRunningWinResult.Refused Refused { get; } = new();
}
