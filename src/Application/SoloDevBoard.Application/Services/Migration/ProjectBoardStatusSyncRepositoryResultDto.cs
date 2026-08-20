namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents a Projects v2 Status column synchronisation apply result for a target repository.</summary>
/// <param name="RepositoryFullName">The target repository in owner/repository format.</param>
/// <param name="CreatedCount">The number of Status options created.</param>
/// <param name="UpdatedCount">The number of Status options updated.</param>
/// <param name="DeletedCount">The number of Status options removed.</param>
/// <param name="SkippedCount">The number of Status options skipped.</param>
/// <param name="CreatedProjectId">The identifier of a newly created project board, when applicable.</param>
/// <param name="Warnings">Warning messages for operations that could not be completed safely.</param>
/// <param name="ErrorMessage">An error message when apply failed for this repository.</param>
public sealed record ProjectBoardStatusSyncRepositoryResultDto(
    string RepositoryFullName,
    int CreatedCount,
    int UpdatedCount,
    int DeletedCount,
    int SkippedCount,
    string? CreatedProjectId,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage)
{
    /// <summary>Gets a value indicating whether the synchronisation failed for this repository.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
