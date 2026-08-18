using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PmPriorityRanker"/>.</summary>
public sealed class PmPriorityRankerTests
{
    [Theory]
    [InlineData("priority/critical", "priority/high", -1)]
    [InlineData("priority/high", "priority/medium", -1)]
    [InlineData("priority/medium", "priority/low", -1)]
    [InlineData("priority/low", null, -1)]
    [InlineData(null, "priority/critical", 1)]
    [InlineData("priority/high", "priority/high", 0)]
    public void ComparePriority_VariousLabels_ReturnsExpectedOrder(string? left, string? right, int expectedSign)
    {
        var comparison = PmPriorityRanker.ComparePriority(left, right);

        Assert.True(Math.Sign(comparison) == expectedSign);
    }

    [Fact]
    public void GetRank_AllKnownPriorities_ReturnsDescendingUrgencyOrder()
    {
        var ranks = new[]
        {
            PmPriorityRanker.GetRank("priority/critical"),
            PmPriorityRanker.GetRank("priority/high"),
            PmPriorityRanker.GetRank("priority/medium"),
            PmPriorityRanker.GetRank("priority/low"),
            PmPriorityRanker.GetRank(null),
        };

        Assert.True(ranks[0] < ranks[1]);
        Assert.True(ranks[1] < ranks[2]);
        Assert.True(ranks[2] < ranks[3]);
        Assert.True(ranks[3] < ranks[4]);
    }
}
