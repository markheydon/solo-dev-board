using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Scans one open-source repository and returns a snapshot row.</summary>
public interface IGitHubOpenSourceInventoryScanner
{
    Task<RepositoryInventorySnapshot> ScanRepositoryAsync(
        InventoryRun run,
        string repositoryFullName,
        bool isArchived,
        bool isFork,
        bool isPrivate,
        string repositoryDescription,
        string? standardContent,
        CancellationToken cancellationToken);
}