using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Tests;

/// <summary>Shared helpers for GitHub API caching tests.</summary>
internal static class GitHubCachingTestSupport
{
    /// <summary>Creates a <see cref="GitHubResponseCache"/> for unit tests.</summary>
    /// <param name="memoryCache">Optional memory cache instance to share between collaborators.</param>
    /// <param name="currentUserContext">Optional current user context for cache key scoping.</param>
    /// <param name="options">Optional cache lifetime configuration.</param>
    /// <returns>A configured response cache instance.</returns>
    internal static GitHubResponseCache CreateResponseCache(
        IMemoryCache? memoryCache = null,
        ICurrentUserContext? currentUserContext = null,
        GitHubCacheOptions? options = null)
    {
        var context = currentUserContext ?? CreateCurrentUserContext();

        return new GitHubResponseCache(
            memoryCache ?? new MemoryCache(new MemoryCacheOptions()),
            context,
            Options.Create(options ?? new GitHubCacheOptions()));
    }

    /// <summary>Creates a substitute current user context for Infrastructure tests.</summary>
    /// <param name="ownerLogin">The owner login used to scope cache keys.</param>
    /// <param name="token">The access token returned by <see cref="ICurrentUserContext.GetAccessToken"/>.</param>
    /// <returns>A configured substitute current user context.</returns>
    internal static ICurrentUserContext CreateCurrentUserContext(string ownerLogin = "test-user", string token = "test-token")
    {
        var context = Substitute.For<ICurrentUserContext>();
        context.OwnerLogin.Returns(ownerLogin);
        context.GetAccessToken().Returns(token);
        return context;
    }
}
