using Microsoft.Extensions.Configuration;

/// <summary>
/// Resolves whether the AppHost should model the full experience resource graph.
/// </summary>
internal static class AppHostPostureResolver
{
    /// <summary>
    /// Returns <see langword="true" /> when hosted sign-in is enabled (hosted graph: web compute only until inventory ships).
    /// </summary>
    /// <param name="configuration">AppHost configuration.</param>
    public static bool UseFullExperiencePosture(IConfiguration configuration) =>
        AppHostDeployParameterResolver.IsHostedSignInEnabled(configuration);
}
