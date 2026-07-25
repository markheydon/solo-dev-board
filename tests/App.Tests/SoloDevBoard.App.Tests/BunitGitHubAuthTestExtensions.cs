using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.App.Authentication;

namespace SoloDevBoard.App.Tests;

/// <summary>Test double for GitHub authentication recovery that never redirects.</summary>
internal sealed class TestGitHubAuthenticationRecoveryService : IGitHubAuthenticationRecoveryService
{
    /// <inheritdoc/>
    public bool TryInitiateRecovery(Exception exception, string? returnUrl = null) => false;
}

/// <summary>Registers GitHub authentication test services with bUnit contexts.</summary>
internal static class BunitGitHubAuthTestExtensions
{
    /// <summary>Registers a no-op GitHub authentication recovery service.</summary>
    /// <param name="services">The bUnit service collection.</param>
    public static void AddTestGitHubAuthenticationRecovery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IGitHubAuthenticationRecoveryService, TestGitHubAuthenticationRecoveryService>();
    }
}
