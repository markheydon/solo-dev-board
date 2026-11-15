namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a built-in recommended label strategy.</summary>
/// <param name="Id">The stable strategy identifier.</param>
/// <param name="Name">The display name for the strategy.</param>
/// <param name="Description">The strategy description shown to users.</param>
public sealed record RecommendedLabelStrategyDto(string Id, string Name, string Description);
