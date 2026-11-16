using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>Enumerates unattended inventory targets for Functions.</summary>
public sealed class OperatorInventoryTargetProvider(
    IOptions<GitHubAuthOptions> authOptions,
    ResolvedPatOwnerLogin resolvedPatOwnerLogin) : IOperatorInventoryTargetProvider
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly ResolvedPatOwnerLogin _resolvedPatOwnerLogin = resolvedPatOwnerLogin ?? throw new ArgumentNullException(nameof(resolvedPatOwnerLogin));

    public Task<IReadOnlyList<OperatorInventoryTarget>> GetTargetsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_authOptions.HostedSignInEnabled)
        {
            if (!long.TryParse(_authOptions.InstallationId, out var installationId))
            {
                return Task.FromResult<IReadOnlyList<OperatorInventoryTarget>>([]);
            }

            if (!AuthConfigurationPlaceholders.IsConfigured(_authOptions.GitHubAppId)
                || !AuthConfigurationPlaceholders.IsConfigured(_authOptions.GitHubAppPrivateKey))
            {
                return Task.FromResult<IReadOnlyList<OperatorInventoryTarget>>([]);
            }

            var key = FormattableString.Invariant($"installation:{installationId}");
            return Task.FromResult<IReadOnlyList<OperatorInventoryTarget>>(
                [new OperatorInventoryTarget(key, UsesInstallationToken: true, installationId)]);
        }

        var ownerLogin = !string.IsNullOrWhiteSpace(_authOptions.OwnerLogin)
            ? _authOptions.OwnerLogin
            : _resolvedPatOwnerLogin.Value;
        if (string.IsNullOrWhiteSpace(ownerLogin))
        {
            return Task.FromResult<IReadOnlyList<OperatorInventoryTarget>>([]);
        }

        var patKey = FormattableString.Invariant($"user:{ownerLogin}");
        return Task.FromResult<IReadOnlyList<OperatorInventoryTarget>>(
            [new OperatorInventoryTarget(patKey, UsesInstallationToken: false, null)]);
    }
}