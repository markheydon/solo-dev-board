using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Probes GitHub API connectivity using the configured personal access token.</summary>
public sealed class GitHubPatConnectivityHealthCheck(
    IOptions<GitHubAuthOptions> authOptions,
    GitHubPatOwnerLoginResolver ownerLoginResolver) : IHealthCheck
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly GitHubPatOwnerLoginResolver _ownerLoginResolver = ownerLoginResolver ?? throw new ArgumentNullException(nameof(ownerLoginResolver));

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_authOptions.HostedSignInEnabled)
        {
            return HealthCheckResult.Healthy("Hosted sign-in is enabled; PAT connectivity probe is not applicable.");
        }

        if (!AuthConfigurationPlaceholders.IsConfigured(_authOptions.PersonalAccessToken))
        {
            return HealthCheckResult.Unhealthy(
                "GitHub personal access token is not configured for PAT-only local trusted mode.");
        }

        try
        {
            var login = await _ownerLoginResolver
                .ResolveAsync(_authOptions.PersonalAccessToken, cancellationToken)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy($"GitHub PAT connectivity verified for @{login}.");
        }
        catch (InvalidOperationException ex)
        {
            return HealthCheckResult.Unhealthy("GitHub rejected the configured personal access token.", ex);
        }
    }
}
