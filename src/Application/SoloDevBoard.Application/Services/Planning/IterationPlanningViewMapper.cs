namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Maps work-item and project-board catalogues to Iteration Planning view rows.</summary>
public static class IterationPlanningViewMapper
{
    /// <summary>
    /// Builds the Iteration Planning view from cross-repository work items and the selected board catalogue.
    /// </summary>
    /// <param name="workItems">Open issues and pull requests from included repositories.</param>
    /// <param name="boardItems">Items currently on the selected planning board.</param>
    /// <param name="failures">Per-repository catalogue failures to carry through to the view.</param>
    /// <param name="hasFocusOrderField"><see langword="true" /> when the selected board exposes a Focus Order field.</param>
    /// <param name="capacity">The persisted planning capacity from PM settings.</param>
    /// <param name="stallDays">The inclusive stall threshold in days from PM settings.</param>
    /// <param name="utcNow">The current UTC time used to compute stall age.</param>
    /// <returns>The planning view snapshot.</returns>
    public static IterationPlanningViewDto Map(
        IReadOnlyList<PlanningWorkItemDto> workItems,
        IReadOnlyList<ProjectBoardItemDto> boardItems,
        IReadOnlyList<PlanningRepositoryCatalogueFailureDto> failures,
        bool hasFocusOrderField,
        int capacity,
        int stallDays,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(workItems);
        ArgumentNullException.ThrowIfNull(boardItems);
        ArgumentNullException.ThrowIfNull(failures);

        var boardItemsByKey = BuildBoardItemsByKey(boardItems);
        var labelsByKey = BuildLabelsByKey(workItems);

        var upNextBoardItems = boardItems
            .Where(static item => DailyFocusBoardStateMapper.IsUpNextStatus(item.Status?.Name))
            .ToArray();

        var upNextItems = upNextBoardItems
            .OrderBy(static item => item.FocusOrder ?? double.MaxValue)
            .ThenBy(static item => item.Content.Title, StringComparer.OrdinalIgnoreCase)
            .Select(item => MapUpNextItem(item, labelsByKey))
            .ToArray();

        var nextStoryFocusOrder = hasFocusOrderField
            ? PlanningFocusOrderSequencer.GetNextFocusOrder(upNextBoardItems)
            : 0;

        var candidates = workItems
            .Where(workItem => IsCandidate(workItem, boardItemsByKey))
            .Select(workItem => MapCandidate(workItem, boardItemsByKey))
            .OrderBy(static candidate => candidate.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Number)
            .ToArray();

        var activeLoad = PlanningCapacityEvaluator.CountActiveLoad(boardItems);
        var resolvedCapacity = PlanningCapacityEvaluator.ResolveCapacity(capacity);
        var resolvedStallDays = stallDays > 0 ? stallDays : PlanningSettingsDefaults.StallDays;
        var stalledUpNextItems = MapStalledUpNextItems(upNextBoardItems, labelsByKey, resolvedStallDays, utcNow);

        return new IterationPlanningViewDto(
            upNextItems,
            candidates,
            failures,
            hasFocusOrderField,
            nextStoryFocusOrder,
            activeLoad,
            resolvedCapacity,
            PlanningCapacityEvaluator.IsAtOrOverCapacity(activeLoad, capacity),
            stalledUpNextItems);
    }

    private static IReadOnlyList<IterationPlanningStalledItemDto> MapStalledUpNextItems(
        IReadOnlyList<ProjectBoardItemDto> upNextBoardItems,
        IReadOnlyDictionary<string, IReadOnlyList<string>> labelsByKey,
        int stallDays,
        DateTimeOffset utcNow)
    {
        return upNextBoardItems
            .Where(item => DailyFocusBoardStateMapper.HasStallClock(item.ActivityTimestamp))
            .Select(item => MapStalledUpNextItem(item, labelsByKey, utcNow))
            .Where(item => item.AgeInDays >= stallDays)
            .OrderByDescending(item => item.AgeInDays)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IterationPlanningStalledItemDto MapStalledUpNextItem(
        ProjectBoardItemDto boardItem,
        IReadOnlyDictionary<string, IReadOnlyList<string>> labelsByKey,
        DateTimeOffset utcNow)
    {
        labelsByKey.TryGetValue(PlanningWorkItemJoinKey.For(boardItem), out var labels);

        return new IterationPlanningStalledItemDto(
            boardItem.ProjectItemId,
            boardItem.Content.ContentType == ProjectBoardItemContentTypeDto.PullRequest
                ? PlanningWorkItemTypeDto.PullRequest
                : PlanningWorkItemTypeDto.Issue,
            boardItem.Content.Number,
            boardItem.Content.Title,
            boardItem.Content.Url,
            $"{boardItem.Content.RepositoryOwner}/{boardItem.Content.RepositoryName}",
            DailyFocusBoardStateMapper.GetAgeInDays(boardItem.ActivityTimestamp, utcNow),
            boardItem.UsedItemUpdatedAtFallback,
            labels ?? []);
    }

    /// <summary>
    /// Returns whether a work item can appear in the candidate picker.
    /// Items already Up Next or In Progress on the board are excluded.
    /// </summary>
    /// <param name="workItem">The catalogue work item.</param>
    /// <param name="boardStatusName">The joined planning-board Status name, or <see langword="null"/> when unset.</param>
    /// <returns><see langword="true" /> when the item may be added to Up Next; otherwise, <see langword="false" />.</returns>
    public static bool IsCandidate(PlanningWorkItemDto workItem, string? boardStatusName)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        return !BacklogReviewGrouping.IsCommittedOrParkedBoardStatus(boardStatusName);
    }

    private static Dictionary<string, ProjectBoardItemDto> BuildBoardItemsByKey(
        IReadOnlyList<ProjectBoardItemDto> boardItems)
    {
        var boardItemsByKey = new Dictionary<string, ProjectBoardItemDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var boardItem in boardItems)
        {
            boardItemsByKey.TryAdd(PlanningWorkItemJoinKey.For(boardItem), boardItem);
        }

        return boardItemsByKey;
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildLabelsByKey(IReadOnlyList<PlanningWorkItemDto> workItems)
    {
        var labelsByKey = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var workItem in workItems)
        {
            labelsByKey[PlanningWorkItemJoinKey.For(workItem)] = workItem.Labels;
        }

        return labelsByKey;
    }

    private static bool IsCandidate(
        PlanningWorkItemDto workItem,
        IReadOnlyDictionary<string, ProjectBoardItemDto> boardItemsByKey)
    {
        boardItemsByKey.TryGetValue(PlanningWorkItemJoinKey.For(workItem), out var boardItem);
        return IsCandidate(workItem, boardItem?.Status?.Name);
    }

    private static IterationPlanningUpNextItemDto MapUpNextItem(
        ProjectBoardItemDto boardItem,
        IReadOnlyDictionary<string, IReadOnlyList<string>> labelsByKey)
    {
        labelsByKey.TryGetValue(PlanningWorkItemJoinKey.For(boardItem), out var labels);

        return new IterationPlanningUpNextItemDto(
            boardItem.ProjectItemId,
            boardItem.Content.ContentType == ProjectBoardItemContentTypeDto.PullRequest
                ? PlanningWorkItemTypeDto.PullRequest
                : PlanningWorkItemTypeDto.Issue,
            boardItem.Content.Number,
            boardItem.Content.Title,
            boardItem.Content.Url,
            $"{boardItem.Content.RepositoryOwner}/{boardItem.Content.RepositoryName}",
            boardItem.FocusOrder,
            labels ?? []);
    }

    private static IterationPlanningCandidateDto MapCandidate(
        PlanningWorkItemDto workItem,
        IReadOnlyDictionary<string, ProjectBoardItemDto> boardItemsByKey)
    {
        boardItemsByKey.TryGetValue(PlanningWorkItemJoinKey.For(workItem), out var boardItem);

        return new IterationPlanningCandidateDto(
            workItem.ItemType,
            workItem.Number,
            workItem.Title,
            workItem.HtmlUrl,
            workItem.RepositoryFullName,
            workItem.Labels,
            boardItem?.Status?.Name,
            boardItem?.ProjectItemId);
    }
}
