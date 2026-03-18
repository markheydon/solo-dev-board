using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.Repositories;
using SoloDevBoard.Domain.Entities.Triage;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Services.GitHub;

/// <summary>Provides access to GitHub API operations.</summary>
public interface IGitHubService
{
    /// <summary>Retrieves repositories accessible to the authenticated GitHub user.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of repositories visible to the authenticated user.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves active repositories accessible to the authenticated GitHub user.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of non-archived repositories visible to the authenticated user.</returns>
    Task<IReadOnlyList<Repository>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves repositories for the specified owner.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of repositories for the specified owner.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>Retrieves active repositories for the specified owner.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of non-archived repositories for the specified owner.</returns>
    Task<IReadOnlyList<Repository>> GetActiveRepositoriesAsync(string owner, CancellationToken cancellationToken = default);

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

    /// <summary>Retrieves recent workflow runs for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of recent workflow runs for the specified repository.</returns>
    Task<IReadOnlyList<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo, CancellationToken cancellationToken = default);

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

    /// <summary>Replaces all labels on a triage item with the specified set.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="itemNumber">The repository-scoped item number.</param>
    /// <param name="labelNames">The label names to set on the item.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous label assignment operation.</returns>
    Task ApplyLabelsToTriageItemAsync(string owner, string repo, int itemNumber, IReadOnlyList<string> labelNames, CancellationToken cancellationToken = default);

    /// <summary>Assigns or clears a milestone on a triage item.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="itemNumber">The repository-scoped item number.</param>
    /// <param name="milestoneNumber">The milestone number to assign, or <see langword="null"/> to clear.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous milestone assignment operation.</returns>
    Task AssignMilestoneToTriageItemAsync(string owner, string repo, int itemNumber, int? milestoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Adds a triage item to a GitHub Project v2 board.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="itemNumber">The repository-scoped item number.</param>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The created project-item node identifier.</returns>
    Task<string> AddTriageItemToProjectBoardAsync(string owner, string repo, int itemNumber, string projectId, CancellationToken cancellationToken = default);

    /// <summary>Closes a triage item as duplicate and records a duplicate reference comment.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="itemType">The triage item type.</param>
    /// <param name="itemNumber">The repository-scoped item number.</param>
    /// <param name="duplicateReference">The canonical issue or pull-request reference.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous duplicate closure operation.</returns>
    Task CloseTriageItemAsDuplicateAsync(string owner, string repo, GitHubTriageItemType itemType, int itemNumber, string duplicateReference, CancellationToken cancellationToken = default);
}
