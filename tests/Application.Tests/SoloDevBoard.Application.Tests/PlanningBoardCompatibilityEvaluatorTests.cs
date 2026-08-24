using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="PlanningBoardCompatibilityEvaluator"/>.</summary>
public sealed class PlanningBoardCompatibilityEvaluatorTests
{
    [Fact]
    public void Evaluate_WhenBoardHasRequiredColumns_ReturnsNoIssues()
    {
        var report = PlanningBoardCompatibilityEvaluator.Evaluate(
            "PVT_board",
            new ProjectBoardFieldIdsDto("field_status", "field_focus"),
            CreateFullStatusOptions());

        Assert.False(report.HasIssues);
    }

    [Fact]
    public void Evaluate_WhenTodoStatusMissing_ReturnsErrorIssue()
    {
        var report = PlanningBoardCompatibilityEvaluator.Evaluate(
            "PVT_board",
            new ProjectBoardFieldIdsDto("field_status", "field_focus"),
            CreateFullStatusOptions().Where(option => !option.Name.Equals("Todo", StringComparison.OrdinalIgnoreCase)).ToArray());

        Assert.Contains(
            report.Issues,
            issue => issue.Code == "missing-status-todo"
                && issue.Severity == PlanningBoardCompatibilitySeverity.Error);
    }

    [Fact]
    public void Evaluate_WhenFocusOrderFieldMissing_ReturnsWarningIssue()
    {
        var report = PlanningBoardCompatibilityEvaluator.Evaluate(
            "PVT_board",
            new ProjectBoardFieldIdsDto("field_status", null),
            CreateFullStatusOptions());

        Assert.Contains(
            report.Issues,
            issue => issue.Code == "missing-focus-order-field"
                && issue.Severity == PlanningBoardCompatibilitySeverity.Warning);
    }

    [Fact]
    public void Evaluate_WhenMultipleIssuesMissing_OrdersErrorsBeforeWarnings()
    {
        var report = PlanningBoardCompatibilityEvaluator.Evaluate(
            "PVT_board",
            new ProjectBoardFieldIdsDto("field_status", null),
            []);

        Assert.True(report.HasIssues);
        Assert.Equal(PlanningBoardCompatibilitySeverity.Error, report.Issues[0].Severity);
        Assert.Contains(report.Issues, issue => issue.Severity == PlanningBoardCompatibilitySeverity.Warning);
    }

    [Fact]
    public void CreateLoadFailureReport_ReturnsSingleErrorIssue()
    {
        var report = PlanningBoardCompatibilityEvaluator.CreateLoadFailureReport("PVT_board");

        Assert.True(report.HasIssues);
        Assert.Single(report.Issues);
        Assert.Equal("compatibility-check-failed", report.Issues[0].Code);
        Assert.Equal(PlanningBoardCompatibilitySeverity.Error, report.Issues[0].Severity);
    }

    private static IReadOnlyList<ProjectBoardStatusOptionDto> CreateFullStatusOptions() =>
    [
        new("opt_todo", PlanningBoardStatusResolver.TodoStatusName),
        new("opt_up_next", DailyFocusBoardStateMapper.UpNextStatusName),
        new("opt_in_progress", DailyFocusBoardStateMapper.InProgressStatusName),
        new("opt_blocked", DailyFocusRecommendationMapper.BlockedStatusName),
        new("opt_ice_box", DailyFocusRecommendationMapper.IceBoxStatusName),
    ];
}
