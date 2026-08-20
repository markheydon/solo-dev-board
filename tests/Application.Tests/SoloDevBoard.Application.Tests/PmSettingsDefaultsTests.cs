using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PmSettingsDefaults"/>.</summary>
public sealed class PmSettingsDefaultsTests
{
    [Fact]
    public void Create_ReturnsRepositoryDefaultThresholds()
    {
        var settings = PmSettingsDefaults.Create();

        Assert.Null(settings.PlanningBoardNodeId);
        Assert.Empty(settings.ExcludedRepositories);
        Assert.Equal(8, settings.Capacity);
        Assert.Equal(3, settings.StallDays);
        Assert.Equal(14, settings.NeglectDays);
    }

    [Theory]
    [InlineData(nameof(PmSettingsDefaults.Capacity), 8)]
    [InlineData(nameof(PmSettingsDefaults.StallDays), 3)]
    [InlineData(nameof(PmSettingsDefaults.NeglectDays), 14)]
    public void DefaultConstants_MatchWireframeThresholds(string constantName, int expectedValue)
    {
        var actualValue = constantName switch
        {
            nameof(PmSettingsDefaults.Capacity) => PmSettingsDefaults.Capacity,
            nameof(PmSettingsDefaults.StallDays) => PmSettingsDefaults.StallDays,
            nameof(PmSettingsDefaults.NeglectDays) => PmSettingsDefaults.NeglectDays,
            _ => throw new ArgumentOutOfRangeException(nameof(constantName), constantName, "Unexpected PM settings default constant."),
        };

        Assert.Equal(expectedValue, actualValue);
    }
}
