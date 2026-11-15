namespace SoloDevBoard.Application.Services.Planning;

/// <summary>
/// Filters and ranks open work items into the Daily Focus top-three recommendation list.
/// </summary>
public static class DailyFocusRecommendationMapper
{
    /// <summary>The maximum number of recommendations shown on Daily Focus.</summary>
    public const int RecommendationCount = 3;

    /// <summary>Status option name for parked blocked work.</summary>
    /// <remarks>Matched by display name, not a compiled Project option identifier (DEC-029).</remarks>
    public const string BlockedStatusName = "Blocked";

    /// <summary>Status option name for shelved work.</summary>
    /// <remarks>Matched by display name, not a compiled Project option identifier (DEC-029).</remarks>
    public const string IceBoxStatusName = "Ice Box";

    /// <summary>
    /// Selects up to three unblocked work items ranked by priority then recency.
    /// </summary>
    /// <param name="workItems">Open issues and pull requests from included repositories.</param>
    /// <param name="boardItems">Items currently on the selected planning board.</param>
    /// <param name="limitToPlanningBoard">
    /// When <see langword="true"/>, only work items that appear on the planning board are eligible.
    /// </param>
    /// <returns>The ranked recommendation list.</returns>
    public static IReadOnlyList<DailyFocusRecommendationDto> SelectTopThree(
        IReadOnlyList<PlanningWorkItemDto> workItems,
        IReadOnlyList<ProjectBoardItemDto> boardItems,
        bool limitToPlanningBoard = false)
    {
        ArgumentNullException.ThrowIfNull(workItems);
        ArgumentNullException.ThrowIfNull(boardItems);

        var excludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var boardKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var boardItem in boardItems)
        {
            var joinKey = PlanningWorkItemJoinKey.For(boardItem);
            boardKeys.Add(joinKey);

            if (IsExcludedBoardStatus(boardItem.Status?.Name))
            {
                excludedKeys.Add(joinKey);
            }
        }

        return workItems
            .Where(item => (!limitToPlanningBoard || boardKeys.Contains(PlanningWorkItemJoinKey.For(item)))
                && PlanningLabelHelpers.IsUnblocked(item.Labels)
                && !excludedKeys.Contains(PlanningWorkItemJoinKey.For(item)))
            .OrderBy(static item => PlanningPriorityRanker.GetRank(PlanningLabelHelpers.ParsePriorityLabel(item.Labels)))
            .ThenByDescending(static item => item.UpdatedAt)
            .ThenBy(static item => item.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Number)
            .Take(RecommendationCount)
            .Select(static (item, index) => Map(item, index + 1))
            .ToArray();
    }

    /// <summary>
    /// Returns whether a board Status name excludes the item from recommendations.
    /// </summary>
    /// <param name="statusName">The Status option display name, or <see langword="null"/> when unset.</param>
    /// <returns>
    /// <see langword="true"/> when the name is Blocked, Ice Box, or In Progress; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsExcludedBoardStatus(string? statusName)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return false;
        }

        return statusName.Equals(BlockedStatusName, StringComparison.OrdinalIgnoreCase)
            || statusName.Equals(IceBoxStatusName, StringComparison.OrdinalIgnoreCase)
            || statusName.Equals(DailyFocusBoardStateMapper.InProgressStatusName, StringComparison.OrdinalIgnoreCase);
    }

    private static DailyFocusRecommendationDto Map(PlanningWorkItemDto item, int rank)
        => new(
            rank,
            item.RepositoryFullName,
            item.Number,
            item.Title,
            item.HtmlUrl,
            PlanningLabelHelpers.ParsePriorityLabel(item.Labels));
}
