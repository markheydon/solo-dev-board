namespace SoloDevBoard.Domain.Entities.Workflows;

/// <summary>Represents a workflow file stored in a GitHub repository.</summary>
public sealed record WorkflowFile
{
    /// <summary>Gets the relative path of the workflow file in the repository.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Gets the decoded YAML content of the workflow file.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Gets the Git blob SHA used for optimistic concurrency when updating the file.</summary>
    public string? Sha { get; init; }
}
