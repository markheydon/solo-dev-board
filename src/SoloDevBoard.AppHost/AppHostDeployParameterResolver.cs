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
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        var hyphenatedConfigurationValue = configuration[$"Parameters:{hyphenatedParameterName}"];
        if (!string.IsNullOrWhiteSpace(hyphenatedConfigurationValue))
        {
            return hyphenatedConfigurationValue;
        }

        var underscoredConfigurationValue = configuration[$"Parameters:{underscoredName}"];
        if (!string.IsNullOrWhiteSpace(underscoredConfigurationValue))
        {
            return underscoredConfigurationValue;
        }

        return defaultValue;
    }
}
