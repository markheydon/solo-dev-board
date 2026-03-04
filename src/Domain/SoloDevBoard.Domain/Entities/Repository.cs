namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a GitHub repository.</summary>
public sealed record Repository : IAggregate
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public bool IsPrivate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
