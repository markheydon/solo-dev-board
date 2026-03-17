using SoloDevBoard.Domain;

namespace SoloDevBoard.Domain.Entities.Workflows;

/// <summary>Represents a GitHub Actions workflow run.</summary>
public sealed record WorkflowRun : IAggregate
{
    /// <summary>Gets the unique GitHub identifier of the workflow run.</summary>
    public int Id { get; init; }

    /// <summary>Gets the workflow name.</summary>
    public string WorkflowName { get; init; } = string.Empty;

    /// <summary>Gets the current workflow run status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets the workflow run conclusion.</summary>
    public string Conclusion { get; init; } = string.Empty;

    /// <summary>Gets the branch for the head commit.</summary>
    public string HeadBranch { get; init; } = string.Empty;

    /// <summary>Gets the SHA for the head commit.</summary>
    public string HeadSha { get; init; } = string.Empty;

    /// <summary>Gets the date and time when the workflow run was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the date and time when the workflow run was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Gets the URL to view the workflow run on GitHub.</summary>
    public string HtmlUrl { get; init; } = string.Empty;
}
