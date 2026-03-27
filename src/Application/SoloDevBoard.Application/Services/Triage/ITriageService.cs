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

    /// <summary>Builds the latest triage session summary from current session state.</summary>
    /// <param name="session">The current session state.</param>
    /// <returns>The computed triage session summary DTO.</returns>
    TriageSessionSummaryDto BuildSessionSummary(TriageSessionDto session);
}
