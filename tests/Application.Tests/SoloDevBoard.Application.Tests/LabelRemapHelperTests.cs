using SoloDevBoard.Application.Services.Labels;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="LabelRemapHelper"/>.</summary>
public sealed class LabelRemapHelperTests
{
    [Fact]
    public void BuildRetaggedLabelNames_WhenSourcePresent_ReplacesSourceWithDestination()
    {
        var result = LabelRemapHelper.BuildRetaggedLabelNames(["story", "priority/high"], "story", "type/story");

        Assert.Equal(["priority/high", "type/story"], result);
    }

    [Fact]
    public void BuildRetaggedLabelNames_WhenDestinationAlreadyPresent_RemovesSourceOnly()
    {
        var result = LabelRemapHelper.BuildRetaggedLabelNames(["type/story", "priority/high"], "story", "type/story");

        Assert.Equal(["type/story", "priority/high"], result);
    }

    [Fact]
    public void BuildRetaggedLabelNames_WhenSourceMatchesCaseInsensitively_RemovesSource()
    {
        var result = LabelRemapHelper.BuildRetaggedLabelNames(["Story"], "story", "type/story");

        Assert.Equal(["type/story"], result);
    }
}
