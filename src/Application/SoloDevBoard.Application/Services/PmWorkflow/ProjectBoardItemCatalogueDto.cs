namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>DTO catalogue of project board items and discovered field identifiers.</summary>
public sealed record ProjectBoardItemCatalogueDto(
    ProjectBoardFieldIdsDto FieldIds,
    IReadOnlyList<ProjectBoardItemDto> Items);
