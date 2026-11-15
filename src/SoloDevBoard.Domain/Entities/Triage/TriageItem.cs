using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents a single triage queue item.</summary>
public sealed record TriageItem : IAggregate
{
    /// <summary>Gets the triage item type.</summary>
    public TriageItemType ItemType { get; init; }

    /// <summary>Gets the unique GitHub identifier for the item.</summary>
    public int Id { get; init; }

    /// <summary>Gets the repository-scoped item number.</summary>
    public int Number { get; init; }

    /// <summary>Gets the repository full name in owner/repository format.</summary>
    public string RepositoryFullName { get; init; } = string.Empty;

    /// <summary>Gets the item title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the web URL for the item.</summary>
    public string HtmlUrl { get; init; } = string.Empty;

    /// <summary>Gets the item body content.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Gets the current state for the item.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>Gets the author login for the item.</summary>
    public string AuthorLogin { get; init; } = string.Empty;

    /// <summary>Gets the labels currently assigned to the item.</summary>
    public IReadOnlyList<Label> Labels { get; init; } = [];

    /// <summary>Gets the milestone currently assigned to the item, if present.</summary>
    public Milestone? Milestone { get; init; }

    /// <summary>Gets the item creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the last update timestamp for the item.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
