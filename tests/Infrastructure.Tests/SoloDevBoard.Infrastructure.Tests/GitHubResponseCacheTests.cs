using Microsoft.Extensions.Caching.Memory;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Unit tests for <see cref="GitHubResponseCache"/> cache key scoping, invalidation, and TTL behaviour.</summary>
public sealed class GitHubResponseCacheTests
{
    [Fact]
    public async Task GetOrCreateLabelsAsync_CalledTwice_ReturnsDefensiveCopies()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var sut = GitHubCachingTestSupport.CreateResponseCache();
        var factoryCalls = 0;

        var first = await sut.GetOrCreateLabelsAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["alpha"]);
            },
            cancellationToken);

        var second = await sut.GetOrCreateLabelsAsync<string>(
            "owner",
            "repo",
            static _ => throw new InvalidOperationException("Factory should not run on cache hit."),
            cancellationToken);

        Assert.NotSame(first, second);
        Assert.Equal(["alpha"], first);
        Assert.Equal(["alpha"], second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateLabelsAsync_DifferentUsers_DoNotShareCache()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var userACache = GitHubCachingTestSupport.CreateResponseCache(
            memoryCache,
            GitHubCachingTestSupport.CreateCurrentUserContext("user-a"));
        var userBCache = GitHubCachingTestSupport.CreateResponseCache(
            memoryCache,
            GitHubCachingTestSupport.CreateCurrentUserContext("user-b"));

        var userAValues = await userACache.GetOrCreateLabelsAsync(
            "owner",
            "repo",
            _ => Task.FromResult<IReadOnlyList<string>>(["user-a"]),
            cancellationToken);

        var userBValues = await userBCache.GetOrCreateLabelsAsync(
            "owner",
            "repo",
            _ => Task.FromResult<IReadOnlyList<string>>(["user-b"]),
            cancellationToken);

        Assert.Equal(["user-a"], userAValues);
        Assert.Equal(["user-b"], userBValues);
    }

    [Fact]
    public async Task InvalidateLabels_RemovesCachedEntry_NextCallRefetches()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var sut = GitHubCachingTestSupport.CreateResponseCache();
        var factoryCalls = 0;

        await sut.GetOrCreateLabelsAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["before"]);
            },
            cancellationToken);

        sut.InvalidateLabels("owner", "repo");

        var refreshed = await sut.GetOrCreateLabelsAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["after"]);
            },
            cancellationToken);

        Assert.Equal(["after"], refreshed);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task InvalidateMilestones_RemovesCachedEntry_NextCallRefetches()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var sut = GitHubCachingTestSupport.CreateResponseCache();
        var factoryCalls = 0;

        await sut.GetOrCreateMilestonesAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["before"]);
            },
            cancellationToken);

        sut.InvalidateMilestones("owner", "repo");

        var refreshed = await sut.GetOrCreateMilestonesAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["after"]);
            },
            cancellationToken);

        Assert.Equal(["after"], refreshed);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateLabelsAsync_AfterTtlExpires_RefetchesFromFactory()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var sut = GitHubCachingTestSupport.CreateResponseCache(
            options: new GitHubCacheOptions { LabelsTtlSeconds = 1 });
        var factoryCalls = 0;

        await sut.GetOrCreateLabelsAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["first"]);
            },
            cancellationToken);

        await Task.Delay(TimeSpan.FromMilliseconds(1_100), cancellationToken);

        var refreshed = await sut.GetOrCreateLabelsAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["second"]);
            },
            cancellationToken);

        Assert.Equal(["second"], refreshed);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateMilestonesAsync_AfterTtlExpires_RefetchesFromFactory()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var sut = GitHubCachingTestSupport.CreateResponseCache(
            options: new GitHubCacheOptions { MilestonesTtlSeconds = 1 });
        var factoryCalls = 0;

        await sut.GetOrCreateMilestonesAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["first"]);
            },
            cancellationToken);

        await Task.Delay(TimeSpan.FromMilliseconds(1_100), cancellationToken);

        var refreshed = await sut.GetOrCreateMilestonesAsync(
            "owner",
            "repo",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<IReadOnlyList<string>>(["second"]);
            },
            cancellationToken);

        Assert.Equal(["second"], refreshed);
        Assert.Equal(2, factoryCalls);
    }
}
