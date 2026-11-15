using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Represents a GitHub issue.</summary>
public sealed record Issue : IAggregate
{
    /// <summary>Gets the unique GitHub identifier of the issue.</summary>
    public int Id { get; init; }

    /// <summary>Gets the repository-scoped issue number.</summary>
    public int Number { get; init; }

    /// <summary>Gets the title of the issue.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the web URL of the issue.</summary>
    public string HtmlUrl { get; init; } = string.Empty;

    /// <summary>Gets the body content of the issue.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Gets the current state of the issue.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>Gets the login of the issue author.</summary>
    public string AuthorLogin { get; init; } = string.Empty;

    /// <summary>Gets the labels currently applied to the issue.</summary>
    public IReadOnlyList<Label> Labels { get; init; } = [];

    /// <summary>Gets the milestone associated with the issue, if present.</summary>
    public Milestone? Milestone { get; init; }

    /// <summary>Gets the date and time when the issue was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the date and time when the issue was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
