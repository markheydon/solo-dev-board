using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="IterationPlanningViewMapper"/>.</summary>
public sealed class IterationPlanningViewMapperTests
{
    [Fact]
    public void Map_UpNextItems_SortedByFocusOrderThenTitle()
    {
        var boardItems = new[]
        {
            CreateBoardItem("PVTI_b", "Up Next", 2, "Beta"),
            CreateBoardItem("PVTI_a", "Up Next", 1, "Alpha"),
            CreateBoardItem("PVTI_todo", "Todo", null, "Later"),
        };

        var result = IterationPlanningViewMapper.Map([], boardItems, [], hasFocusOrderField: true);

        Assert.Equal(2, result.UpNextItems.Count);
        Assert.Equal("Alpha", result.UpNextItems[0].Title);
        Assert.Equal(1, result.UpNextItems[0].FocusOrder);
        Assert.Equal("Beta", result.UpNextItems[1].Title);
    }

    [Fact]
    public void Map_Candidates_ExcludeUpNextAndInProgressItems()
    {
        var workItems = new[]
        {
            CreateWorkItem(10, "Todo item"),
            CreateWorkItem(11, "Up Next item"),
            CreateWorkItem(12, "In Progress item"),
        };

        var boardItems = new[]
        {
            CreateBoardItem("PVTI_todo", "Todo", null, "Todo item", 10),
            CreateBoardItem("PVTI_up-next", "Up Next", 1, "Up Next item", 11),
            CreateBoardItem("PVTI_in-progress", "In Progress", null, "In Progress item", 12),
        };

        var result = IterationPlanningViewMapper.Map(workItems, boardItems, [], hasFocusOrderField: true);

        Assert.Single(result.Candidates);
        Assert.Equal(10, result.Candidates[0].Number);
    }

    [Fact]
    public void Map_WhenFocusOrderFieldExists_ComputesNextStoryFocusOrder()
    {
        var boardItems = new[]
        {
            CreateBoardItem("PVTI_a", "Up Next", 1, "Alpha"),
            CreateBoardItem("PVTI_b", "Up Next", 3, "Beta"),
        };

        var result = IterationPlanningViewMapper.Map([], boardItems, [], hasFocusOrderField: true);

        Assert.True(result.HasFocusOrderField);
        Assert.Equal(4, result.NextStoryFocusOrder);
    }

    [Fact]
    public void Map_WhenFocusOrderFieldMissing_DoesNotComputeNextStoryFocusOrder()
    {
        var boardItems = new[]
        {
            CreateBoardItem("PVTI_a", "Up Next", 1, "Alpha"),
        };

        var result = IterationPlanningViewMapper.Map([], boardItems, [], hasFocusOrderField: false);

        Assert.False(result.HasFocusOrderField);
        Assert.Equal(0, result.NextStoryFocusOrder);
    }

    [Theory]
    [InlineData("Up Next", false)]
    [InlineData("In Progress", false)]
    [InlineData("Todo", true)]
    [InlineData(null, true)]
    public void IsCandidate_BoardStatusName_ReturnsExpectedOutcome(string? boardStatusName, bool expected)
    {
        var workItem = CreateWorkItem(20, "Candidate");

        var result = IterationPlanningViewMapper.IsCandidate(workItem, boardStatusName);

        Assert.Equal(expected, result);
    }

    private static PmWorkItemDto CreateWorkItem(int number, string title) =>
        new(
            PmWorkItemTypeDto.Issue,
            number,
            title,
            $"https://github.com/owner/repo/issues/{number}",
            "owner/repo",
            ["type/story", "priority/medium"],
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null);

    private static ProjectBoardItemDto CreateBoardItem(
        string projectItemId,
        string statusName,
        double? focusOrder,
        string title,
        int number = 1) =>
        new(
            projectItemId,
            new ProjectBoardItemStatusDto($"opt-{statusName.Replace(' ', '-').ToLowerInvariant()}", statusName),
            focusOrder,
            new ProjectBoardItemContentDto(
                ProjectBoardItemContentTypeDto.Issue,
                number,
                "owner",
                "repo",
                title,
                $"https://github.com/owner/repo/issues/{number}"),
            DateTimeOffset.UtcNow,
            UsedItemUpdatedAtFallback: false);
}
