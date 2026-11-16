using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.GitHub;
using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>Registers inventory infrastructure services.</summary>
public static class InventoryServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<InventoryOptions>(configuration.GetSection(InventoryOptions.SectionName));
        services.AddScoped<IOperatorKeyResolver, OperatorKeyResolver>();
        services.AddSingleton<IOperatorInventoryTargetProvider, OperatorInventoryTargetProvider>();
        services.AddScoped<IInventoryRunRepository, EfInventoryRunRepository>();
        services.AddScoped<ICanonicalFundingStandardRepository, EfCanonicalFundingStandardRepository>();
        services.AddScoped<IGitHubOpenSourceInventoryScanner, GitHubOpenSourceInventoryScanner>();
        services.AddScoped<InventoryGitHubClient>();
        services.AddScoped<IInventoryCatalogueClient, RepositoryCatalogueInventoryClient>();
        services.AddScoped<IInventoryOperatorCredentials, InventoryOperatorCredentials>();
        services.AddSingleton<IGitHubInstallationTokenProvider, GitHubInstallationTokenProvider>();

        services.AddHttpClient(InventoryGitHubClient.HttpClientName, static (serviceProvider, client) =>
        {
            var appVersionService = serviceProvider.GetRequiredService<SoloDevBoard.Application.Services.Common.IAppVersionService>();
            client.BaseAddress = new Uri("https://api.github.com");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appVersionService.UserAgent);
            GitHubApiHeaders.ApplyRestDefaults(client);
        });

        services.AddHttpClient("GitHubAppJwtClient");

        return services;
    }
}