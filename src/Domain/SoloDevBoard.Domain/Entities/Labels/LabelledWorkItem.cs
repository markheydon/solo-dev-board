namespace SoloDevBoard.Domain.Entities.Labels;

/// <summary>Represents an issue or pull request that currently carries a given label.</summary>
public sealed record LabelledWorkItem
{
    /// <summary>Gets the repository-scoped issue or pull request number.</summary>
    public int Number { get; init; }
}
