namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Reads and writes serialised PM settings from browser storage.</summary>
public interface IPmSettingsStorage
{
    /// <summary>Gets the stored JSON payload.</summary>
    /// <returns>The stored JSON, or <see langword="null"/> when unset.</returns>
    Task<string?> GetStoredJsonAsync();

    /// <summary>Persists the JSON payload.</summary>
    /// <param name="json">The serialised settings JSON.</param>
    Task SetStoredJsonAsync(string json);
}
