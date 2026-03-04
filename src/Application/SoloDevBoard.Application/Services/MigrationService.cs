using SoloDevBoard.Infrastructure;

namespace SoloDevBoard.Application.Services;

/// <summary>Stub implementation of <see cref="IMigrationService"/>.</summary>
public sealed class MigrationService : IMigrationService
{
    private readonly IGitHubService _gitHubService;

    public MigrationService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public Task MigrateRepositoryAsync(string sourceOwner, string sourceRepo, string targetOwner, CancellationToken cancellationToken = default)
    {
        // TODO: Implement repository migration logic.
        return Task.CompletedTask;
    }
}
