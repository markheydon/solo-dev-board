namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Classifies catalogue work items into Backlog Review urgency groups.</summary>
public static class BacklogReviewGrouping
{
    /// <summary>
    /// Groups work items into urgent, ready-to-start, and blocked or deferred lists.
    /// An item may appear in more than one group.
    /// </summary>
    /// <param name="workItems">Open issues and pull requests from included repositories.</param>
    /// <param name="boardItems">Items currently on the selected planning board.</param>
    /// <param name="failures">Per-repository catalogue failures to carry through to the result.</param>
    /// <returns>The grouped Backlog Review snapshot.</returns>
    public static BacklogReviewResultDto Group(
        IReadOnlyList<PmWorkItemDto> workItems,
        IReadOnlyList<ProjectBoardItemDto> boardItems,
        IReadOnlyList<PmRepositoryCatalogueFailureDto> failures)
    {
        ArgumentNullException.ThrowIfNull(workItems);
        ArgumentNullException.ThrowIfNull(boardItems);
        ArgumentNullException.ThrowIfNull(failures);

        var boardStatusByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var boardItem in boardItems)
        {
            var statusName = boardItem.Status?.Name;
            if (string.IsNullOrWhiteSpace(statusName))
            {
                continue;
            }

            boardStatusByKey[PmWorkItemJoinKey.For(boardItem)] = statusName;
        }

        var urgent = new List<BacklogReviewItemDto>();
        var ready = new List<BacklogReviewItemDto>();
        var blocked = new List<BacklogReviewItemDto>();

        foreach (var workItem in workItems)
        {
            boardStatusByKey.TryGetValue(PmWorkItemJoinKey.For(workItem), out var boardStatusName);
            var row = Map(workItem, boardStatusName);

            if (IsUrgent(workItem))
            {
                urgent.Add(row);
            }

            if (IsReadyToStart(workItem, boardStatusName))
            {
                ready.Add(row);
            }

            if (IsBlockedOrDeferred(workItem, boardStatusName))
            {
                blocked.Add(row);
            }
        }

        return new BacklogReviewResultDto(
            Sort(urgent),
            Sort(ready),
            Sort(blocked),
            failures);
    }

    /// <summary>Returns whether the item belongs in the urgent group.</summary>
    /// <param name="item">The catalogue work item.</param>
    /// <returns><see langword="true" /> when a critical or high priority label is present.</returns>
    public static bool IsUrgent(PmWorkItemDto item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return PmLabelHelpers.IsUrgent(item.Labels);
    }

    /// <summary>
    /// Returns whether the item is ready to start: unblocked by labels and board Status,
    /// and not already Up Next or In Progress.
    /// </summary>
    /// <param name="item">The catalogue work item.</param>
    /// <param name="boardStatusName">The joined planning-board Status name, or <see langword="null"/> when unset.</param>
    /// <returns><see langword="true" /> when the item can be started; otherwise, <see langword="false" />.</returns>
    public static bool IsReadyToStart(PmWorkItemDto item, string? boardStatusName)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!PmLabelHelpers.IsUnblocked(item.Labels))
        {
            return false;
        }

        return !IsCommittedOrParkedBoardStatus(boardStatusName);
    }

    /// <summary>
    /// Returns whether the item is blocked or deferred via status labels or parked board Status.
    /// </summary>
    /// <param name="item">The catalogue work item.</param>
    /// <param name="boardStatusName">The joined planning-board Status name, or <see langword="null"/> when unset.</param>
    /// <returns><see langword="true" /> when the item is parked; otherwise, <see langword="false" />.</returns>
    public static bool IsBlockedOrDeferred(PmWorkItemDto item, string? boardStatusName)
    {
        ArgumentNullException.ThrowIfNull(item);
        return PmLabelHelpers.IsBlockedOrDeferred(item.Labels) || IsParkedBoardStatus(boardStatusName);
    }

    /// <summary>Returns whether a board Status name is Blocked or Ice Box.</summary>
    /// <param name="statusName">The Status option display name, or <see langword="null"/> when unset.</param>
    /// <returns><see langword="true" /> when the name is Blocked or Ice Box; otherwise, <see langword="false" />.</returns>
    public static bool IsParkedBoardStatus(string? statusName)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return false;
        }

        return statusName.Equals(DailyFocusRecommendationMapper.BlockedStatusName, StringComparison.OrdinalIgnoreCase)
            || statusName.Equals(DailyFocusRecommendationMapper.IceBoxStatusName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns whether a board Status name is Up Next, In Progress, Blocked, or Ice Box.</summary>
    /// <param name="statusName">The Status option display name, or <see langword="null"/> when unset.</param>
    /// <returns>
    /// <see langword="true" /> when the name is a committed or parked Status; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsCommittedOrParkedBoardStatus(string? statusName)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return false;
        }

        return DailyFocusBoardStateMapper.IsActiveLoadStatus(statusName) || IsParkedBoardStatus(statusName);
    }

    private static BacklogReviewItemDto Map(PmWorkItemDto item, string? boardStatusName)
        => new(
            item.ItemType,
            item.RepositoryFullName,
            item.Number,
            item.Title,
            item.HtmlUrl,
            item.Labels,
            PmLabelHelpers.ParsePriorityLabel(item.Labels),
            boardStatusName);

    private static IReadOnlyList<BacklogReviewItemDto> Sort(IReadOnlyList<BacklogReviewItemDto> items)
        => items
            .OrderBy(static item => PmPriorityRanker.GetRank(item.PriorityLabel))
            .ThenBy(static item => item.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Number)
            .ToArray();
}
