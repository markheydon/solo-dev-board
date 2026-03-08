namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a GitHub label.</summary>
public sealed record Label
{
    /// <summary>Gets the label name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the hexadecimal colour value of the label.</summary>
    public string Colour { get; init; } = string.Empty;

    /// <summary>Gets the label description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the short repository name this label belongs to (for example <c>solo-dev-board</c>, not <c>owner/solo-dev-board</c>).
    /// Use <see cref="Repository.FullName"/> when an owner-qualified repository value is required.
    /// </summary>
    public string RepositoryName { get; init; } = string.Empty;
}
