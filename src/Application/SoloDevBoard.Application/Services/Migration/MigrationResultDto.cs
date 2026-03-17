using SoloDevBoard.Application.Services.Labels;

namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents the migration apply result across labels and milestones.</summary>
/// <param name="ConflictStrategy">The selected conflict strategy used to apply migration.</param>
/// <param name="LabelResults">Per-repository label apply results.</param>
/// <param name="MilestoneResults">Per-repository milestone apply results.</param>
public sealed record MigrationResultDto(
    MigrationConflictStrategy ConflictStrategy,
    IReadOnlyList<LabelSyncRepositoryResultDto> LabelResults,
    IReadOnlyList<MilestoneSyncRepositoryResultDto> MilestoneResults);
