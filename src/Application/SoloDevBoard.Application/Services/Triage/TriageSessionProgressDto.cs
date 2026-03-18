namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents computed progress information for a triage session.</summary>
public sealed record TriageSessionProgressDto(int TotalItems, int ProcessedItems, int RemainingItems, int SkippedItems);
