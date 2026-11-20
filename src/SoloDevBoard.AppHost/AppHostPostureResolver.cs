using Microsoft.Extensions.Configuration;

/// <summary>
/// Resolves whether the AppHost should use the hosted sign-in graph (web compute only).
/// </summary>
internal static class AppHostPostureResolver
{
    /// <summary>
    /// Returns <see langword="true" /> when hosted sign-in is enabled (<c>hosted-sign-in-enabled</c>).
    /// </summary>
    /// <param name="configuration">AppHost configuration.</param>
    public static bool UseHostedSignInPosture(IConfiguration configuration) =>
        AppHostDeployParameterResolver.IsHostedSignInEnabled(configuration);
}
