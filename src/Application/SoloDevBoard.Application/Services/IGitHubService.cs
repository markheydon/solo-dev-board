using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Provides access to GitHub API operations.</summary>
public interface IGitHubService
{
    /// <summary>Retrieves all repositories for the specified owner.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of repositories for the specified owner.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all issues for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of issues for the specified repository.</returns>
    Task<IReadOnlyList<Issue>> GetIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all pull requests for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of pull requests for the specified repository.</returns>
    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all milestones for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of milestones for the specified repository.</returns>
    Task<IReadOnlyList<Milestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all labels for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of labels for the specified repository.</returns>
    Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Creates a new label in the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="label">The label details to create.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The created label.</returns>
    Task<Label> CreateLabelAsync(string owner, string repo, Label label, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing label in the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="labelName">The current label name to update.</param>
    /// <param name="label">The new label details to apply.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated label.</returns>
    Task<Label> UpdateLabelAsync(string owner, string repo, string labelName, Label label, CancellationToken cancellationToken = default);

    /// <summary>Deletes a label from the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="labelName">The label name to delete.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteLabelAsync(string owner, string repo, string labelName, CancellationToken cancellationToken = default);
}
