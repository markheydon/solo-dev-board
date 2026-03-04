namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a GitHub milestone.</summary>
public sealed record Milestone : IAggregate
{
    public int Id { get; init; }
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public DateTimeOffset? DueOn { get; init; }
    public int OpenIssues { get; init; }
    public int ClosedIssues { get; init; }
}
