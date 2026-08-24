namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Compatibility report for a selected planning board.</summary>
/// <param name="BoardId">The GitHub Project v2 node identifier.</param>
/// <param name="Issues">Issues discovered on the board, ordered by severity then title.</param>
public sealed record PlanningBoardCompatibilityReportDto(
    string BoardId,
    IReadOnlyList<PlanningBoardCompatibilityIssueDto> Issues)
{
    /// <summary>Gets a value indicating whether any issues were discovered.</summary>
    public bool HasIssues => Issues.Count > 0;
}
