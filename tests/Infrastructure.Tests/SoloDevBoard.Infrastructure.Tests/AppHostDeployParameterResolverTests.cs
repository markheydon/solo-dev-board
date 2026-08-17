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

    [Fact]
    public void EnsurePairOrNeither_WhenBothInactive_DoesNotThrow()
    {
        DeployParameterResolver.EnsurePairOrNeither("-", "-", "acr-name", "acr-resource-group");
    }

    [Fact]
    public void EnsurePairOrNeither_WhenBothActive_DoesNotThrow()
    {
        DeployParameterResolver.EnsurePairOrNeither("myacr", "rg-shared", "acr-name", "acr-resource-group");
    }

    [Fact]
    public void EnsurePairOrNeither_WhenOnlyFirstActive_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DeployParameterResolver.EnsurePairOrNeither("myacr", "-", "acr-name", "acr-resource-group"));

        Assert.Contains("acr-name", exception.Message, StringComparison.Ordinal);
        Assert.Contains("acr-resource-group", exception.Message, StringComparison.Ordinal);
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

        public static bool IsActiveParameterValue(string? value) =>
            !string.IsNullOrWhiteSpace(value) && !string.Equals(value.Trim(), "-", StringComparison.Ordinal);

        public static void EnsurePairOrNeither(
            string firstValue,
            string secondValue,
            string firstParameterName,
            string secondParameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(firstParameterName);
            ArgumentException.ThrowIfNullOrWhiteSpace(secondParameterName);

            var firstActive = IsActiveParameterValue(firstValue);
            var secondActive = IsActiveParameterValue(secondValue);
            if (firstActive == secondActive)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Deploy parameters '{firstParameterName}' and '{secondParameterName}' must both be set or both omitted. " +
                $"Set Parameters__{firstParameterName.Replace('-', '_')} and Parameters__{secondParameterName.Replace('-', '_')} together, or leave both unset for Aspire's default registry.");
        }
    }
}
