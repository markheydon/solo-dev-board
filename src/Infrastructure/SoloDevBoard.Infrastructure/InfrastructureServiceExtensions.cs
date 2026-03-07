using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure;

/// <summary>Extension methods for registering Infrastructure services with the DI container.</summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure-layer services. Call this from the application's
    /// composition root (i.e. <c>Program.cs</c>) during startup configuration.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">Application configuration used for options binding.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<GitHubAuthOptions>(configuration.GetSection(GitHubAuthOptions.SectionName));
        // Application-wide current user context for the single-user scenario.
        services.AddSingleton<ICurrentUserContext, SingleUserCurrentUserContext>();
        services.AddTransient<GitHubAuthHandler>();

        services
            .AddHttpClient(GitHubService.GitHubApiClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.github.com");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SoloDevBoard/0.1");
            })
            .AddHttpMessageHandler<GitHubAuthHandler>();

        services.AddScoped<IGitHubService, GitHubService>();

        return services;
    }
}
