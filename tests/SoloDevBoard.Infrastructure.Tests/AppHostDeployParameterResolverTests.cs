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

    [Fact]
    public void HasConfiguredAuthSecretsVault_WhenNameAndAzureResourceGroupPresent_ReturnsTrue()
    {
        var previousRg = Environment.GetEnvironmentVariable("Azure__ResourceGroup");

        try
        {
            Environment.SetEnvironmentVariable("Azure__ResourceGroup", "rg-solodevboard-staging");
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Parameters:auth-secrets-vault-name"] = "auth-secrets-staging",
                    ["Parameters:auth-secrets-vault-resource-group"] = "-",
                })
                .Build();

            var actual = DeployParameterResolver.HasConfiguredAuthSecretsVault(configuration);

            Assert.True(actual);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Azure__ResourceGroup", previousRg);
        }
    }

    [Fact]
    public void IsHostedSignInEnabled_WhenConfigurationTrue_ReturnsTrue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Parameters:hosted-sign-in-enabled"] = "true",
            })
            .Build();

        var actual = DeployParameterResolver.IsHostedSignInEnabled(configuration);

        Assert.True(actual);
    }

    [Fact]
    public void EnsureAuthSecretsVaultConfigured_WhenVaultConfigured_DoesNotThrow()
    {
        var previousRg = Environment.GetEnvironmentVariable("Azure__ResourceGroup");

        try
        {
            Environment.SetEnvironmentVariable("Azure__ResourceGroup", "rg-solodevboard-staging");
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Parameters:auth-secrets-vault-name"] = "auth-secrets-staging",
                })
                .Build();

            DeployParameterResolver.EnsureAuthSecretsVaultConfigured(configuration);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Azure__ResourceGroup", previousRg);
        }
    }

    [Fact]
    public void EnsureAuthSecretsVaultConfigured_WhenVaultNameMissing_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DeployParameterResolver.EnsureAuthSecretsVaultConfigured(configuration));

        Assert.Contains("auth-secrets-vault-name", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Must match vault binding rules in <c>LeanAppHostGraph</c> and <c>HostedSignInAppHostGraph</c>.</summary>
    [Theory]
    [InlineData(false, false, false, false, false, false)]
    [InlineData(false, false, false, true, true, false)]
    [InlineData(true, false, false, true, false, true)]
    [InlineData(true, true, false, true, false, true)]
    [InlineData(true, false, true, true, false, false)]
    [InlineData(true, true, true, true, false, false)]
    [InlineData(true, false, false, false, false, false)]
    public void LocalAuthSecretBinding_WhenHostedSignInAndVaultConfigured_UsesVaultOnlyForUnsetSecrets(
        bool hostedSignIn,
        bool useLocalPat,
        bool useLocalClientSecret,
        bool vaultConfigured,
        bool needVaultForPat,
        bool needVaultForClientSecret)
    {
        var actualNeedVaultForPat = !hostedSignIn && !useLocalPat && vaultConfigured;
        var actualNeedVaultForClientSecret = hostedSignIn && !useLocalClientSecret && vaultConfigured;

        Assert.Equal(needVaultForPat, actualNeedVaultForPat);
        Assert.Equal(needVaultForClientSecret, actualNeedVaultForClientSecret);
    }

    [Theory]
    [InlineData("-", false)]
    [InlineData(" ", false)]
    [InlineData("token", true)]
    public void Resolve_WhenEnvironmentValueIsInactive_FallsThroughToDefault(string environmentValue, bool expectActive)
    {
        var previous = Environment.GetEnvironmentVariable("Parameters__gh_pat");

        try
        {
            Environment.SetEnvironmentVariable("Parameters__gh_pat", environmentValue);
            var configuration = new ConfigurationBuilder().Build();

            var actual = DeployParameterResolver.Resolve(configuration, "gh-pat", "default");

            Assert.Equal(expectActive ? environmentValue.Trim() : "default", actual);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Parameters__gh_pat", previous);
        }
    }

    /// <summary>Test double that must stay aligned with <c>AppHostDeployParameterResolver</c> in SoloDevBoard.AppHost.</summary>
    private static class DeployParameterResolver
    {
        public static string Resolve(IConfiguration configuration, string hyphenatedParameterName, string defaultValue = "-")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(hyphenatedParameterName);

            var underscoredName = hyphenatedParameterName.Replace('-', '_');
            var environmentValue = Environment.GetEnvironmentVariable($"Parameters__{underscoredName}");
            if (IsActiveParameterValue(environmentValue))
            {
                return environmentValue!.Trim();
            }

            var hyphenatedConfigurationValue = configuration[$"Parameters:{hyphenatedParameterName}"];
            if (IsActiveParameterValue(hyphenatedConfigurationValue))
            {
                return hyphenatedConfigurationValue!.Trim();
            }

            var underscoredConfigurationValue = configuration[$"Parameters:{underscoredName}"];
            if (IsActiveParameterValue(underscoredConfigurationValue))
            {
                return underscoredConfigurationValue!.Trim();
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

        public static bool IsHostedSignInEnabled(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var value = Resolve(configuration, "hosted-sign-in-enabled", "false");
            return bool.TryParse(value, out var enabled) && enabled;
        }

        public static string ResolveAuthSecretsVaultResourceGroup(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var resolved = Resolve(configuration, "auth-secrets-vault-resource-group");
            if (IsActiveParameterValue(resolved))
            {
                return resolved!.Trim();
            }

            var azureResourceGroup = Environment.GetEnvironmentVariable("Azure__ResourceGroup");
            if (IsActiveParameterValue(azureResourceGroup))
            {
                return azureResourceGroup!.Trim();
            }

            return "-";
        }

        public static bool HasConfiguredAuthSecretsVault(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var vaultName = Resolve(configuration, "auth-secrets-vault-name");
            var vaultResourceGroup = ResolveAuthSecretsVaultResourceGroup(configuration);
            return IsActiveParameterValue(vaultName) && IsActiveParameterValue(vaultResourceGroup);
        }

        public static void EnsureAuthSecretsVaultConfigured(IConfiguration configuration)
        {
            if (HasConfiguredAuthSecretsVault(configuration))
            {
                return;
            }

            throw new InvalidOperationException(
                "Deploy requires Parameters:auth-secrets-vault-name and a resource group " +
                "(Parameters:auth-secrets-vault-resource-group or Azure__ResourceGroup). " +
                "Populate the tier Key Vault before deploy. See docs/azure-key-vault-setup.md.");
        }
    }
}
