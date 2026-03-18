namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents session state shared between Application and App layers for triage workflow.</summary>
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
