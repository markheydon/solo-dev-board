namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents computed progress information for a triage session.</summary>
/// <param name="TotalItems">The total number of items currently in the active queue.</param>
/// <param name="ProcessedItems">The number of items already processed in the active queue.</param>
/// <param name="RemainingItems">The number of items remaining in the active queue.</param>
/// <param name="SkippedItems">The number of items skipped for later review.</param>
public sealed record TriageSessionProgressDto(int TotalItems, int ProcessedItems, int RemainingItems, int SkippedItems);
