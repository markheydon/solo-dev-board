namespace SoloDevBoard.Domain.Entities;

/// <summary>Represents a GitHub repository.</summary>
public sealed record Repository : IAggregate
{
    /// <summary>Gets the unique GitHub identifier of the repository.</summary>
    public int Id { get; init; }

    /// <summary>Gets the short repository name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the fully-qualified repository name in owner/name form.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets the repository description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the web URL of the repository.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the repository is private.</summary>
    public bool IsPrivate { get; init; }

    /// <summary>Gets a value indicating whether the repository is archived.</summary>
    public bool IsArchived { get; init; }

    /// <summary>Gets the date and time when the repository was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the date and time when the repository was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
