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
    public static string Resolve(IConfiguration configuration, string hyphenatedParameterName, string defaultValue = "-")
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
}
