using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.Infrastructure;

/// <summary>Extension methods for registering Infrastructure services with the DI container.</summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure-layer services. Call this from the application's
    /// composition root (i.e. <c>Program.cs</c>) during startup configuration.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient();
        services.AddScoped<IGitHubService, GitHubService>();

        return services;
    }
}
