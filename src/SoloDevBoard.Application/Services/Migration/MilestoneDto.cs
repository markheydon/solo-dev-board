namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents a milestone at the Application-to-App boundary.</summary>
/// <param name="Id">The unique GitHub milestone identifier.</param>
/// <param name="Number">The repository-scoped milestone number.</param>
/// <param name="Title">The milestone title.</param>
/// <param name="Description">The milestone description.</param>
/// <param name="State">The milestone state.</param>
/// <param name="DueOn">The milestone due date, if configured.</param>
/// <param name="OpenIssues">The number of open issues.</param>
/// <param name="ClosedIssues">The number of closed issues.</param>
public sealed record MilestoneDto(
    int Id,
    int Number,
    string Title,
    string Description,
    string State,
    DateTimeOffset? DueOn,
    int OpenIssues,
    int ClosedIssues);
