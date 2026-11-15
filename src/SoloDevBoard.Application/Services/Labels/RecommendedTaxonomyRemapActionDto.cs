namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents one remap decision for an extra label during recommended taxonomy apply.</summary>
/// <param name="SourceName">The extra label to remap or delete.</param>
/// <param name="DestinationName">The destination label, or <see langword="null" /> when the source is kept.</param>
/// <param name="DeleteWithoutRemap">Whether the source should be deleted without retagging.</param>
public sealed record RecommendedTaxonomyRemapActionDto(
    string SourceName,
    string? DestinationName,
    bool DeleteWithoutRemap);
