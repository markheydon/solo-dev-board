using SoloDevBoard.Application.GitHub;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="RepositoryFullName"/>.</summary>
public sealed class RepositoryFullNameTests
{
    [Theory]
    [InlineData("owner/repo", "owner")]
    [InlineData(" owner / repo ", "owner")]
    [InlineData("owner", "owner")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void ResolveOwner_VariousValues_ReturnsExpectedOwner(string fullName, string expected)
    {
        var result = RepositoryFullName.ResolveOwner(fullName);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveOwner_NullValue_ReturnsEmptyString()
    {
        var result = RepositoryFullName.ResolveOwner(null);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("owner/repo", "repo")]
    [InlineData(" owner / repo ", "repo")]
    [InlineData("owner", "")]
    [InlineData("", "")]
    public void ResolveRepositoryName_VariousValues_ReturnsExpectedName(string fullName, string expected)
    {
        var result = RepositoryFullName.ResolveRepositoryName(fullName);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryParse_ValidFullName_ReturnsOwnerAndRepository()
    {
        var parsed = RepositoryFullName.TryParse("markheydon/solo-dev-board", out var owner, out var repositoryName);

        Assert.True(parsed);
        Assert.Equal("markheydon", owner);
        Assert.Equal("solo-dev-board", repositoryName);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("")]
    [InlineData("owner/repo/extra")]
    public void TryParse_InvalidFullName_ReturnsFalse(string fullName)
    {
        var parsed = RepositoryFullName.TryParse(fullName, out var owner, out var repositoryName);

        Assert.False(parsed);
        Assert.Equal(string.Empty, owner);
        Assert.Equal(string.Empty, repositoryName);
    }

    [Fact]
    public void GroupByOwner_MixedOwners_GroupsDistinctRepositoryNames()
    {
        IReadOnlyList<string> fullNames = ["owner-b/repo-z", "owner-a/repo-a", "owner-a/repo-a", "owner-a/repo-b", "invalid"];

        var grouped = RepositoryFullName.GroupByOwner(fullNames);

        Assert.Equal(2, grouped.Count);
        Assert.Equal(["repo-a", "repo-b"], grouped["owner-a"]);
        Assert.Equal(["repo-z"], grouped["owner-b"]);
    }

    [Fact]
    public void GroupByOwner_NullList_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => RepositoryFullName.GroupByOwner(null!));
}
