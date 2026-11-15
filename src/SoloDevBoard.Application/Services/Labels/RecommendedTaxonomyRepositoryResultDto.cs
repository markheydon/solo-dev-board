namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents the taxonomy apply result for a single repository.</summary>
/// <param name="RepositoryFullName">The owner/repository full name.</param>
/// <param name="CreatedCount">The number of labels created.</param>
/// <param name="UpdatedCount">The number of labels updated.</param>
/// <param name="DeletedCount">The number of labels deleted.</param>
/// <param name="SkippedCount">The number of labels skipped because they already matched.</param>
/// <param name="DeleteErrors">Per-label delete failures when strict mode is enabled.</param>
/// <param name="ErrorMessage">The repository-specific error message when apply fails.</param>
public sealed record RecommendedTaxonomyRepositoryResultDto(
    string RepositoryFullName,
    int CreatedCount,
    int UpdatedCount,
    int DeletedCount,
    int SkippedCount,
    IReadOnlyList<RecommendedTaxonomyLabelDeleteErrorDto> DeleteErrors,
    string? ErrorMessage)
{
    /// <summary>Gets a value indicating whether the apply operation failed for this repository.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage) || DeleteErrors.Count > 0;
}
