namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a GitHub issue.</summary>
public sealed record Issue : IAggregate
{
    public int Id { get; init; }
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string AuthorLogin { get; init; } = string.Empty;
    public IReadOnlyList<Label> Labels { get; init; } = [];
    public Milestone? Milestone { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
