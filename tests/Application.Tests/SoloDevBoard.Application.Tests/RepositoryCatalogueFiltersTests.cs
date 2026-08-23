using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="RepositoryCatalogueFilters"/>.</summary>
public sealed class RepositoryCatalogueFiltersTests
{
    private static readonly IReadOnlyList<RepositoryDto> Catalogue =
    [
        CreateRepository("oss-repo", isOpenSource: true),
        CreateRepository("customer-repo", isOpenSource: false),
        CreateRepository("private-oss", isOpenSource: true, isPrivate: true),
    ];

    [Fact]
    public void Apply_AllFilter_ReturnsFullCatalogue()
    {
        var result = RepositoryCatalogueFilters.Apply(Catalogue, RepositoryCatalogueFilter.All);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Apply_OpenSourceFilter_ReturnsOnlyOpenSourceRepositories()
    {
        var result = RepositoryCatalogueFilters.Apply(Catalogue, RepositoryCatalogueFilter.OpenSource);

        Assert.Equal(2, result.Count);
        Assert.All(result, repository => Assert.True(repository.IsOpenSource));
    }

    [Fact]
    public void Apply_NotOpenSourceFilter_ReturnsComplement()
    {
        var result = RepositoryCatalogueFilters.Apply(Catalogue, RepositoryCatalogueFilter.NotOpenSource);

        var repository = Assert.Single(result);
        Assert.Equal("customer-repo", repository.Name);
        Assert.False(repository.IsOpenSource);
    }

    private static RepositoryDto CreateRepository(string name, bool isOpenSource, bool isPrivate = false)
        => new(
            1,
            name,
            $"owner/{name}",
            string.Empty,
            $"https://github.com/owner/{name}",
            isPrivate,
            false,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            isOpenSource ? ["open-source"] : [],
            isOpenSource);
}
