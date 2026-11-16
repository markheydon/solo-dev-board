using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>
/// Open-source inventory scope from the linked catalogue (same source as the Repositories page),
/// not the GitHub App installation repository list.
/// </summary>
public sealed class RepositoryCatalogueInventoryClient(IRepositoryService repositoryService) : IInventoryCatalogueClient
{
    private readonly IRepositoryService _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));

    public async Task<IReadOnlyList<InventoryCatalogueRepository>> GetOpenSourceRepositoriesAsync(CancellationToken cancellationToken)
    {
        var repositories = await _repositoryService.GetRepositoriesAsync(cancellationToken).ConfigureAwait(false);
        return repositories
            .Where(static repository => repository.IsOpenSource)
            .Select(static repository => new InventoryCatalogueRepository(
                repository.FullName,
                repository.Url,
                repository.IsArchived,
                IsFork: false,
                repository.IsPrivate,
                string.Empty))
            .ToArray();
    }
}