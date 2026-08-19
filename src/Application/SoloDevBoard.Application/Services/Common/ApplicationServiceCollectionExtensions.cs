using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoloDevBoard.Application.Services.Audit;
using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Triage;
using SoloDevBoard.Application.Services.Workflows;

namespace SoloDevBoard.Application.Services.Common;

/// <summary>Extension methods for registering Application-layer services.</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers Application-layer services.</summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAppVersionService, AppVersionService>();
        services.AddScoped<IRepositoryService, RepositoryService>();
        services.AddScoped<ILabelManagerService, LabelService>();
        services.AddScoped<IMigrationService, MigrationService>();
        services.AddScoped<IAuditDashboardService, AuditDashboardService>();
        services.AddSingleton<IAuditDashboardMarkdownExporter, AuditDashboardMarkdownExporter>();
        services.AddScoped<IBoardRulesService, BoardRulesService>();
        services.AddScoped<ITriageService, TriageService>();
        services.AddScoped<IWorkflowTemplateService, WorkflowTemplateService>();
        services.AddScoped<IPmSettingsService, PmSettingsService>();
        services.AddScoped<IProjectItemCatalogueService, ProjectItemCatalogueService>();
        services.AddScoped<IPmWorkItemCatalogueService, PmWorkItemCatalogueService>();
        services.AddScoped<IPmProjectBoardDiscoveryService, PmProjectBoardDiscoveryService>();
        services.AddScoped<IDailyFocusBoardStateService, DailyFocusBoardStateService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IDailyFocusRecommendationService, DailyFocusRecommendationService>();

        return services;
    }
}
