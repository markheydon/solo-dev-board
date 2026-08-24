namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a taxonomy preview for a single repository.</summary>
/// <param name="RepositoryFullName">The owner/repository full name.</param>
/// <param name="ToCreate">Labels that would be created.</param>
/// <param name="ToUpdate">Labels that would be updated.</param>
/// <param name="ToDelete">Labels that would be deleted when strict mode is enabled.</param>
/// <param name="Skipped">Labels already matching the strategy exactly.</param>
/// <param name="KeptAreaLabels">Labels kept because they use the <c>area/</c> prefix when remove-outside is enabled.</param>
public sealed record RecommendedTaxonomyRepositoryPreviewDto(
    string RepositoryFullName,
    IReadOnlyList<LabelDto> ToCreate,
    IReadOnlyList<LabelDto> ToUpdate,
    IReadOnlyList<LabelDto> ToDelete,
    IReadOnlyList<LabelDto> Skipped,
    IReadOnlyList<LabelDto> KeptAreaLabels);
