using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.GitHub;
using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.App.Tests;

public sealed class HostedGitHubAuthServiceExtensionsTests
{
    [Fact]
    public void AddHostedGitHubAuthHttpClients_RegistersOAuthAndApiClientsWithExpectedHeaders()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAppVersionService>(new TestAppVersionService());
        services.AddHostedGitHubAuthHttpClients();
        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var oauthClient = httpClientFactory.CreateClient(HostedGitHubAuthGateway.HostedGitHubOAuthClientName);
        var apiClient = httpClientFactory.CreateClient(HostedGitHubAuthGateway.HostedGitHubApiClientName);

        Assert.Equal(new Uri("https://github.com"), oauthClient.BaseAddress);
        Assert.Contains(
            oauthClient.DefaultRequestHeaders.Accept,
            header => header.MediaType == "application/json");

        Assert.Equal(new Uri("https://api.github.com"), apiClient.BaseAddress);
        Assert.Contains(
            apiClient.DefaultRequestHeaders.Accept,
            header => header.MediaType == GitHubApiHeaders.JsonAcceptMediaType);
        Assert.Equal(
            GitHubApiHeaders.ApiVersion,
            apiClient.DefaultRequestHeaders.GetValues("X-GitHub-Api-Version").Single());
    }

    private sealed class TestAppVersionService : IAppVersionService
    {
        public string Version => "1.0.0-test";

        public string BuildMetadata => "test-build";

        public string BuiltAtDisplay => string.Empty;

        public string UserAgent => "SoloDevBoard/1.0.0-test";
    }
}
