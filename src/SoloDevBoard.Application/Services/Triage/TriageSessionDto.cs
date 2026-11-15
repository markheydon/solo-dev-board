namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents session state shared between Application and App layers for triage workflow.</summary>
/// <param name="SessionId">The unique identifier for the triage session.</param>
/// <param name="OwnerLogin">The repository owner login.</param>
/// <param name="RepositoryName">The repository name.</param>
/// <param name="IncludePullRequests">Indicates whether pull requests were included when the session started.</param>
/// <param name="Queue">The current triage queue.</param>
/// <param name="CurrentIndex">The zero-based index of the active queue item.</param>
/// <param name="SkippedItems">Items skipped for later review.</param>
/// <param name="ActionHistory">Recorded actions taken during the session.</param>
/// <param name="Progress">Computed progress information for the session.</param>
/// <param name="Summary">Computed summary information for the session.</param>
/// <param name="StartedAt">The UTC timestamp when the session started.</param>
public sealed record TriageSessionDto(
    Guid SessionId,
    string OwnerLogin,
    string RepositoryName,
    bool IncludePullRequests,
    IReadOnlyList<TriageItemDto> Queue,
    int CurrentIndex,
    IReadOnlyList<TriageItemDto> SkippedItems,
    IReadOnlyList<TriageActionDto> ActionHistory,
    TriageSessionProgressDto Progress,
    TriageSessionSummaryDto Summary,
    DateTimeOffset StartedAt)
{
    /// <summary>Gets the currently active queue item, or <see langword="null"/> when the queue is exhausted.</summary>
    public TriageItemDto? CurrentItem => CurrentIndex >= 0 && CurrentIndex < Queue.Count ? Queue[CurrentIndex] : null;
}
