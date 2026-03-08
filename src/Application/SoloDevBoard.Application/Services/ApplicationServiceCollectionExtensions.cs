using Microsoft.Extensions.DependencyInjection;

namespace SoloDevBoard.Application.Services;

/// <summary>Extension methods for registering Application-layer services.</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers Application-layer services.</summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IRepositoryService, RepositoryService>();
        services.AddScoped<ILabelManagerService, LabelService>();

        return services;
    }
}
