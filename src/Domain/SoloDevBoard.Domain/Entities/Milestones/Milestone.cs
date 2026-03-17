using SoloDevBoard.Domain;

namespace SoloDevBoard.Domain.Entities.Milestones;

/// <summary>Represents a GitHub milestone.</summary>
public sealed record Milestone : IAggregate
{
    /// <summary>Gets the unique GitHub identifier of the milestone.</summary>
    public int Id { get; init; }

    /// <summary>Gets the repository-scoped milestone number.</summary>
    public int Number { get; init; }

    /// <summary>Gets the title of the milestone.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the description of the milestone.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the current state of the milestone.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>Gets the due date of the milestone, if set.</summary>
    public DateTimeOffset? DueOn { get; init; }

    /// <summary>Gets the number of open issues associated with the milestone.</summary>
    public int OpenIssues { get; init; }

    /// <summary>Gets the number of closed issues associated with the milestone.</summary>
    public int ClosedIssues { get; init; }
}
