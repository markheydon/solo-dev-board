namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Evaluates whether a project board exposes the Status options and fields Planning expects.</summary>
public static class PlanningBoardCompatibilityEvaluator
{
    private static readonly (string StatusName, PlanningBoardCompatibilitySeverity Severity, string Title, string Detail)[] RequiredStatusChecks =
    [
        (
            DailyFocusBoardStateMapper.UpNextStatusName,
            PlanningBoardCompatibilitySeverity.Error,
            $"Missing {DailyFocusBoardStateMapper.UpNextStatusName} status column",
            $"Planning uses a Status option named {DailyFocusBoardStateMapper.UpNextStatusName} for active load, stalled Up Next detection, and Add to Up Next on Iteration. Add an {DailyFocusBoardStateMapper.UpNextStatusName} column on the project board or rename an existing column to match exactly."),
        (
            PlanningBoardStatusResolver.TodoStatusName,
            PlanningBoardCompatibilitySeverity.Error,
            $"Missing {PlanningBoardStatusResolver.TodoStatusName} status column",
            $"Iteration Planning uses a Status option named {PlanningBoardStatusResolver.TodoStatusName} for Re-commit and Remove on stalled Up Next items. Add a {PlanningBoardStatusResolver.TodoStatusName} column on the project board or rename an existing column to match exactly."),
        (
            DailyFocusBoardStateMapper.InProgressStatusName,
            PlanningBoardCompatibilitySeverity.Warning,
            $"Missing {DailyFocusBoardStateMapper.InProgressStatusName} status column",
            $"Daily Focus active load counts items in {DailyFocusBoardStateMapper.UpNextStatusName} and {DailyFocusBoardStateMapper.InProgressStatusName}. Without an {DailyFocusBoardStateMapper.InProgressStatusName} column, in-flight work is omitted from the capacity meter."),
        (
            DailyFocusRecommendationMapper.BlockedStatusName,
            PlanningBoardCompatibilitySeverity.Warning,
            $"Missing {DailyFocusRecommendationMapper.BlockedStatusName} status column",
            $"Iteration Planning Mark Blocked and Backlog Review blocked/deferred grouping expect a Status option named {DailyFocusRecommendationMapper.BlockedStatusName}."),
        (
            DailyFocusRecommendationMapper.IceBoxStatusName,
            PlanningBoardCompatibilitySeverity.Warning,
            $"Missing {DailyFocusRecommendationMapper.IceBoxStatusName} status column",
            $"Iteration Planning Ice Box and Backlog Review blocked/deferred grouping expect a Status option named {DailyFocusRecommendationMapper.IceBoxStatusName}."),
    ];

    /// <summary>
    /// Builds a report when the board catalogue could not be loaded for compatibility evaluation.
    /// </summary>
    /// <param name="boardId">The GitHub Project v2 node identifier.</param>
    /// <returns>A compatibility report containing a single load-failure issue.</returns>
    public static PlanningBoardCompatibilityReportDto CreateLoadFailureReport(string boardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);

        return new PlanningBoardCompatibilityReportDto(
            boardId,
            [
                new PlanningBoardCompatibilityIssueDto(
                    "compatibility-check-failed",
                    PlanningBoardCompatibilitySeverity.Error,
                    "Could not check board setup",
                    "Planning could not load the selected project board to verify Status columns and fields. "
                    + "Select Recheck on the Board setup tab or Refresh on the planning chrome to try again."),
            ]);
    }

    /// <summary>
    /// Evaluates discovered board fields and Status options against Planning expectations.
    /// </summary>
    /// <param name="boardId">The GitHub Project v2 node identifier.</param>
    /// <param name="fieldIds">Discovered field identifiers for the board.</param>
    /// <param name="statusOptions">Discovered Status options for the board.</param>
    /// <returns>A compatibility report for the board.</returns>
    public static PlanningBoardCompatibilityReportDto Evaluate(
        string boardId,
        ProjectBoardFieldIdsDto fieldIds,
        IReadOnlyList<ProjectBoardStatusOptionDto> statusOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);
        ArgumentNullException.ThrowIfNull(fieldIds);
        ArgumentNullException.ThrowIfNull(statusOptions);

        var issues = new List<PlanningBoardCompatibilityIssueDto>();

        if (string.IsNullOrWhiteSpace(fieldIds.StatusFieldId))
        {
            issues.Add(new PlanningBoardCompatibilityIssueDto(
                "missing-status-field",
                PlanningBoardCompatibilitySeverity.Error,
                "Missing Status field",
                "Planning requires a single-select Status field on the project board. Add a Status field in GitHub Projects settings before using this board."));
        }

        foreach (var check in RequiredStatusChecks)
        {
            if (HasStatusOption(statusOptions, check.StatusName))
            {
                continue;
            }

            issues.Add(new PlanningBoardCompatibilityIssueDto(
                $"missing-status-{NormalizeCode(check.StatusName)}",
                check.Severity,
                check.Title,
                check.Detail));
        }

        if (string.IsNullOrWhiteSpace(fieldIds.FocusOrderFieldId))
        {
            issues.Add(new PlanningBoardCompatibilityIssueDto(
                "missing-focus-order-field",
                PlanningBoardCompatibilitySeverity.Warning,
                "Missing Focus Order field",
                "Iteration Planning can still move items to Up Next, but stories, enablers, and tests will not receive sequential Focus Order until the board exposes a number field named Focus Order."));
        }

        return new PlanningBoardCompatibilityReportDto(
            boardId,
            issues
                .OrderBy(issue => issue.Severity)
                .ThenBy(issue => issue.Title, StringComparer.Ordinal)
                .ToArray());
    }

    private static bool HasStatusOption(
        IReadOnlyList<ProjectBoardStatusOptionDto> statusOptions,
        string statusName) =>
        statusOptions.Any(option => option.Name.Equals(statusName, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeCode(string statusName) =>
        statusName.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant();
}
