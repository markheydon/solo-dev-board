using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for <see cref="GitHubPaginationOptionsValidator"/>.</summary>
public sealed class GitHubPaginationOptionsValidatorTests
{
    private readonly GitHubPaginationOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new GitHubPaginationOptions();

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidWorkflowRunsMaxPages_ReturnsFailure(int invalidValue)
    {
        // Arrange
        var options = new GitHubPaginationOptions { WorkflowRunsMaxPages = invalidValue };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(nameof(GitHubPaginationOptions.WorkflowRunsMaxPages), result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_InvalidWorkflowRunsPerPage_ReturnsFailure(int invalidValue)
    {
        // Arrange
        var options = new GitHubPaginationOptions { WorkflowRunsPerPage = invalidValue };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(nameof(GitHubPaginationOptions.WorkflowRunsPerPage), result.FailureMessage, StringComparison.Ordinal);
    }
}
