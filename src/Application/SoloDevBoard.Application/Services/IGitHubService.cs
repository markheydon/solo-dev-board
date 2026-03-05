using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Provides access to GitHub API operations.</summary>
public interface IGitHubService
{
    /// <summary>Retrieves all repositories for the specified owner.</summary>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all issues for the specified repository.</summary>
    Task<IReadOnlyList<Issue>> GetIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all pull requests for the specified repository.</summary>
    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all milestones for the specified repository.</summary>
    Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all labels for the specified repository.</summary>
    Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Creates a new label in the specified repository.</summary>
    Task<Label> CreateLabelAsync(string owner, string repo, Label label, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing label in the specified repository.</summary>
    Task<Label> UpdateLabelAsync(string owner, string repo, string labelName, Label label, CancellationToken cancellationToken = default);

    /// <summary>Deletes a label from the specified repository.</summary>
    Task DeleteLabelAsync(string owner, string repo, string labelName, CancellationToken cancellationToken = default);
}
