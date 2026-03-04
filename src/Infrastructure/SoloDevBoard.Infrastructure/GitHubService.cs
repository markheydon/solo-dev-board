using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Infrastructure;

/// <summary>Stub implementation of <see cref="IGitHubService"/> using <see cref="IHttpClientFactory"/>.</summary>
public sealed class GitHubService : IGitHubService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Repository>> GetRepositoriesAsync(string owner, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GitHub API call to list repositories for the given owner.
        IReadOnlyList<Repository> result = [];
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Issue>> GetIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GitHub API call to list issues for the given repository.
        IReadOnlyList<Issue> result = [];
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GitHub API call to list pull requests for the given repository.
        IReadOnlyList<PullRequest> result = [];
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GitHub API call to list milestones for the given repository.
        IReadOnlyList<Milestone> result = [];
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GitHub API call to list labels for the given repository.
        IReadOnlyList<Label> result = [];
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<Label> CreateLabelAsync(string owner, string repo, Label label, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GitHub API call to create a label.
        return Task.FromResult(label);
    }

    /// <inheritdoc/>
    public Task<Label> UpdateLabelAsync(string owner, string repo, string labelName, Label label, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GitHub API call to update an existing label.
        return Task.FromResult(label);
    }

    /// <inheritdoc/>
    public Task DeleteLabelAsync(string owner, string repo, string labelName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GitHub API call to delete a label.
        return Task.CompletedTask;
    }
}
