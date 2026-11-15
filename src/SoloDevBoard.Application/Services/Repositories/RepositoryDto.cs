namespace SoloDevBoard.Application.Services.Repositories;

/// <summary>Represents a repository returned from Application-layer repository operations.</summary>
/// <param name="Id">The unique GitHub identifier of the repository.</param>
/// <param name="Name">The short repository name.</param>
/// <param name="FullName">The fully-qualified repository name in owner/name form.</param>
/// <param name="Description">The repository description.</param>
/// <param name="Url">The web URL of the repository.</param>
/// <param name="IsPrivate"><see langword="true"/> if the repository is private; otherwise <see langword="false"/>.</param>
/// <param name="IsArchived"><see langword="true"/> if the repository is archived; otherwise <see langword="false"/>.</param>
/// <param name="CreatedAt">The date and time when the repository was created.</param>
/// <param name="UpdatedAt">The date and time when the repository was last updated.</param>
/// <param name="Topics">The GitHub repository topics returned by the catalogue list endpoints.</param>
/// <param name="IsOpenSource"><see langword="true"/> when the repository has the canonical open-source topic; otherwise <see langword="false"/>.</param>
public sealed record RepositoryDto(
    int Id,
    string Name,
    string FullName,
    string Description,
    string Url,
    bool IsPrivate,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Topics,
    bool IsOpenSource);
