using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace SoloDevBoard.ServiceDefaults.Tests;

/// <summary>Tests for structured logging configuration in <see cref="Extensions"/>.</summary>
public sealed class StructuredLoggingConfigurationTests
{
    [Fact]
    public void ConfigureStructuredLogging_ProductionEnvironment_RegistersJsonConsoleProviderWithExpectedOptions()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });

        builder.ConfigureStructuredLogging();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<ILoggerProvider>().ToList();
        var formatterOptions = serviceProvider.GetRequiredService<IOptions<JsonConsoleFormatterOptions>>().Value;

        Assert.Single(providers);
        Assert.IsType<ConsoleLoggerProvider>(providers[0]);
        Assert.True(formatterOptions.IncludeScopes);
        Assert.True(formatterOptions.UseUtcTimestamp);
        Assert.Equal("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", formatterOptions.TimestampFormat);
    }

    [Fact]
    public void ConfigureStructuredLogging_DevelopmentEnvironment_PreservesDefaultLoggingProviders()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });

        builder.ConfigureStructuredLogging();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<ILoggerProvider>().ToList();

        Assert.True(providers.Count > 1);
    }
}
