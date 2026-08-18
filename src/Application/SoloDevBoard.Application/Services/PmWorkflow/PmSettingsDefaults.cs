namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default values for <see cref="PmSettingsDto"/>.</summary>
public static class PmSettingsDefaults
{
    /// <summary>Default planning capacity limit.</summary>
    public const int Capacity = 8;

    /// <summary>Default stall threshold in days.</summary>
    public const int StallDays = 3;

    /// <summary>Default neglect threshold in days.</summary>
    public const int NeglectDays = 14;

    /// <summary>Creates a new settings DTO with repository defaults.</summary>
    /// <returns>A default <see cref="PmSettingsDto"/> instance.</returns>
    public static PmSettingsDto Create() => new(
        PlanningBoardNodeId: null,
        ExcludedRepositories: [],
        Capacity: Capacity,
        StallDays: StallDays,
        NeglectDays: NeglectDays);
}
