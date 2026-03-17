namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents which migration item types should be included.</summary>
/// <param name="IncludeLabels">A value indicating whether labels are included.</param>
/// <param name="IncludeMilestones">A value indicating whether milestones are included.</param>
public sealed record MigrationScopeDto(bool IncludeLabels, bool IncludeMilestones);