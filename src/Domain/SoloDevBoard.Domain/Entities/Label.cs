namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a GitHub label.</summary>
public sealed record Label
{
    public string Name { get; init; } = string.Empty;
    public string Colour { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
