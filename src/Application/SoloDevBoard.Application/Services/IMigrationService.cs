namespace SoloDevBoard.Application.Services;

/// <summary>Provides repository migration operations.</summary>
public interface IMigrationService
{
    /// <summary>Migrates a repository from a source owner to a target owner.</summary>
    /// <param name="sourceOwner">The source GitHub account owner login.</param>
    /// <param name="sourceRepo">The source repository name.</param>
    /// <param name="targetOwner">The target GitHub account owner login.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous migration operation.</returns>
    Task MigrateRepositoryAsync(string sourceOwner, string sourceRepo, string targetOwner, CancellationToken cancellationToken = default);
}
