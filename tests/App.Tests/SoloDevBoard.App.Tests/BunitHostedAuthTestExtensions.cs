using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.App.Authentication;

namespace SoloDevBoard.App.Tests;

/// <summary>Test double for hosted authentication recovery that never redirects.</summary>
internal sealed class TestHostedAuthenticationRecoveryService : IHostedAuthenticationRecoveryService
{
    /// <inheritdoc/>
    public bool TryInitiateRecovery(Exception exception, string? returnUrl = null) => false;
}

/// <summary>Registers hosted authentication test services with bUnit contexts.</summary>
internal static class BunitHostedAuthTestExtensions
{
    /// <summary>Registers a no-op hosted authentication recovery service.</summary>
    /// <param name="services">The bUnit service collection.</param>
    public static void AddTestHostedAuthenticationRecovery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IHostedAuthenticationRecoveryService, TestHostedAuthenticationRecoveryService>();
    }
}
