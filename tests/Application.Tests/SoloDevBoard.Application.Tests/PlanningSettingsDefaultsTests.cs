using SoloDevBoard.Application.Services.Planning;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PlanningSettingsDefaults"/>.</summary>
public sealed class PlanningSettingsDefaultsTests
{
    [Fact]
    public void Create_ReturnsRepositoryDefaultThresholds()
    {
        var settings = PlanningSettingsDefaults.Create();

        Assert.Null(settings.PlanningBoardNodeId);
        Assert.Empty(settings.ExcludedRepositories);
        Assert.Equal(8, settings.Capacity);
        Assert.Equal(3, settings.StallDays);
        Assert.Equal(14, settings.NeglectDays);
    }
}
