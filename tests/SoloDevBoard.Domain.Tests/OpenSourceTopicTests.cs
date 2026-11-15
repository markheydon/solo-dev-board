using SoloDevBoard.Domain.Entities.Repositories;

namespace SoloDevBoard.Domain.Tests;

/// <summary>Tests for <see cref="OpenSourceTopic"/>.</summary>
public sealed class OpenSourceTopicTests
{
    [Fact]
    public void IsOpenSource_TopicsContainCanonicalSlug_ReturnsTrue()
    {
        var result = OpenSourceTopic.IsOpenSource(["open-source", "dotnet"]);

        Assert.True(result);
    }

    [Fact]
    public void IsOpenSource_TopicsContainMixedCaseSlug_ReturnsTrue()
    {
        var result = OpenSourceTopic.IsOpenSource(["Open-Source"]);

        Assert.True(result);
    }

    [Fact]
    public void IsOpenSource_TopicsContainOssOnly_ReturnsFalse()
    {
        var result = OpenSourceTopic.IsOpenSource(["oss", "dotnet"]);

        Assert.False(result);
    }

    [Fact]
    public void IsOpenSource_TopicsAreEmpty_ReturnsFalse()
    {
        var result = OpenSourceTopic.IsOpenSource([]);

        Assert.False(result);
    }

    [Fact]
    public void IsOpenSource_TopicsAreNull_ReturnsFalse()
    {
        var result = OpenSourceTopic.IsOpenSource(null);

        Assert.False(result);
    }

    [Fact]
    public void IsOpenSource_PublicRepositoryWithoutTopic_ReturnsFalse()
    {
        var result = OpenSourceTopic.IsOpenSource(["documentation"]);

        Assert.False(result);
    }

    [Fact]
    public void IsOpenSource_PrivateRepositoryWithTopic_ReturnsTrue()
    {
        var result = OpenSourceTopic.IsOpenSource(["open-source"]);

        Assert.True(result);
    }
}
