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
}
