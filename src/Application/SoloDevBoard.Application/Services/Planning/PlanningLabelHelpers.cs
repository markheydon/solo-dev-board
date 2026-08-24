namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Parses SoloDevBoard label prefixes used by the PM workflow.</summary>
public static class PlanningLabelHelpers
{
    /// <summary>The prefix for <c>type/</c> labels.</summary>
    public const string TypePrefix = "type/";

    /// <summary>The prefix for <c>priority/</c> labels.</summary>
    public const string PriorityPrefix = "priority/";

    /// <summary>The prefix for <c>status/</c> labels.</summary>
    public const string StatusPrefix = "status/";

    /// <summary>The blocked status label name.</summary>
    public const string BlockedStatusLabel = "status/blocked";

    /// <summary>The ice-box status label name.</summary>
    public const string IceBoxStatusLabel = "status/ice-box";

    /// <summary>The epic type label name.</summary>
    public const string EpicTypeLabel = "type/epic";

    /// <summary>The feature type label name.</summary>
    public const string FeatureTypeLabel = "type/feature";

    /// <summary>The critical priority label name.</summary>
    public const string CriticalPriorityLabel = "priority/critical";

    /// <summary>The high priority label name.</summary>
    public const string HighPriorityLabel = "priority/high";

    /// <summary>Parses the first <c>type/</c> label from the supplied label names.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns>The matching label name, or <see langword="null"/> when none is present.</returns>
    public static string? ParseTypeLabel(IReadOnlyList<string> labels)
        => ParsePrefixedLabel(labels, TypePrefix);

    /// <summary>Parses the first <c>priority/</c> label from the supplied label names.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns>The matching label name, or <see langword="null"/> when none is present.</returns>
    public static string? ParsePriorityLabel(IReadOnlyList<string> labels)
        => ParsePrefixedLabel(labels, PriorityPrefix);

    /// <summary>Parses the first <c>status/</c> label from the supplied label names.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns>The matching label name, or <see langword="null"/> when none is present.</returns>
    public static string? ParseStatusLabel(IReadOnlyList<string> labels)
        => ParsePrefixedLabel(labels, StatusPrefix);

    /// <summary>Determines whether the item is blocked via <c>status/blocked</c>.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns><see langword="true"/> when the blocked status label is present.</returns>
    public static bool IsBlocked(IReadOnlyList<string> labels)
        => ContainsLabel(labels, BlockedStatusLabel);

    /// <summary>Determines whether the item is shelved via <c>status/ice-box</c>.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns><see langword="true"/> when the ice-box status label is present.</returns>
    public static bool IsIceBoxed(IReadOnlyList<string> labels)
        => ContainsLabel(labels, IceBoxStatusLabel);

    /// <summary>Determines whether the item is blocked or deferred.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns><see langword="true"/> when a blocked or ice-box status label is present.</returns>
    public static bool IsBlockedOrDeferred(IReadOnlyList<string> labels)
        => IsBlocked(labels) || IsIceBoxed(labels);

    /// <summary>Determines whether the item is unblocked for recommendation and ready-to-start groups.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns><see langword="true"/> when neither blocked nor ice-box status labels are present.</returns>
    public static bool IsUnblocked(IReadOnlyList<string> labels)
        => !IsBlockedOrDeferred(labels);

    /// <summary>Determines whether the item is missing a core <c>type/</c> or <c>priority/</c> label.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns><see langword="true"/> when either core label prefix is absent.</returns>
    public static bool IsAwaitingTriage(IReadOnlyList<string> labels)
        => ParseTypeLabel(labels) is null || ParsePriorityLabel(labels) is null;

    /// <summary>Determines whether the item belongs in the urgent backlog group.</summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <returns><see langword="true"/> when a critical or high priority label is present.</returns>
    public static bool IsUrgent(IReadOnlyList<string> labels)
    {
        var priority = ParsePriorityLabel(labels);
        return priority is CriticalPriorityLabel or HighPriorityLabel;
    }

    /// <summary>
    /// Determines whether an open epic or feature is near completion because all tracked sub-issues are closed.
    /// </summary>
    /// <param name="labels">Label names to inspect.</param>
    /// <param name="subIssueTotal">The tracked sub-issue total, when known.</param>
    /// <param name="subIssueCompleted">The completed tracked sub-issue count, when known.</param>
    /// <returns><see langword="true"/> when sub-issue counts indicate full completion.</returns>
    public static bool IsEpicNearComplete(IReadOnlyList<string> labels, int? subIssueTotal, int? subIssueCompleted)
    {
        var typeLabel = ParseTypeLabel(labels);
        if (typeLabel is not EpicTypeLabel and not FeatureTypeLabel)
        {
            return false;
        }

        return subIssueTotal is > 0 && subIssueCompleted == subIssueTotal;
    }

    private static string? ParsePrefixedLabel(IReadOnlyList<string> labels, string prefix)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        foreach (var label in labels)
        {
            if (label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }
        }

        return null;
    }

    private static bool ContainsLabel(IReadOnlyList<string> labels, string expectedLabel)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedLabel);

        return labels.Any(label => label.Equals(expectedLabel, StringComparison.OrdinalIgnoreCase));
    }
}
