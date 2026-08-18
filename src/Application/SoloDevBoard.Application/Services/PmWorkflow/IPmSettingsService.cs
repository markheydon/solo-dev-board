namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Reads and writes Cross-Repo PM Workflow preferences.</summary>
public interface IPmSettingsService
{
    /// <summary>Loads persisted settings, falling back to defaults when storage is missing or invalid.</summary>
    /// <returns>The effective PM settings.</returns>
    Task<PmSettingsDto> GetSettingsAsync();

    /// <summary>Persists the supplied settings to browser storage.</summary>
    /// <param name="settings">The settings to store.</param>
    Task SaveSettingsAsync(PmSettingsDto settings);
}
