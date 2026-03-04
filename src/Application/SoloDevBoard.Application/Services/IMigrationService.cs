namespace SoloDevBoard.Application.Services;

/// <summary>Provides repository migration operations.</summary>
public interface IMigrationService
{
    Task MigrateRepositoryAsync(string sourceOwner, string sourceRepo, string targetOwner, CancellationToken cancellationToken = default);
}
