namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents an automation rule for the board.</summary>
public sealed record BoardRule
{
    /// <summary>Gets the unique identifier of the board rule.</summary>
    public int Id { get; init; }

    /// <summary>Gets the name of the board rule.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the trigger condition expression for the rule.</summary>
    public string Trigger { get; init; } = string.Empty;

    /// <summary>Gets the action expression performed when the trigger matches.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the board rule is enabled.</summary>
    public bool IsEnabled { get; init; }
}
