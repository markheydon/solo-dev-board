namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a label synchronisation result for a target repository.</summary>
/// <param name="RepositoryFullName">The target repository in owner/repository format.</param>
/// <param name="CreatedCount">The number of labels created.</param>
/// <param name="UpdatedCount">The number of labels updated.</param>
/// <param name="DeletedCount">The number of labels deleted.</param>
/// <param name="SkippedCount">The number of labels skipped because they already matched exactly.</param>
/// <param name="ErrorMessage">The repository-specific error message when synchronisation fails.</param>
public sealed record LabelSyncRepositoryResultDto(
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
