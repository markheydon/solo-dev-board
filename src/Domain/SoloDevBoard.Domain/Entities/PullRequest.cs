namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a GitHub pull request.</summary>
public sealed record PullRequest : IAggregate
{
    public int Id { get; init; }
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string AuthorLogin { get; init; } = string.Empty;
    public string HeadBranch { get; init; } = string.Empty;
    public string BaseBranch { get; init; } = string.Empty;
    public bool IsDraft { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
