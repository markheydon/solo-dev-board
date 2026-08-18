using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Services.Audit;
using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Application.Services.Common;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Triage;
using SoloDevBoard.Application.Services.Workflows;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="ApplicationServiceCollectionExtensions"/>.</summary>
public sealed class ApplicationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplicationServices_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddApplicationServices();

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void AddApplicationServices_ValidServices_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        AssertServiceRegistration<IAppVersionService, AppVersionService>(services, ServiceLifetime.Singleton);
        AssertServiceRegistration<IRepositoryService, RepositoryService>(services, ServiceLifetime.Scoped);
        AssertServiceRegistration<ILabelManagerService, LabelService>(services, ServiceLifetime.Scoped);
        AssertServiceRegistration<IMigrationService, MigrationService>(services, ServiceLifetime.Scoped);
        AssertServiceRegistration<IAuditDashboardService, AuditDashboardService>(services, ServiceLifetime.Scoped);
        AssertServiceRegistration<IAuditDashboardMarkdownExporter, AuditDashboardMarkdownExporter>(services, ServiceLifetime.Singleton);
        AssertServiceRegistration<IBoardRulesService, BoardRulesService>(services, ServiceLifetime.Scoped);
        AssertServiceRegistration<ITriageService, TriageService>(services, ServiceLifetime.Scoped);
        AssertServiceRegistration<IWorkflowTemplateService, WorkflowTemplateService>(services, ServiceLifetime.Scoped);
        AssertServiceRegistration<IPmSettingsService, PmSettingsService>(services, ServiceLifetime.Scoped);
    }

    private static void AssertServiceRegistration<TService, TImplementation>(
        IServiceCollection services,
        ServiceLifetime lifetime)
    {
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(TService));
        Assert.Equal(lifetime, descriptor.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
    }
}
