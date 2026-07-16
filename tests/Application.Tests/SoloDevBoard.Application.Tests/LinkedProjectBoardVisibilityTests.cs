using SoloDevBoard.Application.Services.GitHub;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="LinkedProjectBoardVisibility"/>.</summary>
public sealed class LinkedProjectBoardVisibilityTests
{
    [Fact]
    public void BuildInaccessibleProjectsWarning_NoInaccessibleProjects_ReturnsNull()
    {
        // Act
        var result = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(2, 0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void BuildInaccessibleProjectsWarning_PartialInaccessibleProjects_ReturnsWarning()
    {
        // Act
        var result = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(2, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("2 linked project boards", result, StringComparison.Ordinal);
        Assert.Contains("1 board could not be loaded", result, StringComparison.Ordinal);
        Assert.Contains("GitHub App sign-in", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildInaccessibleProjectsWarning_AllInaccessibleProjects_ReturnsWarning()
    {
        // Act
        var result = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(2, 2);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("none could be loaded", result, StringComparison.Ordinal);
    }
}
