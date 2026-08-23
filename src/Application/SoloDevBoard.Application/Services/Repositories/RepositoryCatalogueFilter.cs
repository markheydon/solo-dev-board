namespace SoloDevBoard.Application.Services.Repositories;

/// <summary>Built-in catalogue filters for the Repositories page and downstream OSS-only views.</summary>
public enum RepositoryCatalogueFilter
{
    /// <summary>Show every loaded catalogue repository.</summary>
    All,

    /// <summary>Show only repositories classified as open source.</summary>
    OpenSource,

    /// <summary>Show only repositories that are not classified as open source.</summary>
    NotOpenSource,
}

/// <summary>Applies built-in catalogue filters using the shared open-source classification rule.</summary>
public static class RepositoryCatalogueFilters
{
    /// <summary>Filters the supplied catalogue using the requested built-in filter.</summary>
    /// <param name="repositories">The loaded catalogue repositories.</param>
    /// <param name="filter">The catalogue filter to apply.</param>
    /// <returns>A filtered view of <paramref name="repositories"/>.</returns>
    public static IReadOnlyList<RepositoryDto> Apply(
        IReadOnlyList<RepositoryDto> repositories,
        RepositoryCatalogueFilter filter)
        => filter switch
        {
            RepositoryCatalogueFilter.OpenSource => repositories.Where(static repository => repository.IsOpenSource).ToArray(),
            RepositoryCatalogueFilter.NotOpenSource => repositories.Where(static repository => !repository.IsOpenSource).ToArray(),
            _ => repositories,
        };
}
