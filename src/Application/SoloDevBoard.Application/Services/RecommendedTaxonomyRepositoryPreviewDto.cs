namespace SoloDevBoard.Application.Services;

/// <summary>Represents a taxonomy preview for a single repository.</summary>
/// <param name="RepositoryFullName">The owner/repository full name.</param>
/// <param name="ToCreate">Labels that would be created.</param>
/// <param name="ToUpdate">Labels that would be updated.</param>
/// <param name="Skipped">Labels already matching the strategy exactly.</param>
public sealed record RecommendedTaxonomyRepositoryPreviewDto(
    string RepositoryFullName,
    IReadOnlyList<LabelDto> ToCreate,
    IReadOnlyList<LabelDto> ToUpdate,
    IReadOnlyList<LabelDto> Skipped);
