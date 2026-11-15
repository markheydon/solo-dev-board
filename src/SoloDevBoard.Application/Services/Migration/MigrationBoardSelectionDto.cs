namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents source and per-target project board selections for column migration.</summary>
/// <param name="SourceProjectId">The source project board identifier.</param>
/// <param name="TargetSelections">The per-target board selections.</param>
public sealed record MigrationBoardSelectionDto(
    string SourceProjectId,
    IReadOnlyList<MigrationTargetBoardSelectionDto> TargetSelections);
