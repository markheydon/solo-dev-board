namespace SoloDevBoard.Domain.Entities.BoardRules;

/// <summary>Represents a column on the solo dev board.</summary>
public sealed record BoardColumn
{
    /// <summary>Gets the unique identifier of the board column.</summary>
    public int Id { get; init; }

    /// <summary>Gets the display name of the board column.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the display order of the board column.</summary>
    public int Order { get; init; }

    /// <summary>Gets the label filters used to route issues into this column.</summary>
    public IReadOnlyList<string> LabelFilters { get; init; } = [];
}
