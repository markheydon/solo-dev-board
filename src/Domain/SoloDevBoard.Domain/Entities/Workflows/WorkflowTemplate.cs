namespace SoloDevBoard.Domain.Entities.Workflows;

/// <summary>Represents a reusable workflow template.</summary>
public sealed record WorkflowTemplate
{
    /// <summary>Gets the unique identifier of the workflow template.</summary>
    public int Id { get; init; }

    /// <summary>Gets the display name of the workflow template.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the description of the workflow template.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the template category used for browsing and filtering.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Gets the tags associated with the workflow template.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Gets the relative workflow file path applied to repositories.</summary>
    public string WorkflowFilePath { get; init; } = string.Empty;

    /// <summary>Gets a short description of when the workflow runs.</summary>
    public string TriggerDescription { get; init; } = string.Empty;

    /// <summary>Gets the YAML content of the workflow template.</summary>
    public string YamlContent { get; init; } = string.Empty;

    /// <summary>Gets the date and time when the template was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
