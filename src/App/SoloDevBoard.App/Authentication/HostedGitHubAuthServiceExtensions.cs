using SoloDevBoard.Application.GitHub;
using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.App.Authentication;

/// <summary>Registers HttpClients used by <see cref="HostedGitHubAuthGateway"/>.</summary>
internal static class HostedGitHubAuthServiceExtensions
{
    /// <summary>Registers separate OAuth and GitHub API clients for hosted sign-in.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    internal static IServiceCollection AddHostedGitHubAuthHttpClients(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient(HostedGitHubAuthGateway.HostedGitHubOAuthClientName, static (serviceProvider, client) =>
        {
            var appVersionService = serviceProvider.GetRequiredService<IAppVersionService>();
            client.BaseAddress = new Uri("https://github.com");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appVersionService.UserAgent);
        });

        services.AddHttpClient(HostedGitHubAuthGateway.HostedGitHubApiClientName, static (serviceProvider, client) =>
        {
            var appVersionService = serviceProvider.GetRequiredService<IAppVersionService>();
            client.BaseAddress = new Uri("https://api.github.com");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appVersionService.UserAgent);
            GitHubApiHeaders.ApplyRestDefaults(client);
        });

        return services;
    }
}
