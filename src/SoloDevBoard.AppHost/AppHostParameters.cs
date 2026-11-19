/// <summary>
/// Shared AppHost parameters for auth, admission, and optional dev Key Vault (Path B).
/// </summary>
internal sealed class AppHostParameters
{
    /// <summary>Vault name parameter for Path B and deploy.</summary>
    public IResourceBuilder<ParameterResource> AuthSecretsVaultName { get; init; } = null!;

    /// <summary>Vault resource group parameter for Path B and deploy.</summary>
    public IResourceBuilder<ParameterResource> AuthSecretsVaultResourceGroup { get; init; } = null!;

    /// <summary>When <see langword="true" />, Path B vault parameters are active.</summary>
    public bool PathBAuthSecretsVaultConfigured { get; init; }

    /// <summary>Hosted sign-in toggle parameter.</summary>
    public IResourceBuilder<ParameterResource> HostedSignInEnabled { get; init; } = null!;

    /// <summary>GitHub PAT secret parameter (Path A).</summary>
    public IResourceBuilder<ParameterResource> GithubPat { get; init; } = null!;

    /// <summary>GitHub App OAuth client ID parameter.</summary>
    public IResourceBuilder<ParameterResource> GhAppClientId { get; init; } = null!;

    /// <summary>GitHub App OAuth client secret parameter (Path A).</summary>
    public IResourceBuilder<ParameterResource> GhAppClientSecret { get; init; } = null!;

    /// <summary>Hosted admission control toggle parameter.</summary>
    public IResourceBuilder<ParameterResource> HostedAdmissionEnabled { get; init; } = null!;

    /// <summary>Allowed GitHub user logins for hosted admission.</summary>
    public IResourceBuilder<ParameterResource> AllowedUserLogins { get; init; } = null!;

    /// <summary>Allowed GitHub organisation logins for hosted admission.</summary>
    public IResourceBuilder<ParameterResource> AllowedOrgLogins { get; init; } = null!;

    /// <summary>Declares shared parameters on the distributed application builder.</summary>
    /// <param name="builder">AppHost builder.</param>
    public static AppHostParameters AddShared(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var resolvedAuthSecretsVaultName = AppHostDeployParameterResolver.Resolve(builder.Configuration, "auth-secrets-vault-name");
        var resolvedAuthSecretsVaultResourceGroup = AppHostDeployParameterResolver.Resolve(
            builder.Configuration,
            "auth-secrets-vault-resource-group");
        if (!AppHostDeployParameterResolver.IsActiveParameterValue(resolvedAuthSecretsVaultResourceGroup))
        {
            resolvedAuthSecretsVaultResourceGroup = AppHostDeployParameterResolver.ResolveAuthSecretsVaultResourceGroup(builder.Configuration);
        }

        var authSecretsVaultName = builder.AddParameter("auth-secrets-vault-name", resolvedAuthSecretsVaultName)
            .WithDescription(
                "Azure Key Vault name for auth secret fallback (local Path B) and deploy. " +
                "Use '-' for local Path A only. Staging defaults to auth-secrets-staging; Production to auth-secrets.");

        var authSecretsVaultResourceGroup = builder.AddParameter("auth-secrets-vault-resource-group", resolvedAuthSecretsVaultResourceGroup)
            .WithDescription(
                "Resource group that owns the auth-secrets vault. Use '-' to inherit Azure__ResourceGroup at deploy or local Path B.");

        var pathBAuthSecretsVaultConfigured = AppHostDeployParameterResolver.HasConfiguredAuthSecretsVault(builder.Configuration);

        var hostedSignInEnabled = builder.AddParameter("hosted-sign-in-enabled")
            .WithDescription("Enable GitHub App hosted sign-in at /auth/sign-in. When false, PAT-only local trusted mode is used (default).");

        var githubPat = pathBAuthSecretsVaultConfigured
            ? builder.AddParameter(
                "gh-pat",
                AppHostDeployParameterResolver.Resolve(builder.Configuration, "gh-pat", "-"),
                secret: true)
            : builder.AddParameter("gh-pat", secret: true);
        githubPat = githubPat.WithDescription(
            "GitHub PAT for PAT-only local trusted mode. Set to '-' via the dashboard for hosted sign-in. Your GitHub login is resolved automatically from the token.");

        var ghAppClientId = builder.AddParameter("gh-app-client-id")
            .WithDescription("GitHub App OAuth client ID for hosted sign-in. Set to '-' for PAT-only local trusted mode.");

        var ghAppClientSecret = pathBAuthSecretsVaultConfigured
            ? builder.AddParameter(
                "gh-app-client-secret",
                AppHostDeployParameterResolver.Resolve(builder.Configuration, "gh-app-client-secret", "-"),
                secret: true)
            : builder.AddParameter("gh-app-client-secret", secret: true);
        ghAppClientSecret = ghAppClientSecret.WithDescription(
            "GitHub App OAuth client secret for hosted sign-in. Set to '-' via the dashboard for PAT-only local trusted mode.");

        var hostedAdmissionEnabled = builder.AddParameter("hosted-admission-enabled")
            .WithDescription("Enable deny-by-default admission control for hosted sign-in.");

        var allowedUserLogins = builder.AddParameter("allowed-user-logins")
            .WithDescription(
                "Comma-separated GitHub user logins for hosted admission. Use '-' when using allowed-org-logins instead, or in PAT-only local trusted mode.");

        var allowedOrgLogins = builder.AddParameter("allowed-org-logins")
            .WithDescription(
                "Comma-separated GitHub organisation logins for hosted admission. Use '-' when using allowed-user-logins instead, or in PAT-only local trusted mode.");

        return new AppHostParameters
        {
            AuthSecretsVaultName = authSecretsVaultName,
            AuthSecretsVaultResourceGroup = authSecretsVaultResourceGroup,
            PathBAuthSecretsVaultConfigured = pathBAuthSecretsVaultConfigured,
            HostedSignInEnabled = hostedSignInEnabled,
            GithubPat = githubPat,
            GhAppClientId = ghAppClientId,
            GhAppClientSecret = ghAppClientSecret,
            HostedAdmissionEnabled = hostedAdmissionEnabled,
            AllowedUserLogins = allowedUserLogins,
            AllowedOrgLogins = allowedOrgLogins,
        };
    }
}
