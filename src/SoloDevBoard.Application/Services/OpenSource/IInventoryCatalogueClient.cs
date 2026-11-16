namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Lists open-source catalogue repositories for inventory scope.</summary>
public interface IInventoryCatalogueClient
{
    Task<IReadOnlyList<InventoryCatalogueRepository>> GetOpenSourceRepositoriesAsync(CancellationToken cancellationToken);
}

/// <summary>Repository row in the inventory walk scope.</summary>
public sealed record InventoryCatalogueRepository(
    string FullName,
    string HtmlUrl,
    bool IsArchived,
    bool IsFork,
    bool IsPrivate,
    string Description);