namespace SoloDevBoard.Domain.Entities.Planning;

/// <summary>Catalogue of project board items and discovered field identifiers for PM workflow.</summary>
public sealed record ProjectBoardItemCatalogue
{
    /// <summary>Gets the discovered Status and optional Focus Order field identifiers.</summary>
    public ProjectBoardFieldIds FieldIds { get; init; } = new();

    /// <summary>Gets the Status options discovered on the board, in board-defined order.</summary>
    public IReadOnlyList<ProjectBoardStatusOption> StatusOptions { get; init; } = [];

    /// <summary>Gets the project board items included in the catalogue.</summary>
    public IReadOnlyList<ProjectBoardItem> Items { get; init; } = [];
}
