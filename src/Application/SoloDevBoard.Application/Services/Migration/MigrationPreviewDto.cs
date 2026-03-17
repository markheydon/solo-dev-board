using SoloDevBoard.Application.Services.Labels;

namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents the migration preview across labels and milestones.</summary>
/// <param name="ConflictStrategy">The selected conflict strategy used to generate the preview.</param>
/// <param name="LabelPreviews">Per-repository label previews.</param>
/// <param name="MilestonePreviews">Per-repository milestone previews.</param>
public sealed record MigrationPreviewDto(
    MigrationConflictStrategy ConflictStrategy,
    IReadOnlyList<LabelSyncRepositoryPreviewDto> LabelPreviews,
    IReadOnlyList<MilestoneSyncRepositoryPreviewDto> MilestonePreviews);
