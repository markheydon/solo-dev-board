using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PlanningFocusOrderSequencer"/>.</summary>
public sealed class PlanningFocusOrderSequencerTests
{
    [Theory]
    [InlineData("type/story", true)]
    [InlineData("type/enabler", true)]
    [InlineData("type/test", true)]
    [InlineData("type/feature", false)]
    [InlineData("type/epic", false)]
    [InlineData("type/bug", false)]
    [InlineData(null, false)]
    public void ShouldAssignFocusOrder_TypeLabel_ReturnsExpectedOutcome(string? typeLabel, bool expected)
    {
        IReadOnlyList<string> labels = typeLabel is null ? [] : [typeLabel];

        var result = PlanningFocusOrderSequencer.ShouldAssignFocusOrder(labels);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetNextFocusOrder_NoExistingValues_ReturnsOne()
    {
        var result = PlanningFocusOrderSequencer.GetNextFocusOrder([]);

        Assert.Equal(1, result);
    }

    [Fact]
    public void GetNextFocusOrder_ExistingUpNextValues_ReturnsMaxPlusOne()
    {
        var items = new[]
        {
            CreateUpNextItem(focusOrder: 1),
            CreateUpNextItem(focusOrder: 3),
            CreateUpNextItem(focusOrder: 2),
            CreateUpNextItem(focusOrder: null),
        };

        var result = PlanningFocusOrderSequencer.GetNextFocusOrder(items);

        Assert.Equal(4, result);
    }

    [Fact]
    public void GetNextFocusOrder_UpNextItemsIsNull_ThrowsArgumentNullException()
    {
        IReadOnlyList<ProjectBoardItemDto>? items = null;

        Assert.Throws<ArgumentNullException>(() => PlanningFocusOrderSequencer.GetNextFocusOrder(items!));
    }

    private static ProjectBoardItemDto CreateUpNextItem(double? focusOrder) =>
        new(
            "PVTI_item",
            new ProjectBoardItemStatusDto("opt-up-next", "Up Next"),
            focusOrder,
            new ProjectBoardItemContentDto(
                ProjectBoardItemContentTypeDto.Issue,
                40,
                "owner",
                "repo",
                "Title",
                "https://github.com/owner/repo/issues/40"),
            DateTimeOffset.UtcNow,
            UsedItemUpdatedAtFallback: false);
}
