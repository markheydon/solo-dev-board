namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents a milestone option available for triage assignment.</summary>
/// <param name="Number">The repository-scoped milestone number.</param>
/// <param name="Title">The milestone title.</param>
public sealed record TriageMilestoneOptionDto(int Number, string Title);
