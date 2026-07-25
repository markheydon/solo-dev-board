using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Domain.Tests;

public sealed class MilestoneTests
{
    [Fact]
    public void Milestone_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange
        var dueOn = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);

        // Act
        var milestone = new Milestone
        {
            Id = 10,
            Number = 3,
            Title = "v1.0.0",
            Description = "First production release.",
            State = "open",
            DueOn = dueOn,
            OpenIssues = 5,
            ClosedIssues = 12,
        };

        // Assert
        Assert.Equal(10, milestone.Id);
        Assert.Equal(3, milestone.Number);
        Assert.Equal("v1.0.0", milestone.Title);
        Assert.Equal("First production release.", milestone.Description);
        Assert.Equal("open", milestone.State);
        Assert.Equal(dueOn, milestone.DueOn);
        Assert.Equal(5, milestone.OpenIssues);
        Assert.Equal(12, milestone.ClosedIssues);
    }

    [Fact]
    public void Milestone_Records_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var milestone1 = new Milestone { Id = 1, Number = 1, Title = "Sprint 1" };
        var milestone2 = new Milestone { Id = 1, Number = 1, Title = "Sprint 1" };

        // Assert
        Assert.Equal(milestone1, milestone2);
    }
}
