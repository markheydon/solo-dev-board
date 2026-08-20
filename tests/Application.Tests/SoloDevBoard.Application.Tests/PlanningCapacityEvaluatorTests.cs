using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PlanningCapacityEvaluator"/>.</summary>
public sealed class PlanningCapacityEvaluatorTests
{
    [Fact]
    public void IsAtOrOverCapacity_ActiveLoadEqualsCapacity_ReturnsTrue()
    {
        var result = PlanningCapacityEvaluator.IsAtOrOverCapacity(activeLoad: 8, capacity: 8);

        Assert.True(result);
    }

    [Fact]
    public void IsAtOrOverCapacity_ActiveLoadGreaterThanCapacity_ReturnsTrue()
    {
        var result = PlanningCapacityEvaluator.IsAtOrOverCapacity(activeLoad: 9, capacity: 8);

        Assert.True(result);
    }

    [Fact]
    public void IsAtOrOverCapacity_ActiveLoadBelowCapacity_ReturnsFalse()
    {
        var result = PlanningCapacityEvaluator.IsAtOrOverCapacity(activeLoad: 7, capacity: 8);

        Assert.False(result);
    }

    [Fact]
    public void WouldExceedCapacityAfterAdd_AtLimit_ReturnsTrue()
    {
        var result = PlanningCapacityEvaluator.WouldExceedCapacityAfterAdd(activeLoad: 8, capacity: 8);

        Assert.True(result);
    }

    [Fact]
    public void WouldExceedCapacityAfterAdd_BelowLimit_ReturnsFalse()
    {
        var result = PlanningCapacityEvaluator.WouldExceedCapacityAfterAdd(activeLoad: 7, capacity: 8);

        Assert.False(result);
    }

    [Fact]
    public void CountActiveLoad_UpNextAndInProgressItems_ReturnsSum()
    {
        var boardItems = new[]
        {
            CreateBoardItem("Up Next"),
            CreateBoardItem("Up Next"),
            CreateBoardItem("In Progress"),
            CreateBoardItem("Todo"),
        };

        var result = PlanningCapacityEvaluator.CountActiveLoad(boardItems);

        Assert.Equal(3, result);
    }

    private static ProjectBoardItemDto CreateBoardItem(string statusName) =>
        new(
            "PVTI_item",
            new ProjectBoardItemStatusDto("opt", statusName),
            null,
            new ProjectBoardItemContentDto(
                ProjectBoardItemContentTypeDto.Issue,
                1,
                "owner",
                "repo",
                "Title",
                "https://github.com/owner/repo/issues/1"),
            DateTimeOffset.UtcNow,
            UsedItemUpdatedAtFallback: false);
}
