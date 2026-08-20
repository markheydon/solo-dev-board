namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents the board selection for a single target repository.</summary>
/// <param name="RepositoryFullName">The target repository in owner/repository format.</param>
/// <param name="TargetProjectId">The selected target project board identifier, or <see langword="null"/> when creating a new board.</param>
/// <param name="NewBoardTitle">The title for a newly created board when <paramref name="TargetProjectId"/> is <see langword="null"/>.</param>
public sealed record MigrationTargetBoardSelectionDto(
    string RepositoryFullName,
    string? TargetProjectId,
    string? NewBoardTitle);
