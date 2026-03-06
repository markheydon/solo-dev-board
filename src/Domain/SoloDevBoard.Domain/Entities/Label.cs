namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a GitHub label.</summary>
public sealed record Label
{
    /// <summary>Gets the label name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the hexadecimal colour value of the label.</summary>
    public string Colour { get; init; } = string.Empty;

    /// <summary>Gets the label description.</summary>
    public string Description { get; init; } = string.Empty;
}
