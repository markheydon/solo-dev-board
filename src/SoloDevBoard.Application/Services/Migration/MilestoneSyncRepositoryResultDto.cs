namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents a milestone synchronisation result for a target repository.</summary>
/// <param name="RepositoryFullName">The target repository in owner/repository format.</param>
/// <param name="CreatedCount">The number of milestones created.</param>
/// <param name="UpdatedCount">The number of milestones updated.</param>
/// <param name="DeletedCount">The number of milestones deleted.</param>
/// <param name="SkippedCount">The number of milestones skipped.</param>
/// <param name="ErrorMessage">The repository-specific error message when synchronisation fails.</param>
public sealed record MilestoneSyncRepositoryResultDto(
    string RepositoryFullName,
    int CreatedCount,
    int UpdatedCount,
    int DeletedCount,
    int SkippedCount,
    string? ErrorMessage)
{
    /// <summary>Gets a value indicating whether the synchronisation failed for this repository.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
