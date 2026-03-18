namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents a recorded action in a triage session.</summary>
public sealed record TriageActionDto(
    TriageActionTypeDto ActionType,
    TriageItemTypeDto ItemType,
    int ItemNumber,
    string RepositoryFullName,
    string Detail,
    DateTimeOffset OccurredAt);
