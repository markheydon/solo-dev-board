using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Domain.Tests;

public sealed class RepositoryTests
{
    [Fact]
    public void Repository_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange & Act
        var repository = new Repository
        {
            Id = 1,
            Name = "my-repo",
            FullName = "owner/my-repo",
            Description = "A test repository",
            Url = "https://github.com/owner/my-repo",
            IsPrivate = false,
            CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero),
        };

        // Assert
        Assert.Equal(1, repository.Id);
        Assert.Equal("my-repo", repository.Name);
        Assert.Equal("owner/my-repo", repository.FullName);
        Assert.False(repository.IsPrivate);
    }

    [Fact]
    public void Repository_Records_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var repo1 = new Repository { Id = 42, Name = "test", FullName = "owner/test" };
        var repo2 = new Repository { Id = 42, Name = "test", FullName = "owner/test" };

        // Assert
        Assert.Equal(repo1, repo2);
    }
}
