namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>DTO for a GitHub Project v2 board item in the PM workflow catalogue.</summary>
public sealed record ProjectBoardItemDto(
    string ProjectItemId,
    ProjectBoardItemStatusDto? Status,
    double? FocusOrder,
    ProjectBoardItemContentDto Content,
    DateTimeOffset ActivityTimestamp);
