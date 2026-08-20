namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Maps work-item and project-board catalogues to Iteration Planning view rows.</summary>
public static class IterationPlanningViewMapper
{
    /// <summary>
    /// Builds the Iteration Planning view from cross-repository work items and the selected board catalogue.
    /// </summary>
    /// <param name="workItems">Open issues and pull requests from included repositories.</param>
    /// <param name="boardItems">Items currently on the selected planning board.</param>
    /// <param name="failures">Per-repository catalogue failures to carry through to the view.</param>
    /// <returns>The planning view snapshot.</returns>
    public static IterationPlanningViewDto Map(
        IReadOnlyList<PmWorkItemDto> workItems,
        IReadOnlyList<ProjectBoardItemDto> boardItems,
        IReadOnlyList<PmRepositoryCatalogueFailureDto> failures)
    {
        ArgumentNullException.ThrowIfNull(workItems);
        ArgumentNullException.ThrowIfNull(boardItems);
        ArgumentNullException.ThrowIfNull(failures);

        var boardItemsByKey = BuildBoardItemsByKey(boardItems);
        var labelsByKey = BuildLabelsByKey(workItems);

        var upNextItems = boardItems
            .Where(static item => DailyFocusBoardStateMapper.IsUpNextStatus(item.Status?.Name))
            .OrderBy(static item => item.FocusOrder ?? double.MaxValue)
            .ThenBy(static item => item.Content.Title, StringComparer.OrdinalIgnoreCase)
            .Select(item => MapUpNextItem(item, labelsByKey))
            .ToArray();

        var candidates = workItems
            .Where(workItem => IsCandidate(workItem, boardItemsByKey))
            .Select(workItem => MapCandidate(workItem, boardItemsByKey))
            .OrderBy(static candidate => candidate.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Number)
            .ToArray();

        return new IterationPlanningViewDto(upNextItems, candidates, failures);
    }

    /// <summary>
    /// Returns whether a work item can appear in the candidate picker.
    /// Items already Up Next or In Progress on the board are excluded.
    /// </summary>
    /// <param name="workItem">The catalogue work item.</param>
    /// <param name="boardStatusName">The joined planning-board Status name, or <see langword="null"/> when unset.</param>
    /// <returns><see langword="true" /> when the item may be added to Up Next; otherwise, <see langword="false" />.</returns>
    public static bool IsCandidate(PmWorkItemDto workItem, string? boardStatusName)
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
            boardItemsByKey.TryAdd(PmWorkItemJoinKey.For(boardItem), boardItem);
        }

        return boardItemsByKey;
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildLabelsByKey(IReadOnlyList<PmWorkItemDto> workItems)
    {
        var labelsByKey = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var workItem in workItems)
        {
            labelsByKey[PmWorkItemJoinKey.For(workItem)] = workItem.Labels;
        }

        return labelsByKey;
    }

    private static bool IsCandidate(
        PmWorkItemDto workItem,
        IReadOnlyDictionary<string, ProjectBoardItemDto> boardItemsByKey)
    {
        boardItemsByKey.TryGetValue(PmWorkItemJoinKey.For(workItem), out var boardItem);
        return IsCandidate(workItem, boardItem?.Status?.Name);
    }

    private static IterationPlanningUpNextItemDto MapUpNextItem(
        ProjectBoardItemDto boardItem,
        IReadOnlyDictionary<string, IReadOnlyList<string>> labelsByKey)
    {
        labelsByKey.TryGetValue(PmWorkItemJoinKey.For(boardItem), out var labels);

        return new IterationPlanningUpNextItemDto(
            boardItem.ProjectItemId,
            boardItem.Content.ContentType == ProjectBoardItemContentTypeDto.PullRequest
                ? PmWorkItemTypeDto.PullRequest
                : PmWorkItemTypeDto.Issue,
            boardItem.Content.Number,
            boardItem.Content.Title,
            boardItem.Content.Url,
            $"{boardItem.Content.RepositoryOwner}/{boardItem.Content.RepositoryName}",
            boardItem.FocusOrder,
            labels ?? []);
    }

    private static IterationPlanningCandidateDto MapCandidate(
        PmWorkItemDto workItem,
        IReadOnlyDictionary<string, ProjectBoardItemDto> boardItemsByKey)
    {
        boardItemsByKey.TryGetValue(PmWorkItemJoinKey.For(workItem), out var boardItem);

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
