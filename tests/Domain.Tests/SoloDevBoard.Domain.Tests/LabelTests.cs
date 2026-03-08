using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Domain.Tests;

public sealed class LabelTests
{
    [Fact]
    public void Label_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange & Act
        var label = new Label
        {
            Name = "priority/high",
            Colour = "d93f0b",
            Description = "High-priority work item",
            RepositoryName = "solo-dev-board",
        };

        // Assert
        Assert.Equal("priority/high", label.Name);
        Assert.Equal("d93f0b", label.Colour);
        Assert.Equal("High-priority work item", label.Description);
        Assert.Equal("solo-dev-board", label.RepositoryName);
    }

    [Fact]
    public void Label_RepositoryNameNotProvided_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var label = new Label { Name = "bug", Colour = "d73a4a" };

        // Assert
        Assert.Equal(string.Empty, label.RepositoryName);
    }
}
