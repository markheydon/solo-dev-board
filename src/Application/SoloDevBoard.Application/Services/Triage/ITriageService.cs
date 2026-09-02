namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Provides one-at-a-time triage session orchestration operations.</summary>
public interface ITriageService
{
    /// <summary>Starts a triage session for a repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="includePullRequests"><see langword="true"/> to include pull requests in the triage queue; otherwise, <see langword="false"/>.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The initialised triage session DTO.</returns>
    Task<TriageSessionDto> StartSessionAsync(string owner, string repo, bool includePullRequests = false, CancellationToken cancellationToken = default);

    /// <summary>Advances the session to the next queue item.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated triage session DTO.</returns>
    Task<TriageSessionDto> AdvanceSessionAsync(TriageSessionDto session, CancellationToken cancellationToken = default);

    /// <summary>Skips the currently active item and records a skip action.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="reason">An optional user-provided skip reason.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated triage session DTO.</returns>
    Task<TriageSessionDto> SkipCurrentItemAsync(TriageSessionDto session, string reason, CancellationToken cancellationToken = default);

    /// <summary>Appends skipped items to the end of the queue for revisit.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated triage session DTO.</returns>
    Task<TriageSessionDto> RevisitSkippedItemsAsync(TriageSessionDto session, CancellationToken cancellationToken = default);

    /// <summary>Applies a label to the currently active session item.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="labelName">The label name to apply.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated triage session DTO.</returns>
    Task<TriageSessionDto> ApplyLabelToCurrentItemAsync(TriageSessionDto session, string labelName, CancellationToken cancellationToken = default);

    /// <summary>Retrieves milestone options for the repository in the active triage session.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of milestone options sorted by title.</returns>
    Task<IReadOnlyList<TriageMilestoneOptionDto>> GetMilestoneOptionsAsync(TriageSessionDto session, CancellationToken cancellationToken = default);

    /// <summary>Retrieves project-board options for the repository in the active triage session.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Supported project-board options and visibility metadata sorted by title.</returns>
    Task<TriageProjectBoardDiscoveryDto> GetProjectBoardOptionsAsync(TriageSessionDto session, CancellationToken cancellationToken = default);

    /// <summary>Assigns or clears a milestone on the current session item.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="milestoneNumber">The milestone number to assign, or <see langword="null"/> to clear.</param>
    /// <param name="milestoneTitle">The milestone title associated with <paramref name="milestoneNumber"/>.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated triage session DTO.</returns>
    Task<TriageSessionDto> AssignMilestoneToCurrentItemAsync(TriageSessionDto session, int? milestoneNumber, string? milestoneTitle, CancellationToken cancellationToken = default);

    /// <summary>Adds the current session item to a project board and sets its status.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="projectId">The project-board node identifier.</param>
    /// <param name="projectTitle">The project-board display title.</param>
    /// <param name="statusFieldId">The project status-field node identifier.</param>
    /// <param name="statusOptionId">The selected project status-option node identifier.</param>
    /// <param name="statusOptionName">The selected project status-option display name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated triage session DTO.</returns>
    Task<TriageSessionDto> AddCurrentItemToProjectBoardAsync(
        TriageSessionDto session,
        string projectId,
        string projectTitle,
        string statusFieldId,
        string statusOptionId,
        string statusOptionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies optional process metadata (label, milestone, project board) in order, then advances the session.
    /// Empty or unchanged fields are no-ops. Stops on the first write failure without advancing.
    /// </summary>
    /// <param name="session">The current session state.</param>
    /// <param name="request">The optional metadata writes to apply before advancing.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated triage session DTO after successful writes and queue advancement.</returns>
    Task<TriageSessionDto> ProcessAndAdvanceCurrentItemAsync(
        TriageSessionDto session,
        TriageProcessCommitRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Closes the current session item as a duplicate with a canonical reference.</summary>
    /// <param name="session">The current session state.</param>
    /// <param name="duplicateReference">The canonical duplicate reference to include in the closure comment.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated triage session DTO.</returns>
    Task<TriageSessionDto> CloseCurrentItemAsDuplicateAsync(
        TriageSessionDto session,
        string duplicateReference,
        CancellationToken cancellationToken = default);

    /// <summary>Builds the latest triage session summary from current session state.</summary>
    /// <param name="session">The current session state.</param>
    /// <returns>The computed triage session summary DTO.</returns>
    TriageSessionSummaryDto BuildSessionSummary(TriageSessionDto session);
}
