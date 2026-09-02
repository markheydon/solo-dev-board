namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Default values for <see cref="PlanningSettingsDto"/>.</summary>
public static class PlanningSettingsDefaults
{
    /// <summary>Default planning capacity limit.</summary>
    public const int Capacity = 8;

    /// <summary>Default stall threshold in days.</summary>
    public const int StallDays = 3;

    /// <summary>Default neglect threshold in days.</summary>
    public const int NeglectDays = 14;

    /// <summary>Creates a new settings DTO with repository defaults.</summary>
    /// <returns>A default <see cref="PlanningSettingsDto"/> instance.</returns>
    public static PlanningSettingsDto Create() => new(
        PlanningBoardNodeId: null,
        ExcludedRepositories: [],
        Capacity: Capacity,
        StallDays: StallDays,
        NeglectDays: NeglectDays,
        LimitRecommendationsToPlanningBoard: false);
}
