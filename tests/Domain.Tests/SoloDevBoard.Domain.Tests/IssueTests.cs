using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Domain.Tests;

public sealed class IssueTests
{
    [Fact]
    public void Issue_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange & Act
        var issue = new Issue
        {
            Id = 100,
            Number = 5,
            Title = "Fix the bug",
            Body = "There is a bug that needs fixing.",
            State = "open",
            AuthorLogin = "developer1",
            CreatedAt = new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2024, 3, 16, 12, 0, 0, TimeSpan.Zero),
        };

        // Assert
        Assert.Equal(100, issue.Id);
        Assert.Equal(5, issue.Number);
        Assert.Equal("Fix the bug", issue.Title);
        Assert.Equal("open", issue.State);
        Assert.Equal("developer1", issue.AuthorLogin);
    }

    [Fact]
    public void Issue_WithLabels_ShouldContainAllLabels()
    {
        // Arrange
        var labels = new List<Label>
        {
            new Label { Name = "bug", Colour = "d73a4a" },
            new Label { Name = "priority: high", Colour = "e4e669" },
        };

        // Act
        var issue = new Issue
        {
            Id = 200,
            Number = 10,
            Title = "Critical bug",
            Labels = labels,
        };

        // Assert
        Assert.Equal(2, issue.Labels.Count);
        Assert.Contains(issue.Labels, l => l.Name == "bug");
    }
}
