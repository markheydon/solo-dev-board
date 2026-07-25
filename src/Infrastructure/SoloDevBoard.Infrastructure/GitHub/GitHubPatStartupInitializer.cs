using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Validates the configured personal access token and resolves owner login during PAT-mode startup.</summary>
public sealed class GitHubPatStartupInitializer(
    IOptions<GitHubAuthOptions> authOptions,
    ResolvedPatOwnerLogin resolvedPatOwnerLogin,
    GitHubPatOwnerLoginResolver ownerLoginResolver,
    ILogger<GitHubPatStartupInitializer> logger) : IHostedService
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly ResolvedPatOwnerLogin _resolvedPatOwnerLogin = resolvedPatOwnerLogin ?? throw new ArgumentNullException(nameof(resolvedPatOwnerLogin));
    private readonly GitHubPatOwnerLoginResolver _ownerLoginResolver = ownerLoginResolver ?? throw new ArgumentNullException(nameof(ownerLoginResolver));
    private readonly ILogger<GitHubPatStartupInitializer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_authOptions.HostedSignInEnabled)
        {
            return;
        }

        if (!AuthConfigurationPlaceholders.IsConfigured(_authOptions.PersonalAccessToken))
        {
            return;
        }

        var ownerLogin = await _ownerLoginResolver
            .ResolveAsync(_authOptions.PersonalAccessToken, cancellationToken)
            .ConfigureAwait(false);

        _resolvedPatOwnerLogin.Value = ownerLogin;
        _logger.LogInformation(
            "Verified GitHub personal access token connectivity for owner login '{OwnerLogin}'.",
            ownerLogin);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
