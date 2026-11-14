using Microsoft.Extensions.Configuration;

/// <summary>
/// Resolves Aspire AppHost parameter values from CD environment variables and configuration.
/// </summary>
internal static class AppHostDeployParameterResolver
{
    /// <summary>
    /// Resolves a deploy-time parameter using <c>Parameters__*</c> environment variables and configuration keys.
    /// </summary>
    /// <param name="configuration">AppHost configuration.</param>
    /// <param name="hyphenatedParameterName">Parameter name as declared in AppHost (for example <c>gh-app-client-id</c>).</param>
    /// <param name="defaultValue">Value when no deploy input is present.</param>
    /// <returns>The resolved parameter value.</returns>
    public static string Resolve(
        IConfiguration configuration,
        string hyphenatedParameterName,
        string defaultValue = "-")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(hyphenatedParameterName);

        var underscoredName = hyphenatedParameterName.Replace('-', '_');
        var environmentValue = Environment.GetEnvironmentVariable($"Parameters__{underscoredName}");
        if (IsActiveParameterValue(environmentValue))
        {
            return environmentValue!.Trim();
        }

        var hyphenatedConfigurationValue = configuration[$"Parameters:{hyphenatedParameterName}"];
        if (IsActiveParameterValue(hyphenatedConfigurationValue))
        {
            return hyphenatedConfigurationValue!.Trim();
        }

        var underscoredConfigurationValue = configuration[$"Parameters:{underscoredName}"];
        if (IsActiveParameterValue(underscoredConfigurationValue))
        {
            return underscoredConfigurationValue!.Trim();
        }

        return defaultValue;
    }

    /// <summary>Returns <see langword="true" /> when a deploy parameter value is active (not unset or <c>-</c>).</summary>
    public static bool IsActiveParameterValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !string.Equals(value.Trim(), "-", StringComparison.Ordinal);

    /// <summary>Returns <see langword="true" /> when hosted sign-in is enabled from resolved parameters.</summary>
    public static bool IsHostedSignInEnabled(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var value = Resolve(configuration, "hosted-sign-in-enabled", "false");
        return bool.TryParse(value, out var enabled) && enabled;
    }

    /// <summary>Resolves the auth-secrets vault resource group from parameters or <c>Azure__ResourceGroup</c>.</summary>
    public static string ResolveAuthSecretsVaultResourceGroup(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var resolved = Resolve(configuration, "auth-secrets-vault-resource-group");
        if (IsActiveParameterValue(resolved))
        {
            return resolved!.Trim();
        }

        var azureResourceGroup = Environment.GetEnvironmentVariable("Azure__ResourceGroup");
        if (IsActiveParameterValue(azureResourceGroup))
        {
            return azureResourceGroup!.Trim();
        }

        return "-";
    }

    /// <summary>Returns <see langword="true" /> when vault name and resource group are both active.</summary>
    public static bool HasConfiguredAuthSecretsVault(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var vaultName = Resolve(configuration, "auth-secrets-vault-name");
        var vaultResourceGroup = ResolveAuthSecretsVaultResourceGroup(configuration);
        return IsActiveParameterValue(vaultName) && IsActiveParameterValue(vaultResourceGroup);
    }

    /// <summary>
    /// Ensures deploy can reference an existing Key Vault. Call from publish mode before provisioning.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when vault name or resource group is missing.</exception>
    public static void EnsureAuthSecretsVaultConfigured(IConfiguration configuration)
    {
        if (HasConfiguredAuthSecretsVault(configuration))
        {
            return;
        }

        throw new InvalidOperationException(
            "Deploy requires Parameters:auth-secrets-vault-name and a resource group " +
            "(Parameters:auth-secrets-vault-resource-group or Azure__ResourceGroup). " +
            "Populate the tier Key Vault before deploy. See docs/azure-key-vault-setup.md.");
    }

    /// <summary>
    /// Ensures both optional deploy parameters are active together or neither is active.
    /// </summary>
    /// <param name="firstValue">Resolved value for the first parameter.</param>
    /// <param name="secondValue">Resolved value for the second parameter.</param>
    /// <param name="firstParameterName">First parameter name for error messages.</param>
    /// <param name="secondParameterName">Second parameter name for error messages.</param>
    /// <exception cref="InvalidOperationException">Thrown when exactly one parameter is active.</exception>
    public static void EnsurePairOrNeither(
        string firstValue,
        string secondValue,
        string firstParameterName,
        string secondParameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstParameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(secondParameterName);

        var firstActive = IsActiveParameterValue(firstValue);
        var secondActive = IsActiveParameterValue(secondValue);
        if (firstActive == secondActive)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Deploy parameters '{firstParameterName}' and '{secondParameterName}' must both be set or both omitted. " +
            $"Set Parameters__{firstParameterName.Replace('-', '_')} and Parameters__{secondParameterName.Replace('-', '_')} together, or leave both unset for Aspire's default registry.");
    }
}
