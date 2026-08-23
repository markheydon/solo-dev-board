using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Common;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Application.Services.Workflows;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;
using SoloDevBoard.Infrastructure.Labels;
using SoloDevBoard.Infrastructure.Migration;
using SoloDevBoard.Infrastructure.Milestones;
using SoloDevBoard.Infrastructure.Workflows;

namespace SoloDevBoard.Infrastructure.Common;

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
        services.Configure<DocsCaptureOptions>(configuration.GetSection(DocsCaptureOptions.SectionName));
        services.AddOptions<GitHubCacheOptions>()
            .Bind(configuration.GetSection(GitHubCacheOptions.SectionName))
            .PostConfigure(static options =>
            {
                if (options.RepositoriesTtlSeconds == 0)
                {
                    options.RepositoriesTtlSeconds = 60;
                }

                if (options.LabelsTtlSeconds == 0)
                {
                    options.LabelsTtlSeconds = 300;
                }

                if (options.MilestonesTtlSeconds == 0)
                {
                    options.MilestonesTtlSeconds = 300;
                }
            })
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<GitHubCacheOptions>, GitHubCacheOptionsValidator>();
        services.AddOptions<GitHubPaginationOptions>()
            .Bind(configuration.GetSection(GitHubPaginationOptions.SectionName))
            .PostConfigure(static options =>
            {
                if (options.WorkflowRunsMaxPages == 0)
                {
                    options.WorkflowRunsMaxPages = 1;
                }

                if (options.WorkflowRunsPerPage == 0)
                {
                    options.WorkflowRunsPerPage = 30;
                }
            })
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<GitHubPaginationOptions>, GitHubPaginationOptionsValidator>();
        services.AddMemoryCache();
        services.Configure<HostedAdmissionControlOptions>(configuration.GetSection(HostedAdmissionControlOptions.SectionName));
        services.AddSingleton<ResolvedPatOwnerLogin>();
        services.AddSingleton<GitHubPatOwnerLoginResolver>();
        services.AddHostedService<GitHubAuthConfigurationValidator>();
        services.AddHostedService<GitHubPatStartupInitializer>();
        services.AddHostedService<DocsCaptureStartupLogger>();
        services.AddHttpClient(GitHubPatOwnerLoginResolver.HttpClientName, static (serviceProvider, client) =>
        {
            var appVersionService = serviceProvider.GetRequiredService<IAppVersionService>();
            client.BaseAddress = new Uri("https://api.github.com");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appVersionService.UserAgent);
            GitHubApiHeaders.ApplyRestDefaults(client);
        });
        services.AddHttpContextAccessor();
        services.AddScoped<SingleUserCurrentUserContext>();
        services.AddScoped<HostedUserCurrentUserContext>();
        services.AddScoped<IHostedAdmissionEvaluator, AllowListHostedAdmissionEvaluator>();
        services.AddScoped<ICurrentUserContext>(static serviceProvider =>
        {
            var authOptions = serviceProvider.GetRequiredService<IOptions<GitHubAuthOptions>>().Value;

            return authOptions.HostedSignInEnabled
                ? serviceProvider.GetRequiredService<HostedUserCurrentUserContext>()
                : serviceProvider.GetRequiredService<SingleUserCurrentUserContext>();
        });
        services.AddScoped<IGitHubConnectivityStatusService, GitHubConnectivityStatusService>();
        services.AddTransient<GitHubAuthHandler>();
        services.AddHealthChecks()
            .AddCheck<GitHubPatConnectivityHealthCheck>(
                "github",
                tags: ["github", "ready"]);

        services
            .AddHttpClient(GitHubService.GitHubApiClientName, static (serviceProvider, client) =>
            {
                var appVersionService = serviceProvider.GetRequiredService<IAppVersionService>();
                client.BaseAddress = new Uri("https://api.github.com");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(appVersionService.UserAgent);
                GitHubApiHeaders.ApplyRestDefaults(client);
            })
            .AddHttpMessageHandler<GitHubAuthHandler>();

        services.AddScoped<GitHubResponseCache>();
        services.AddScoped<IGitHubService, GitHubService>();
        services.AddScoped<ILabelRepository, GitHubLabelRepository>();
        services.AddScoped<IMilestoneRepository, GitHubMilestoneRepository>();
        services.AddScoped<IProjectBoardStructureRepository, GitHubProjectBoardStructureRepository>();
        services.AddScoped<IWorkflowFileRepository, GitHubWorkflowFileRepository>();

        return services;
    }
}
