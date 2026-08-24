namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Reads and writes Cross-Repo Planning preferences.</summary>
public interface IPlanningSettingsService
{
    /// <summary>Loads persisted settings, falling back to defaults when storage is missing or invalid.</summary>
    /// <returns>The effective PM settings.</returns>
    Task<PlanningSettingsDto> GetSettingsAsync();

    /// <summary>Persists the supplied settings to browser storage.</summary>
    /// <param name="settings">The settings to store.</param>
    Task SaveSettingsAsync(PlanningSettingsDto settings);
}
