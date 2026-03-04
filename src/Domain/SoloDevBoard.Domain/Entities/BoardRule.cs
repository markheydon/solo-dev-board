namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents an automation rule for the board.</summary>
public sealed record BoardRule
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Trigger { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}
