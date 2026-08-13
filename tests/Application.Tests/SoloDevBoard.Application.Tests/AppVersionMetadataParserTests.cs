using System.Reflection;
using System.Reflection.Emit;
using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="AppVersionMetadataParser"/>.</summary>
public sealed class AppVersionMetadataParserTests
{
    [Theory]
    [InlineData("1.0.0+abc1234", "1.0.0", "abc1234")]
    [InlineData("1.0.1-staging.0.5+def5678", "1.0.1-staging.0.5", "def5678")]
    [InlineData("1.0.0", "1.0.0", "")]
    public void Parse_InformationalVersionAttributePresent_ReturnsVersionAndBuildMetadata(
        string informationalVersion,
        string expectedVersion,
        string expectedBuildMetadata)
    {
        // Arrange
        var assembly = CreateAssemblyWithInformationalVersion(informationalVersion);

        // Act
        var metadata = AppVersionMetadataParser.Parse(assembly);

        // Assert
        Assert.Equal(expectedVersion, metadata.Version);
        Assert.Equal(expectedBuildMetadata, metadata.BuildMetadata);
    }

    [Fact]
    public void Parse_AssemblyVersionOnly_ReturnsNonEmptyVersion()
    {
        // Arrange
        var assembly = typeof(AppVersionMetadataParserTests).Assembly;

        // Act
        var metadata = AppVersionMetadataParser.Parse(assembly);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(metadata.Version));
    }

    private static Assembly CreateAssemblyWithInformationalVersion(string informationalVersion)
    {
        var assemblyName = new AssemblyName(Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        assemblyBuilder.DefineDynamicModule("TestModule");
        var attributeBuilder = new CustomAttributeBuilder(
            typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!,
            [informationalVersion]);
        assemblyBuilder.SetCustomAttribute(attributeBuilder);
        return assemblyBuilder;
    }
}
