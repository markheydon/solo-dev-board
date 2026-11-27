namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Runs one daily snapshot collection pass for a queued operator.</summary>
public interface IDailySnapshotCollectionPassService
{
    /// <summary>Handles one queue message (start, retry, or complete).</summary>
    Task HandleQueueMessageAsync(
        DailySnapshotQueueInvocation invocation,
        CancellationToken cancellationToken = default);
}

/// <summary>Queue trigger context passed from the Functions host.</summary>
public sealed class DailySnapshotQueueInvocation
{
    /// <summary>Message body.</summary>
    public required DailySnapshotQueueMessage Message { get; init; }

    /// <summary>Completes the platform message after handling.</summary>
    public required Func<CancellationToken, Task> CompleteAsync { get; init; }

    /// <summary>Renews visibility while a pass is executing.</summary>
    public required Func<TimeSpan, CancellationToken, Task> UpdateVisibilityAsync { get; init; }
}

/// <summary>Payload on the daily snapshot collection queue.</summary>
public sealed class DailySnapshotQueueMessage
{
    /// <summary>UTC occasion date (<c>yyyy-MM-dd</c>).</summary>
    public string OccasionDate { get; set; } = string.Empty;

    /// <summary>Sanitised operator login.</summary>
    public string OperatorLogin { get; set; } = string.Empty;

    /// <summary><c>initial</c>, <c>retry</c>, or <c>unmetFill</c>.</summary>
    public string Kind { get; set; } = "initial";

    /// <summary>Retries and <c>unmetFill</c>: earliest UTC instant the message may be handled (ISO-8601 in JSON).</summary>
    public string? NotBefore { get; set; }
}
