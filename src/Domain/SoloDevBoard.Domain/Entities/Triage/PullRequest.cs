namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents a GitHub pull request.</summary>
public sealed record PullRequest : IAggregate
{
    /// <summary>Gets the unique GitHub identifier of the pull request.</summary>
    public int Id { get; init; }

    /// <summary>Gets the repository-scoped pull request number.</summary>
    public int Number { get; init; }

    /// <summary>Gets the title of the pull request.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the web URL of the pull request.</summary>
    public string HtmlUrl { get; init; } = string.Empty;

    /// <summary>Gets the body content of the pull request.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Gets the current state of the pull request.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>Gets the login of the pull request author.</summary>
    public string AuthorLogin { get; init; } = string.Empty;

    /// <summary>Gets the head branch name of the pull request.</summary>
    public string HeadBranch { get; init; } = string.Empty;

    /// <summary>Gets the base branch name targeted by the pull request.</summary>
    public string BaseBranch { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the pull request is a draft.</summary>
    public bool IsDraft { get; init; }

    /// <summary>Gets the date and time when the pull request was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the date and time when the pull request was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
