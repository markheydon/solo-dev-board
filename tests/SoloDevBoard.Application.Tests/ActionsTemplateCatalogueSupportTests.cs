using SoloDevBoard.Application.Services.ActionsTemplates;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="ActionsTemplateIdFormatter"/> and <see cref="WorkflowYamlTemplateParser"/>.</summary>
public sealed class ActionsTemplateCatalogueSupportTests
{
    [Theory]
    [InlineData(1, "builtin:1")]
    [InlineData(3, "builtin:3")]
    public void FormatBuiltIn_ReturnsStableIdentifier(int builtInNumber, string expectedId)
    {
        // Act
        var result = ActionsTemplateIdFormatter.FormatBuiltIn(builtInNumber);

        // Assert
        Assert.Equal(expectedId, result);
    }

    [Fact]
    public void FormatCustom_ReturnsStableIdentifier()
    {
        // Act
        var result = ActionsTemplateIdFormatter.FormatCustom("owner/repo", ".github/workflows/ci.yml");

        // Assert
        Assert.Equal("custom:owner/repo:.github/workflows/ci.yml", result);
    }

    [Fact]
    public void ParseCustom_RoundTripsFormattedIdentifier()
    {
        // Arrange
        var templateId = ActionsTemplateIdFormatter.FormatCustom("owner/repo", ".github/workflows/deploy.yml");

        // Act
        var (repositoryFullName, workflowFilePath) = ActionsTemplateIdFormatter.ParseCustom(templateId);

        // Assert
        Assert.Equal("owner/repo", repositoryFullName);
        Assert.Equal(".github/workflows/deploy.yml", workflowFilePath);
    }

    [Fact]
    public void InferParameters_UniqueTokens_ReturnRequiredStringParameters()
    {
        // Arrange
        const string yaml = "name: CI\nbranch: {{mainBranch}}\nversion: {{dotnetVersion}}\nbranch: {{mainBranch}}";

        // Act
        var parameters = WorkflowYamlTemplateParser.InferParameters(yaml);

        // Assert
        Assert.Equal(2, parameters.Count);
        Assert.Contains(parameters, parameter => parameter.Name == "mainBranch" && parameter.Label == "mainBranch" && parameter.IsRequired);
        Assert.Contains(parameters, parameter => parameter.Name == "dotnetVersion" && parameter.Label == "dotnetVersion" && parameter.IsRequired);
        Assert.All(parameters, parameter => Assert.Equal(string.Empty, parameter.Description));
    }

    [Fact]
    public void ResolveDisplayName_WhenYamlHasNameKey_UsesYamlValue()
    {
        // Act
        var result = WorkflowYamlTemplateParser.ResolveDisplayName("name: Deploy to Azure\njobs: {}", "deploy.yml");

        // Assert
        Assert.Equal("Deploy to Azure", result);
    }

    [Fact]
    public void ResolveDisplayName_WhenYamlHasNoNameKey_UsesFileName()
    {
        // Act
        var result = WorkflowYamlTemplateParser.ResolveDisplayName("jobs:\n  build:\n    runs-on: ubuntu-latest", "deploy.yml");

        // Assert
        Assert.Equal("deploy", result);
    }
}
