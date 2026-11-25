namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>One hosted account's current scan state blob.</summary>
public sealed class AccountScanStatusDocument
{
    /// <summary>Document schema version.</summary>
    public int SchemaVersion { get; set; } = AccountScanStatusDocumentConstants.CurrentSchemaVersion;

    /// <summary>Sanitised operator login.</summary>
    public string OperatorLogin { get; set; } = string.Empty;

    /// <summary>Stored status (<c>neverRun</c>, <c>scheduled</c>, <c>running</c>, <c>finished</c>, <c>suspended</c>).</summary>
    public string Status { get; set; } = AccountScanStatusDocumentConstants.StatusNeverRun;

    /// <summary>When the last completed scan ran, if any.</summary>
    public DateTimeOffset? LastScanAt { get; set; }

    /// <summary><c>success</c>, <c>failure</c>, or null.</summary>
    public string? LastResult { get; set; }

    /// <summary>Success or failure detail; empty when none has run.</summary>
    public string ResultComment { get; set; } = string.Empty;

    /// <summary><c>notAllowed</c> or null.</summary>
    public string? SuspensionReason { get; set; }

    /// <summary>Latest liveness mark while <see cref="Status"/> is <c>running</c>.</summary>
    public DateTimeOffset? LivenessAt { get; set; }

    /// <summary>When this <c>running</c> generation started.</summary>
    public DateTimeOffset? RunningSince { get; set; }

    /// <summary>Increments when a new scan wins.</summary>
    public int ScanGeneration { get; set; }

    /// <summary>True when a queue message is accepted and not yet completed.</summary>
    public bool QueueMessagePending { get; set; }

    /// <summary>Copied from message <c>notBefore</c> for retries.</summary>
    public DateTimeOffset? QueueVisibleAfter { get; set; }
}

/// <summary>Known stored and JSON status values.</summary>
public static class AccountScanStatusDocumentConstants
{
    /// <summary>Current schema version.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Stored status: no scan has run.</summary>
    public const string StatusNeverRun = "neverRun";

    /// <summary>Stored status: waiting in queue.</summary>
    public const string StatusScheduled = "scheduled";

    /// <summary>Stored status: scan in progress.</summary>
    public const string StatusRunning = "running";

    /// <summary>Stored status: terminal success or failure.</summary>
    public const string StatusFinished = "finished";

    /// <summary>Stored status: blocked until resolution.</summary>
    public const string StatusSuspended = "suspended";

    /// <summary>Last result: success.</summary>
    public const string LastResultSuccess = "success";

    /// <summary>Last result: failure.</summary>
    public const string LastResultFailure = "failure";

    /// <summary>Success comment.</summary>
    public const string CommentSuccess = "Success";

    /// <summary>Suspension: not allowed.</summary>
    public const string SuspensionNotAllowed = "notAllowed";

    /// <summary>Creates the default <c>neverRun</c> shape for a login.</summary>
    public static AccountScanStatusDocument CreateNeverRun(string sanitisedLogin) => new()
    {
        OperatorLogin = sanitisedLogin,
        Status = StatusNeverRun,
        LastScanAt = null,
        LastResult = null,
        ResultComment = string.Empty,
        SuspensionReason = null,
        LivenessAt = null,
        RunningSince = null,
        ScanGeneration = 0,
        QueueMessagePending = false,
        QueueVisibleAfter = null,
    };
}
