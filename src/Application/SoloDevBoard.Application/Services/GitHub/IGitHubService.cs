using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Migration;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.Planning;
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
    /// <param name="forceReload">When <see langword="true" />, bypasses any cached repository catalogue before loading.</param>
    /// <returns>A read-only list of non-archived repositories visible to the authenticated user.</returns>
    Task<IReadOnlyList<Repository>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default, bool forceReload = false);

    /// <summary>Retrieves repositories for the specified owner.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of repositories for the specified owner.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>Retrieves active repositories for the specified owner.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <param name="forceReload">When <see langword="true" />, bypasses any cached repository catalogue before loading.</param>
    /// <returns>A read-only list of non-archived repositories for the specified owner.</returns>
    Task<IReadOnlyList<Repository>> GetActiveRepositoriesAsync(string owner, CancellationToken cancellationToken = default, bool forceReload = false);

    /// <summary>Retrieves all issues for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of issues for the specified repository.</returns>
    Task<IReadOnlyList<Issue>> GetIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves issues for the specified repository filtered by state.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="state">The issue state filter: <c>open</c>, <c>closed</c>, or <c>all</c>.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of issues for the specified repository.</returns>
    Task<IReadOnlyList<Issue>> GetIssuesAsync(string owner, string repo, string state, CancellationToken cancellationToken);

    /// <summary>Retrieves all pull requests for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of pull requests for the specified repository.</returns>
    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves pull requests for the specified repository filtered by state.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="state">The pull request state filter: <c>open</c>, <c>closed</c>, or <c>all</c>.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of pull requests for the specified repository.</returns>
    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, string state, CancellationToken cancellationToken);

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
    /// <param name="labelNames">The label names to set on the item, replacing any existing labels.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous label assignment operation.</returns>
    Task ApplyLabelsToTriageItemAsync(string owner, string repo, int itemNumber, IReadOnlyList<string> labelNames, CancellationToken cancellationToken = default);

    /// <summary>Adds labels to a triage item without removing existing labels.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="itemNumber">The repository-scoped item number.</param>
    /// <param name="labelNames">The label names to add to the item.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous label addition operation.</returns>
    Task AddLabelsToTriageItemAsync(string owner, string repo, int itemNumber, IReadOnlyList<string> labelNames, CancellationToken cancellationToken = default);

    /// <summary>Removes one label from a triage item when present.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="itemNumber">The repository-scoped item number.</param>
    /// <param name="labelName">The label name to remove from the item.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous label removal operation.</returns>
    Task RemoveLabelFromTriageItemAsync(string owner, string repo, int itemNumber, string labelName, CancellationToken cancellationToken = default);

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

    /// <summary>Retrieves GitHub Project v2 boards available for a repository with status options.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Supported project boards plus visibility metadata for linked boards that could not be read.</returns>
    Task<RepositoryProjectBoardDiscoveryResult> GetProjectBoardsForRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves supported board rules metadata for the specified GitHub Project v2 board.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The board rules definition metadata.</returns>
    Task<BoardRulesDefinitionDto> GetBoardRulesDefinitionAsync(string owner, string repo, string projectId, CancellationToken cancellationToken = default);

    /// <summary>Updates the status field for a project board item.</summary>
    /// <param name="projectId">The project-board node identifier.</param>
    /// <param name="projectItemId">The project-item node identifier.</param>
    /// <param name="statusFieldId">The project status-field node identifier.</param>
    /// <param name="statusOptionId">The selected status-option node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous status update operation.</returns>
    Task UpdateProjectBoardItemStatusAsync(
        string projectId,
        string projectItemId,
        string statusFieldId,
        string statusOptionId,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves all project board items with Status, optional Focus Order, and linked content.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The project board item catalogue including discovered field identifiers.</returns>
    Task<ProjectBoardItemCatalogue> GetProjectBoardItemsAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>Sets the Focus Order number field on a project board item.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="projectItemId">The project-item node identifier.</param>
    /// <param name="focusOrderFieldId">The Focus Order field node identifier.</param>
    /// <param name="focusOrder">The Focus Order value to set.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task UpdateProjectBoardItemFocusOrderAsync(
        string projectId,
        string projectItemId,
        string focusOrderFieldId,
        double focusOrder,
        CancellationToken cancellationToken = default);

    /// <summary>Clears the Focus Order number field on a project board item.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="projectItemId">The project-item node identifier.</param>
    /// <param name="focusOrderFieldId">The Focus Order field node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous clear operation.</returns>
    Task ClearProjectBoardItemFocusOrderAsync(
        string projectId,
        string projectItemId,
        string focusOrderFieldId,
        CancellationToken cancellationToken = default);

    /// <summary>Discovers linked project boards with full Status field structure for migration.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Supported boards with Status options and linked-project visibility metadata.</returns>
    Task<ProjectBoardDiscovery> DiscoverProjectBoardStatusStructuresAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the Status field structure for a project board.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The Status field structure for the project board.</returns>
    Task<ProjectBoardStatusStructure> GetProjectBoardStatusStructureAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the GitHub node identifier for a repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The repository node identifier.</returns>
    Task<string> GetRepositoryNodeIdAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Creates a repository-linked Projects v2 board.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="title">The title for the new project board.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The Status field structure for the newly created board.</returns>
    Task<ProjectBoardStatusStructure> CreateRepositoryLinkedProjectAsync(string owner, string repo, string title, CancellationToken cancellationToken = default);

    /// <summary>Replaces the Status field options on a project board.</summary>
    /// <param name="projectId">The GitHub Project v2 node identifier.</param>
    /// <param name="statusFieldId">The Status field node identifier.</param>
    /// <param name="options">The complete Status option list to persist.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated Status field structure.</returns>
    Task<ProjectBoardStatusStructure> UpdateProjectBoardStatusOptionsAsync(
        string projectId,
        string statusFieldId,
        IReadOnlyList<ProjectBoardStatusStructureOption> options,
        CancellationToken cancellationToken = default);

    /// <summary>Closes a triage item as duplicate and records a duplicate reference comment.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="itemType">The triage item type.</param>
    /// <param name="itemNumber">The repository-scoped item number.</param>
    /// <param name="duplicateReference">The canonical issue or pull-request reference.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous duplicate closure operation.</returns>
    Task CloseTriageItemAsDuplicateAsync(string owner, string repo, GitHubTriageItemType itemType, int itemNumber, string duplicateReference, CancellationToken cancellationToken = default);

    #region Work-item catalogue methods

    /// <summary>
    /// Retrieves review-pending metadata for open pull requests in the specified repository.
    /// Paginates through all open pull requests when a repository has more than 100.
    /// </summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Review metadata keyed by pull request number.</returns>
    Task<IReadOnlyList<PullRequestReviewMetadata>> GetOpenPullRequestReviewMetadataAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tracked sub-issue summary counts for the specified open issues.
    /// Queries only the requested issue numbers and paginates tracked sub-issues for accurate completion counts.
    /// </summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="issueNumbers">Repository-scoped issue numbers to inspect.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Sub-issue totals for issues that expose tracked sub-issues.</returns>
    Task<IReadOnlyList<IssueSubIssueSummary>> GetIssueSubIssueSummariesAsync(
        string owner,
        string repo,
        IReadOnlyList<int> issueNumbers,
        CancellationToken cancellationToken = default);

    #endregion
}
