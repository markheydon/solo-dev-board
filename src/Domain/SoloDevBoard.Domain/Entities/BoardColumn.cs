namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a column on the solo dev board.</summary>
public sealed record BoardColumn
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
    public IReadOnlyList<string> LabelFilters { get; init; } = [];
}
