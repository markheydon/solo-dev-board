namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents the outcome of a preview-first recommended taxonomy apply with remap extras.</summary>
/// <param name="ApplyResults">Per-repository create, update, delete, and skip summaries.</param>
/// <param name="RemapResults">Per-label remap outcomes for extras that were remapped or deleted without remap.</param>
public sealed record RecommendedTaxonomyRemapApplyResultDto(
    IReadOnlyList<RecommendedTaxonomyRepositoryResultDto> ApplyResults,
    IReadOnlyList<LabelRemapResultDto> RemapResults);
