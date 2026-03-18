namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents a recorded action in a triage session.</summary>
/// <param name="ActionType">The type of action that was performed.</param>
/// <param name="ItemType">The triage item type for the affected item.</param>
/// <param name="ItemNumber">The repository-scoped number of the affected issue or pull request.</param>
/// <param name="RepositoryFullName">The full repository name in <c>owner/repo</c> format.</param>
/// <param name="Detail">Additional detail describing the action.</param>
/// <param name="OccurredAt">The UTC timestamp when the action occurred.</param>
public sealed record TriageActionDto(
    TriageActionTypeDto ActionType,
    TriageItemTypeDto ItemType,
    int ItemNumber,
    string RepositoryFullName,
    string Detail,
    DateTimeOffset OccurredAt);
