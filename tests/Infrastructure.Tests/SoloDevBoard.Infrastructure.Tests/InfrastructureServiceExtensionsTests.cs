using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    [Fact]
    public void AddInfrastructureServices_RegistersGitHubResponseCache()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IAppVersionService>(new TestAppVersionService());

        // Act
        services.AddInfrastructureServices(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        // Assert
        Assert.NotNull(scope.ServiceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>());
        Assert.NotNull(scope.ServiceProvider.GetService<GitHubResponseCache>());
    }

    [Fact]
    public void AddInfrastructureServices_InvalidGitHubCacheTtl_ThrowsOnStartup()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{GitHubCacheOptions.SectionName}:{nameof(GitHubCacheOptions.RepositoriesTtlSeconds)}"] = "-1",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IAppVersionService>(new TestAppVersionService());
        services.AddInfrastructureServices(configuration);

        // Act / Assert
        using var serviceProvider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(
            () => _ = serviceProvider.GetRequiredService<IOptions<GitHubCacheOptions>>().Value);
    }

    [Fact]
    public void AddInfrastructureServices_RegistersGitHubApiClientWithRestHeaders()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IAppVersionService>(new TestAppVersionService());
        services.AddInfrastructureServices(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var apiClient = httpClientFactory.CreateClient(GitHubService.GitHubApiClientName);
        var patResolverClient = httpClientFactory.CreateClient(GitHubPatOwnerLoginResolver.HttpClientName);

        Assert.Contains(
            apiClient.DefaultRequestHeaders.Accept,
            header => header.MediaType == GitHubApiHeaders.JsonAcceptMediaType);
        Assert.Equal(
            GitHubApiHeaders.ApiVersion,
            apiClient.DefaultRequestHeaders.GetValues("X-GitHub-Api-Version").Single());

        Assert.Contains(
            patResolverClient.DefaultRequestHeaders.Accept,
            header => header.MediaType == GitHubApiHeaders.JsonAcceptMediaType);
        Assert.Equal(
            GitHubApiHeaders.ApiVersion,
            patResolverClient.DefaultRequestHeaders.GetValues("X-GitHub-Api-Version").Single());
    }

    [Fact]
    public void AddInfrastructureServices_InvalidGitHubPaginationOptions_ThrowsOnStartup()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{GitHubPaginationOptions.SectionName}:{nameof(GitHubPaginationOptions.WorkflowRunsMaxPages)}"] = "-1",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IAppVersionService>(new TestAppVersionService());
        services.AddInfrastructureServices(configuration);

        // Act / Assert
        using var serviceProvider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(
            () => _ = serviceProvider.GetRequiredService<IOptions<GitHubPaginationOptions>>().Value);
    }
}

internal sealed class TestAppVersionService : IAppVersionService
{
    public string Version => "1.0.0-test";

    public string BuildMetadata => "test-build";

    public string UserAgent => "SoloDevBoard/1.0.0-test";
}
