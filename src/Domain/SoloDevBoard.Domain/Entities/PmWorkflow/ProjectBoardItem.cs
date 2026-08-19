namespace SoloDevBoard.Domain.Entities.PmWorkflow;

/// <summary>Represents a GitHub Project v2 board item with PM workflow catalogue fields.</summary>
public sealed record ProjectBoardItem
{
    /// <summary>Gets the project-item node identifier.</summary>
    public string ProjectItemId { get; init; } = string.Empty;

    /// <summary>Gets the current Status field value when set.</summary>
    public ProjectBoardItemStatus? Status { get; init; }

    /// <summary>Gets the Focus Order number when the field is set on the item.</summary>
    public double? FocusOrder { get; init; }

    /// <summary>Gets the linked issue or pull request content.</summary>
    public ProjectBoardItemContent Content { get; init; } = new();

    /// <summary>
    /// Gets the timestamp used for stall detection.
    /// Prefer the Status field-updated time when available; otherwise fall back to the item <c>updatedAt</c> value.
    /// <see cref="DateTimeOffset.UnixEpoch"/> means both timestamps were missing and must not be treated as elapsed time.
    /// </summary>
    public DateTimeOffset ActivityTimestamp { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="ActivityTimestamp"/> used the item <c>updatedAt</c>
    /// because Status-changed-at was unavailable.
    /// </summary>
    public bool UsedItemUpdatedAtFallback { get; init; }
}
