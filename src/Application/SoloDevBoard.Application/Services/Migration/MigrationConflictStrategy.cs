namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Defines conflict handling behaviour for migration preview and apply operations.</summary>
public enum MigrationConflictStrategy
{
    /// <summary>Creates missing items and skips existing conflicts.</summary>
    Skip,

    /// <summary>Creates missing items, updates conflicting items, and removes target-only items.</summary>
    Overwrite,

    /// <summary>Creates missing items and updates conflicting items while preserving target-only items.</summary>
    Merge,
}
