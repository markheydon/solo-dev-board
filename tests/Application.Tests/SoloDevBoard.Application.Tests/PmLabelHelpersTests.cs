using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PmLabelHelpers"/>.</summary>
public sealed class PmLabelHelpersTests
{
    [Theory]
    [InlineData(new[] { "type/story", "priority/high" }, "type/story")]
    [InlineData(new[] { "TYPE/EPIC" }, "TYPE/EPIC")]
    [InlineData(new[] { "bug" }, null)]
    public void ParseTypeLabel_VariousLabels_ReturnsExpected(string[] labels, string? expected)
    {
        var result = PmLabelHelpers.ParseTypeLabel(labels);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new[] { "priority/critical", "type/story" }, "priority/critical")]
    [InlineData(new[] { "priority/low" }, "priority/low")]
    [InlineData(new[] { "type/story" }, null)]
    public void ParsePriorityLabel_VariousLabels_ReturnsExpected(string[] labels, string? expected)
    {
        var result = PmLabelHelpers.ParsePriorityLabel(labels);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new[] { "status/blocked", "type/story" }, "status/blocked")]
    [InlineData(new[] { "status/in-progress" }, "status/in-progress")]
    [InlineData(new[] { "type/story" }, null)]
    public void ParseStatusLabel_VariousLabels_ReturnsExpected(string[] labels, string? expected)
    {
        var result = PmLabelHelpers.ParseStatusLabel(labels);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new[] { "status/blocked" }, true)]
    [InlineData(new[] { "STATUS/BLOCKED" }, true)]
    [InlineData(new[] { "status/ice-box" }, false)]
    [InlineData(new[] { "type/story" }, false)]
    public void IsBlocked_VariousLabels_ReturnsExpected(string[] labels, bool expected)
    {
        Assert.Equal(expected, PmLabelHelpers.IsBlocked(labels));
    }

    [Theory]
    [InlineData(new[] { "status/ice-box" }, true)]
    [InlineData(new[] { "status/blocked" }, false)]
    public void IsIceBoxed_VariousLabels_ReturnsExpected(string[] labels, bool expected)
    {
        Assert.Equal(expected, PmLabelHelpers.IsIceBoxed(labels));
    }

    [Theory]
    [InlineData(new[] { "status/blocked" }, true)]
    [InlineData(new[] { "status/ice-box" }, true)]
    [InlineData(new[] { "type/story", "priority/high" }, false)]
    public void IsBlockedOrDeferred_VariousLabels_ReturnsExpected(string[] labels, bool expected)
    {
        Assert.Equal(expected, PmLabelHelpers.IsBlockedOrDeferred(labels));
    }

    [Theory]
    [InlineData(new[] { "type/story", "priority/high" }, true)]
    [InlineData(new[] { "status/blocked" }, false)]
    [InlineData(new[] { "status/ice-box" }, false)]
    public void IsUnblocked_VariousLabels_ReturnsExpected(string[] labels, bool expected)
    {
        Assert.Equal(expected, PmLabelHelpers.IsUnblocked(labels));
    }

    [Theory]
    [InlineData(new[] { "type/story" }, true)]
    [InlineData(new[] { "priority/high" }, true)]
    [InlineData(new[] { "type/story", "priority/high" }, false)]
    public void IsAwaitingTriage_VariousLabels_ReturnsExpected(string[] labels, bool expected)
    {
        Assert.Equal(expected, PmLabelHelpers.IsAwaitingTriage(labels));
    }

    [Theory]
    [InlineData(new[] { "priority/critical" }, true)]
    [InlineData(new[] { "priority/high" }, true)]
    [InlineData(new[] { "priority/medium" }, false)]
    public void IsUrgent_VariousLabels_ReturnsExpected(string[] labels, bool expected)
    {
        Assert.Equal(expected, PmLabelHelpers.IsUrgent(labels));
    }

    [Theory]
    [InlineData("type/epic", 3, 3, true)]
    [InlineData("type/feature", 2, 1, false)]
    [InlineData("type/story", 3, 3, false)]
    [InlineData("type/epic", 0, 0, false)]
    public void IsEpicNearComplete_VariousCounts_ReturnsExpected(string typeLabel, int total, int completed, bool expected)
    {
        var result = PmLabelHelpers.IsEpicNearComplete([typeLabel, "priority/high"], total, completed);

        Assert.Equal(expected, result);
    }
}
