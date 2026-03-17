using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Common;
using SoloDevBoard.Infrastructure.Common;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class InfrastructureServiceExtensionsTests
{
    [Fact]
    public void AddInfrastructureServices_HostedSignInEnabled_ResolvesHostedUserCurrentUserContext()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.HostedSignInEnabled)}"] = "true",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IAppVersionService>(new TestAppVersionService());

        // Act
        services.AddInfrastructureServices(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var currentUserContext = scope.ServiceProvider.GetRequiredService<ICurrentUserContext>();
        var admissionEvaluator = scope.ServiceProvider.GetRequiredService<IHostedAdmissionEvaluator>();

        // Assert
        Assert.IsType<HostedUserCurrentUserContext>(currentUserContext);
        Assert.IsType<AllowListHostedAdmissionEvaluator>(admissionEvaluator);
    }

    [Fact]
    public void AddInfrastructureServices_HostedSignInDisabled_ResolvesSingleUserCurrentUserContext()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.HostedSignInEnabled)}"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IAppVersionService>(new TestAppVersionService());

        // Act
        services.AddInfrastructureServices(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var currentUserContext = scope.ServiceProvider.GetRequiredService<ICurrentUserContext>();

        // Assert
        Assert.IsType<SingleUserCurrentUserContext>(currentUserContext);
    }
}

internal sealed class TestAppVersionService : IAppVersionService
{
    public string Version => "1.0.0-test";

    public string UserAgent => "SoloDevBoard/1.0.0-test";
}
