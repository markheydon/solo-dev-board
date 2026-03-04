using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Infrastructure;

/// <summary>Provides access to GitHub API operations.</summary>
public interface IGitHubService
{
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(string owner, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issue>> GetIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task<Label> CreateLabelAsync(string owner, string repo, Label label, CancellationToken cancellationToken = default);
    Task<Label> UpdateLabelAsync(string owner, string repo, string labelName, Label label, CancellationToken cancellationToken = default);
    Task DeleteLabelAsync(string owner, string repo, string labelName, CancellationToken cancellationToken = default);
}
