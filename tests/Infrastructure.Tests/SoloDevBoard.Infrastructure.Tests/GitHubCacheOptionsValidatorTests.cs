using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Tests for <see cref="GitHubCacheOptionsValidator"/>.</summary>
public sealed class GitHubCacheOptionsValidatorTests
{
    private readonly GitHubCacheOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new GitHubCacheOptions();

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(nameof(GitHubCacheOptions.RepositoriesTtlSeconds), 0)]
    [InlineData(nameof(GitHubCacheOptions.LabelsTtlSeconds), -1)]
    [InlineData(nameof(GitHubCacheOptions.MilestonesTtlSeconds), -5)]
    [InlineData(nameof(GitHubCacheOptions.WorkflowDirectoryTtlSeconds), 0)]
    public void Validate_InvalidTtl_ReturnsFailure(string propertyName, int invalidValue)
    {
        // Arrange
        var options = new GitHubCacheOptions();
        typeof(GitHubCacheOptions).GetProperty(propertyName)!.SetValue(options, invalidValue);

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(propertyName, result.FailureMessage, StringComparison.Ordinal);
    }
}
