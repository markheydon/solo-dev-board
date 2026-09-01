using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>
/// Provides scoped in-memory caching for read-heavy GitHub API catalogue responses.
/// Cache keys are scoped by the current user's owner login so hosted sessions do not share data.
/// </summary>
public sealed class GitHubResponseCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly GitHubCacheOptions _options;

    /// <summary>Initialises a new instance of the <see cref="GitHubResponseCache"/> class.</summary>
    /// <param name="memoryCache">The memory cache used to store GitHub API responses.</param>
    /// <param name="currentUserContext">The current user context used to scope cache keys.</param>
    /// <param name="options">Cache lifetime configuration.</param>
    public GitHubResponseCache(
        IMemoryCache memoryCache,
        ICurrentUserContext currentUserContext,
        IOptions<GitHubCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(currentUserContext);
        ArgumentNullException.ThrowIfNull(options);

        _memoryCache = memoryCache;
        _currentUserContext = currentUserContext;
        _options = options.Value;
    }

    /// <summary>Gets or creates a cached repository catalogue for the authenticated user.</summary>
    /// <typeparam name="T">The cached item type.</typeparam>
    /// <param name="factory">The factory used to load the catalogue when the cache misses.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The cached or freshly loaded catalogue.</returns>
    public Task<IReadOnlyList<T>> GetOrCreateUserRepositoriesAsync<T>(
        Func<CancellationToken, Task<IReadOnlyList<T>>> factory,
        CancellationToken cancellationToken = default)
        => GetOrCreateAsync(BuildUserRepositoriesKey(), _options.RepositoriesTtlSeconds, factory, cancellationToken);

    /// <summary>Gets or creates a cached repository catalogue for the specified owner.</summary>
    /// <typeparam name="T">The cached item type.</typeparam>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="factory">The factory used to load the catalogue when the cache misses.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The cached or freshly loaded catalogue.</returns>
    public Task<IReadOnlyList<T>> GetOrCreateOwnerRepositoriesAsync<T>(
        string owner,
        Func<CancellationToken, Task<IReadOnlyList<T>>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        return GetOrCreateAsync(BuildOwnerRepositoriesKey(owner), _options.RepositoriesTtlSeconds, factory, cancellationToken);
    }

    /// <summary>Gets or creates a cached label catalogue for the specified repository.</summary>
    /// <typeparam name="T">The cached item type.</typeparam>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="factory">The factory used to load the catalogue when the cache misses.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The cached or freshly loaded catalogue.</returns>
    public Task<IReadOnlyList<T>> GetOrCreateLabelsAsync<T>(
        string owner,
        string repo,
        Func<CancellationToken, Task<IReadOnlyList<T>>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        return GetOrCreateAsync(BuildLabelsKey(owner, repo), _options.LabelsTtlSeconds, factory, cancellationToken);
    }

    /// <summary>Gets or creates a cached milestone catalogue for the specified repository.</summary>
    /// <typeparam name="T">The cached item type.</typeparam>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="factory">The factory used to load the catalogue when the cache misses.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The cached or freshly loaded catalogue.</returns>
    public Task<IReadOnlyList<T>> GetOrCreateMilestonesAsync<T>(
        string owner,
        string repo,
        Func<CancellationToken, Task<IReadOnlyList<T>>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        return GetOrCreateAsync(BuildMilestonesKey(owner, repo), _options.MilestonesTtlSeconds, factory, cancellationToken);
    }

    /// <summary>Removes the cached label catalogue for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    public void InvalidateLabels(string owner, string repo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        _memoryCache.Remove(BuildLabelsKey(owner, repo));
    }

    /// <summary>Removes the cached milestone catalogue for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    public void InvalidateMilestones(string owner, string repo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        _memoryCache.Remove(BuildMilestonesKey(owner, repo));
    }

    /// <summary>Removes the cached repository catalogue for the authenticated user.</summary>
    public void InvalidateUserRepositories()
        => _memoryCache.Remove(BuildUserRepositoriesKey());

    /// <summary>Removes the cached repository catalogue for the specified owner.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    public void InvalidateOwnerRepositories(string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        _memoryCache.Remove(BuildOwnerRepositoriesKey(owner));
    }

    private string BuildUserRepositoriesKey()
        => $"gh:{NormalizeKeySegment(_currentUserContext.OwnerLogin)}:repos:user";

    private string BuildOwnerRepositoriesKey(string owner)
        => $"gh:{NormalizeKeySegment(_currentUserContext.OwnerLogin)}:repos:owner:{NormalizeKeySegment(owner)}";

    private string BuildLabelsKey(string owner, string repo)
        => $"gh:{NormalizeKeySegment(_currentUserContext.OwnerLogin)}:labels:{NormalizeKeySegment(owner)}:{NormalizeKeySegment(repo)}";

    private string BuildMilestonesKey(string owner, string repo)
        => $"gh:{NormalizeKeySegment(_currentUserContext.OwnerLogin)}:milestones:{NormalizeKeySegment(owner)}:{NormalizeKeySegment(repo)}";

    private static string NormalizeKeySegment(string value) => value.ToLowerInvariant();

    private async Task<IReadOnlyList<T>> GetOrCreateAsync<T>(
        string key,
        int ttlSeconds,
        Func<CancellationToken, Task<IReadOnlyList<T>>> factory,
        CancellationToken cancellationToken)
    {
        var cached = await _memoryCache.GetOrCreateAsync(
            key,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds);
                var result = await factory(cancellationToken).ConfigureAwait(false);
                return CopyCatalogue(result);
            }).ConfigureAwait(false);

        return CopyCatalogue(cached);
    }

    private static T[] CopyCatalogue<T>(IReadOnlyList<T>? catalogue)
        => catalogue is null || catalogue.Count == 0
            ? Array.Empty<T>()
            : catalogue.ToArray();
}
