namespace SoloDevBoard.Domain.Entities.Workflows;

/// <summary>Represents a workflow file entry returned from a GitHub directory listing.</summary>
public sealed record WorkflowDirectoryEntry
{
    /// <summary>Gets the relative path of the workflow file in the repository.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Gets the file name of the workflow entry.</summary>
    public string Name { get; init; } = string.Empty;
}
