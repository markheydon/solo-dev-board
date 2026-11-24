using SoloDevBoard.Application.Services.Planning;

namespace SoloDevBoard.Infrastructure.Planning;

/// <summary>No-op planning settings storage for non-interactive hosts (for example the Functions worker).</summary>
public sealed class WorkerPlanningSettingsStorage : IPlanningSettingsStorage
{
    /// <inheritdoc/>
    public Task<string?> GetStoredJsonAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    /// <inheritdoc/>
    public Task SetStoredJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Task.CompletedTask;
    }
}
