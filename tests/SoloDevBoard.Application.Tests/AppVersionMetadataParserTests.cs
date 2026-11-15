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
        var assembly = CreateAssembly(informationalVersion);

        var metadata = AppVersionMetadataParser.Parse(assembly);

        Assert.Equal(expectedVersion, metadata.Version);
        Assert.Equal(expectedBuildMetadata, metadata.BuildMetadata);
        Assert.Null(metadata.BuildTimestampUtc);
    }

    [Fact]
    public void Parse_BuildTimestampMetadataPresent_ReturnsParsedUtcTimestamp()
    {
        var assembly = CreateAssembly(
            "1.0.1-staging.0.49+abc1234",
            buildTimestampUtc: "2026-08-23T14:11:00.0000000Z");

        var metadata = AppVersionMetadataParser.Parse(assembly);

        Assert.Equal("1.0.1-staging.0.49", metadata.Version);
        Assert.Equal("abc1234", metadata.BuildMetadata);
        Assert.Equal(new DateTimeOffset(2026, 8, 23, 14, 11, 0, TimeSpan.Zero), metadata.BuildTimestampUtc);
    }

    [Fact]
    public void Parse_InvalidBuildTimestampMetadata_ReturnsNullTimestamp()
    {
        var assembly = CreateAssembly(
            "1.0.1-staging.0.49+abc1234",
            buildTimestampUtc: "not-a-timestamp");

        var metadata = AppVersionMetadataParser.Parse(assembly);

        Assert.Null(metadata.BuildTimestampUtc);
    }

    [Fact]
    public void Parse_AssemblyVersionOnly_ReturnsNonEmptyVersion()
    {
        var assembly = typeof(AppVersionMetadataParserTests).Assembly;

        var metadata = AppVersionMetadataParser.Parse(assembly);

        Assert.False(string.IsNullOrWhiteSpace(metadata.Version));
    }

    private static Assembly CreateAssembly(
        string informationalVersion,
        string? buildTimestampUtc = null)
    {
        var assemblyName = new AssemblyName(Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        assemblyBuilder.DefineDynamicModule("TestModule");

        var informationalVersionAttributeBuilder = new CustomAttributeBuilder(
            typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!,
            [informationalVersion]);
        assemblyBuilder.SetCustomAttribute(informationalVersionAttributeBuilder);

        if (!string.IsNullOrWhiteSpace(buildTimestampUtc))
        {
            var buildTimestampAttributeBuilder = new CustomAttributeBuilder(
                typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!,
                ["BuildTimestampUtc", buildTimestampUtc]);
            assemblyBuilder.SetCustomAttribute(buildTimestampAttributeBuilder);
        }

        return assemblyBuilder;
    }
}
