namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Classifies catalogue work items into Backlog Review urgency groups.</summary>
public static class BacklogReviewGrouping
{
    /// <summary>
    /// Groups work items into urgent, ready-to-start, awaiting-triage, blocked or deferred, near-complete epics,
    /// and neglected repositories.
    /// </summary>
    /// <param name="workItems">Open issues and pull requests from included repositories.</param>
    /// <param name="boardItems">Items currently on the selected planning board.</param>
    /// <param name="repositorySummaries">Per-repository open-work summaries from the catalogue.</param>
    /// <param name="failures">Per-repository catalogue failures to carry through to the result.</param>
    /// <param name="neglectDays">Inclusive days without issue or pull request activity before a repository is neglected.</param>
    /// <param name="referenceTimeUtc">The UTC instant used for neglect-day comparisons.</param>
    /// <returns>The grouped Backlog Review snapshot.</returns>
    public static BacklogReviewResultDto Group(
        IReadOnlyList<PmWorkItemDto> workItems,
        IReadOnlyList<ProjectBoardItemDto> boardItems,
        IReadOnlyList<PmRepositorySummaryDto> repositorySummaries,
        IReadOnlyList<PmRepositoryCatalogueFailureDto> failures,
        int neglectDays,
        DateTimeOffset referenceTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(workItems);
        ArgumentNullException.ThrowIfNull(boardItems);
        ArgumentNullException.ThrowIfNull(repositorySummaries);
        ArgumentNullException.ThrowIfNull(failures);

        var boardStatusByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var boardItem in boardItems)
        {
            var statusName = boardItem.Status?.Name;
            if (string.IsNullOrWhiteSpace(statusName))
            {
                continue;
            }

            // Duplicate join keys are unlikely; keep the first board status encountered.
            boardStatusByKey.TryAdd(PmWorkItemJoinKey.For(boardItem), statusName);
        }

        var urgent = new List<BacklogReviewItemDto>();
        var ready = new List<BacklogReviewItemDto>();
        var awaitingTriage = new List<BacklogReviewItemDto>();
        var blocked = new List<BacklogReviewItemDto>();
        var epicsNearComplete = new List<BacklogEpicNearCompleteItemDto>();

        foreach (var workItem in workItems)
        {
            boardStatusByKey.TryGetValue(PmWorkItemJoinKey.For(workItem), out var boardStatusName);
            var row = Map(workItem, boardStatusName);

            if (IsUrgent(workItem))
            {
                urgent.Add(row);
            }

            if (IsAwaitingTriage(workItem))
            {
                awaitingTriage.Add(row);
            }
            else if (IsReadyToStart(workItem, boardStatusName))
            {
                ready.Add(row);
            }

            if (IsBlockedOrDeferred(workItem, boardStatusName))
            {
                blocked.Add(row);
            }

            if (TryMapEpicNearComplete(workItem, out var epicNearComplete))
            {
                epicsNearComplete.Add(epicNearComplete);
            }
        }

        var neglectedRepositories = BuildNeglectedRepositories(repositorySummaries, neglectDays, referenceTimeUtc);
        var subIssueCountsUnavailable = HasOpenEpicsWithoutSubIssueCounts(workItems);

        return new BacklogReviewResultDto(
            Sort(urgent),
            Sort(ready),
            Sort(awaitingTriage),
            Sort(blocked),
            SortEpics(epicsNearComplete),
            neglectedRepositories,
            subIssueCountsUnavailable,
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

    /// <summary>Returns whether the item is missing a core <c>type/</c> or <c>priority/</c> label.</summary>
    /// <param name="item">The catalogue work item.</param>
    /// <returns><see langword="true"/> when either core label prefix is absent.</returns>
    public static bool IsAwaitingTriage(PmWorkItemDto item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return PmLabelHelpers.IsAwaitingTriage(item.Labels);
    }

    /// <summary>
    /// Returns whether the item is ready to start: fully labelled, unblocked by labels and board Status,
    /// not urgent, and not already Up Next or In Progress.
    /// </summary>
    /// <param name="item">The catalogue work item.</param>
    /// <param name="boardStatusName">The joined planning-board Status name, or <see langword="null"/> when unset.</param>
    /// <returns><see langword="true" /> when the item can be started; otherwise, <see langword="false" />.</returns>
    public static bool IsReadyToStart(PmWorkItemDto item, string? boardStatusName)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (IsAwaitingTriage(item) || IsUrgent(item))
        {
            return false;
        }

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

    /// <summary>
    /// Returns whether a repository has had no issue or pull request activity within the neglect threshold.
    /// </summary>
    /// <param name="summary">The repository summary from the work-item catalogue.</param>
    /// <param name="neglectDays">The inclusive neglect threshold in days.</param>
    /// <param name="referenceTimeUtc">The UTC instant used for the comparison.</param>
    /// <returns><see langword="true"/> when the repository is neglected; otherwise, <see langword="false"/>.</returns>
    public static bool IsNeglected(
        PmRepositorySummaryDto summary,
        int neglectDays,
        DateTimeOffset referenceTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(summary);

        if (neglectDays <= 0)
        {
            return false;
        }

        if (summary.LastActivityAt == default)
        {
            return true;
        }

        var daysSinceActivity = (referenceTimeUtc - summary.LastActivityAt).TotalDays;
        return daysSinceActivity >= neglectDays;
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

    private static bool TryMapEpicNearComplete(
        PmWorkItemDto item,
        out BacklogEpicNearCompleteItemDto epicNearComplete)
    {
        epicNearComplete = null!;

        if (!PmLabelHelpers.IsEpicNearComplete(item.Labels, item.SubIssueTotal, item.SubIssueCompleted))
        {
            return false;
        }

        var typeLabel = PmLabelHelpers.ParseTypeLabel(item.Labels)!;
        epicNearComplete = new BacklogEpicNearCompleteItemDto(
            item.RepositoryFullName,
            item.Number,
            item.Title,
            item.HtmlUrl,
            typeLabel,
            item.SubIssueTotal!.Value,
            item.SubIssueCompleted!.Value);
        return true;
    }

    private static bool HasOpenEpicsWithoutSubIssueCounts(IReadOnlyList<PmWorkItemDto> workItems)
    {
        var openEpicsOrFeatures = workItems
            .Where(static item => item.ItemType == PmWorkItemTypeDto.Issue)
            .Select(static item => (Item: item, TypeLabel: PmLabelHelpers.ParseTypeLabel(item.Labels)))
            .Where(static pair => pair.TypeLabel is PmLabelHelpers.EpicTypeLabel or PmLabelHelpers.FeatureTypeLabel)
            .ToArray();

        if (openEpicsOrFeatures.Length == 0)
        {
            return false;
        }

        return openEpicsOrFeatures.All(static pair => pair.Item.SubIssueTotal is null);
    }

    private static IReadOnlyList<BacklogNeglectedRepositoryDto> BuildNeglectedRepositories(
        IReadOnlyList<PmRepositorySummaryDto> repositorySummaries,
        int neglectDays,
        DateTimeOffset referenceTimeUtc)
        => repositorySummaries
            .Where(summary => summary.IsIncluded && IsNeglected(summary, neglectDays, referenceTimeUtc))
            .Select(static summary => new BacklogNeglectedRepositoryDto(
                summary.FullName,
                summary.LastActivityAt == default ? null : summary.LastActivityAt,
                summary.OpenIssueCount,
                summary.OpenPullRequestCount))
            .OrderBy(static repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

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

    private static IReadOnlyList<BacklogEpicNearCompleteItemDto> SortEpics(
        IReadOnlyList<BacklogEpicNearCompleteItemDto> items)
        => items
            .OrderBy(static item => item.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Number)
            .ToArray();
}
