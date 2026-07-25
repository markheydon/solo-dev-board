using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace SoloDevBoard.ServiceDefaults.Tests;

public sealed class HealthEndpointTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task MapDefaultEndpoints_InConfiguredEnvironment_ExposesHealthyReadinessAndLivenessEndpoints(string environment)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
        });
        builder.AddDefaultHealthChecks();

        var app = builder.Build();
        app.MapDefaultEndpoints();

        await app.StartAsync(cancellationToken);
        try
        {
            var baseAddress = new Uri(app.Urls.First());
            using var client = new HttpClient { BaseAddress = baseAddress };

            var healthResponse = await client.GetAsync("/health", cancellationToken);
            var aliveResponse = await client.GetAsync("/alive", cancellationToken);

            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, aliveResponse.StatusCode);

            var healthBody = await healthResponse.Content.ReadAsStringAsync(cancellationToken);
            var aliveBody = await aliveResponse.Content.ReadAsStringAsync(cancellationToken);

            Assert.Contains("Healthy", healthBody, StringComparison.Ordinal);
            Assert.Contains("Healthy", aliveBody, StringComparison.Ordinal);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task MapDefaultEndpoints_NonLiveCheckUnhealthy_LivenessRemainsHealthyWhileReadinessFails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.Services.AddHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("dependency", static () => HealthCheckResult.Unhealthy("Dependency unavailable."), ["ready"]);

        var app = builder.Build();
        app.MapDefaultEndpoints();

        await app.StartAsync(cancellationToken);
        try
        {
            var baseAddress = new Uri(app.Urls.First());
            using var client = new HttpClient { BaseAddress = baseAddress };

            var healthResponse = await client.GetAsync("/health", cancellationToken);
            var aliveResponse = await client.GetAsync("/alive", cancellationToken);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, aliveResponse.StatusCode);

            var healthBody = await healthResponse.Content.ReadAsStringAsync(cancellationToken);
            var aliveBody = await aliveResponse.Content.ReadAsStringAsync(cancellationToken);
            Assert.Equal("Unhealthy", healthBody);
            Assert.Contains("Healthy", aliveBody, StringComparison.Ordinal);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }
}
