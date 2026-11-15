namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a taxonomy preview for a single repository.</summary>
/// <param name="RepositoryFullName">The owner/repository full name.</param>
/// <param name="ToCreate">Labels that would be created.</param>
/// <param name="ToUpdate">Labels that would be updated.</param>
/// <param name="ToDelete">Unused extras that would be deleted without remapping when strict mode is enabled.</param>
/// <param name="ToRemap">Extras attached to issues or pull requests that need a remap decision when strict mode is enabled.</param>
/// <param name="Skipped">Labels already matching the strategy exactly.</param>
/// <param name="KeptAreaLabels">Labels kept because they use the <c>area/</c> prefix when remove-outside is enabled.</param>
public sealed record RecommendedTaxonomyRepositoryPreviewDto(
    string RepositoryFullName,
    IReadOnlyList<LabelDto> ToCreate,
    IReadOnlyList<LabelDto> ToUpdate,
    IReadOnlyList<LabelDto> ToDelete,
    IReadOnlyList<LabelDto> ToRemap,
    IReadOnlyList<LabelDto> Skipped,
    IReadOnlyList<LabelDto> KeptAreaLabels);
