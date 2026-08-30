using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Services.Common;
using SoloDevBoard.Infrastructure.Common;

namespace SoloDevBoard.Composition;

/// <summary>Extension methods for registering SoloDevBoard services with the DI container.</summary>
public static class SoloDevBoardServiceCollectionExtensions
{
    /// <summary>
    /// Registers Application and Infrastructure services. Call this from each host composition root
    /// (for example Blazor Server or a future Functions worker).
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">Application configuration used for options binding.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSoloDevBoard(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration);

        return services;
    }
}
