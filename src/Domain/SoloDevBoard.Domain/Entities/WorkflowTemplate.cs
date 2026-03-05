namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a reusable workflow template.</summary>
public sealed record WorkflowTemplate
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string YamlContent { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
