using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Common;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Composition;

namespace SoloDevBoard.Composition.Tests;

public sealed class SoloDevBoardServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSoloDevBoard_RegistersApplicationAndInfrastructureServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{Application.Authentication.GitHubAuthOptions.SectionName}:{nameof(Application.Authentication.GitHubAuthOptions.HostedSignInEnabled)}"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IAppVersionService>(new TestAppVersionService());

        // Act
        services.AddSoloDevBoard(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        // Assert
        Assert.NotNull(scope.ServiceProvider.GetService<ILabelManagerService>());
        Assert.NotNull(scope.ServiceProvider.GetService<IGitHubService>());
        Assert.NotNull(scope.ServiceProvider.GetService<ICurrentUserContext>());
    }

    private sealed class TestAppVersionService : IAppVersionService
    {
        public string Version => "1.0.0-test";

        public string BuildMetadata => "test-build";

        public string BuiltAtDisplay => string.Empty;

        public string UserAgent => "SoloDevBoard/1.0.0-test";
    }
}
