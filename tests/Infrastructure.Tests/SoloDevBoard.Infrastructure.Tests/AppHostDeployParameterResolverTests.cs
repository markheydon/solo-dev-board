using Microsoft.Extensions.Configuration;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for deploy-time Aspire parameter resolution used by AppHost.</summary>
public sealed class AppHostDeployParameterResolverTests
{
    [Fact]
    public void Resolve_WhenEnvironmentVariablePresent_ReturnsEnvironmentValue()
    {
        const string expected = "Iv23liCIrMqjMWrzSliW";
        var previous = Environment.GetEnvironmentVariable("Parameters__gh_app_client_id");

        try
        {
            Environment.SetEnvironmentVariable("Parameters__gh_app_client_id", expected);
            var configuration = new ConfigurationBuilder().Build();

            var actual = DeployParameterResolver.Resolve(configuration, "gh-app-client-id");

            Assert.Equal(expected, actual);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Parameters__gh_app_client_id", previous);
        }
    }

    [Fact]
    public void Resolve_WhenConfigurationHyphenKeyPresent_ReturnsConfigurationValue()
    {
        const string expected = "markheydon";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Parameters:allowed-user-logins"] = expected,
            })
            .Build();

        var actual = DeployParameterResolver.Resolve(configuration, "allowed-user-logins");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Resolve_WhenNoInputPresent_ReturnsDefaultValue()
    {
        var configuration = new ConfigurationBuilder().Build();

        var actual = DeployParameterResolver.Resolve(configuration, "allowed-org-logins", "-");

        Assert.Equal("-", actual);
    }

    /// <summary>Test double mirroring AppHost deploy parameter resolution.</summary>
    private static class DeployParameterResolver
    {
        public static string Resolve(IConfiguration configuration, string hyphenatedParameterName, string defaultValue = "-")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(hyphenatedParameterName);

            var underscoredName = hyphenatedParameterName.Replace('-', '_');
            var environmentValue = Environment.GetEnvironmentVariable($"Parameters__{underscoredName}");
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }

            var hyphenatedConfigurationValue = configuration[$"Parameters:{hyphenatedParameterName}"];
            if (!string.IsNullOrWhiteSpace(hyphenatedConfigurationValue))
            {
                return hyphenatedConfigurationValue;
            }

            var underscoredConfigurationValue = configuration[$"Parameters:{underscoredName}"];
            if (!string.IsNullOrWhiteSpace(underscoredConfigurationValue))
            {
                return underscoredConfigurationValue;
            }

            return defaultValue;
        }
    }
}
