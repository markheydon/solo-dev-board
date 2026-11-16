using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.GitHub;
using SoloDevBoard.Application.Services.OpenSource;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>Scoped operator credentials for a single inventory message.</summary>
public sealed class InventoryOperatorCredentials(
    IOptions<GitHubAuthOptions> authOptions,
    IGitHubInstallationTokenProvider installationTokenProvider) : IInventoryOperatorCredentials
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly IGitHubInstallationTokenProvider _installationTokenProvider = installationTokenProvider ?? throw new ArgumentNullException(nameof(installationTokenProvider));

    public string OperatorKey { get; private set; } = string.Empty;

    public bool UsesInstallationToken { get; private set; }

    public long? InstallationId { get; private set; }

    public void Configure(OperatorInventoryTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        OperatorKey = target.OperatorKey;
        UsesInstallationToken = target.UsesInstallationToken;
        InstallationId = target.InstallationId;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (UsesInstallationToken)
        {
            if (InstallationId is null)
            {
                throw new InvalidOperationException("Installation id is required for hosted inventory.");
            }

            return await _installationTokenProvider.GetInstallationTokenAsync(InstallationId.Value, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(_authOptions.PersonalAccessToken))
        {
            throw new InvalidOperationException("GitHub personal access token is not configured.");
        }

        return _authOptions.PersonalAccessToken;
    }
}