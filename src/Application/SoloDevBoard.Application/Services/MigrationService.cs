namespace SoloDevBoard.Application.Services;

/// <summary>Stub implementation of <see cref="IMigrationService"/>.</summary>
public sealed class MigrationService : IMigrationService
{
    /// <inheritdoc/>
    public Task MigrateRepositoryAsync(string sourceOwner, string sourceRepo, string targetOwner, CancellationToken cancellationToken = default)
    {
        // TODO: Implement repository migration logic.
        return Task.CompletedTask;
    }
}
