namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>DTO for issue or pull request content linked to a project board item.</summary>
public sealed record ProjectBoardItemContentDto(
    ProjectBoardItemContentTypeDto ContentType,
    int Number,
    string RepositoryOwner,
    string RepositoryName,
    string Title,
    string Url);
