namespace SoloDevBoard.Application.GitHub;

/// <summary>Mints and caches GitHub App installation access tokens.</summary>
public interface IGitHubInstallationTokenProvider
{
    Task<string> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken);
}