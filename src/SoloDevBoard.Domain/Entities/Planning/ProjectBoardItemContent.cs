using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Domain.Entities.Planning;

/// <summary>Represents the GitHub issue or pull request linked to a project board item.</summary>
public sealed record ProjectBoardItemContent
{
    /// <summary>Gets the linked content type.</summary>
    public TriageItemType ContentType { get; init; }

    /// <summary>Gets the repository-scoped issue or pull request number.</summary>
    public int Number { get; init; }

    /// <summary>Gets the repository owner login.</summary>
    public string RepositoryOwner { get; init; } = string.Empty;

    /// <summary>Gets the repository name.</summary>
    public string RepositoryName { get; init; } = string.Empty;

    /// <summary>Gets the issue or pull request title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the canonical GitHub URL for the content.</summary>
    public string Url { get; init; } = string.Empty;
}
