using SoloDevBoard.Application.Services.Planning;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PlanningPriorityRanker"/>.</summary>
public sealed class PlanningPriorityRankerTests
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
        var comparison = PlanningPriorityRanker.ComparePriority(left, right);

        Assert.True(Math.Sign(comparison) == expectedSign);
    }

    [Fact]
    public void GetRank_AllKnownPriorities_ReturnsDescendingUrgencyOrder()
    {
        var ranks = new[]
        {
            PlanningPriorityRanker.GetRank("priority/critical"),
            PlanningPriorityRanker.GetRank("priority/high"),
            PlanningPriorityRanker.GetRank("priority/medium"),
            PlanningPriorityRanker.GetRank("priority/low"),
            PlanningPriorityRanker.GetRank(null),
        };

        Assert.True(ranks[0] < ranks[1]);
        Assert.True(ranks[1] < ranks[2]);
        Assert.True(ranks[2] < ranks[3]);
        Assert.True(ranks[3] < ranks[4]);
    }
}
