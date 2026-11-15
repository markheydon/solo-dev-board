using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Domain.Entities.Repositories;

namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Discovers Projects v2 boards linked to active repositories.</summary>
public sealed class PlanningProjectBoardDiscoveryService(IGitHubService gitHubService) : IPlanningProjectBoardDiscoveryService
{
    /// <inheritdoc/>
    public async Task<PlanningProjectBoardDiscoveryDto> GetPlanningBoardOptionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var repositories = await gitHubService.GetActiveRepositoriesAsync(cancellationToken).ConfigureAwait(false);
        return await DiscoverPlanningBoardOptionsAsync(repositories, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<PlanningProjectBoardDiscoveryDto> GetPlanningBoardOptionsForRepositoriesAsync(
        IReadOnlyList<RepositoryDto> repositories,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        cancellationToken.ThrowIfCancellationRequested();

        return DiscoverPlanningBoardOptionsAsync(
            repositories.Select(repository => new RepositoryScanTarget(repository.FullName, repository.Name)),
            cancellationToken);
    }

    private async Task<PlanningProjectBoardDiscoveryDto> DiscoverPlanningBoardOptionsAsync(
        IReadOnlyList<Repository> repositories,
        CancellationToken cancellationToken)
    {
        return await DiscoverPlanningBoardOptionsAsync(
            repositories.Select(repository => new RepositoryScanTarget(repository.FullName, repository.Name)),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<PlanningProjectBoardDiscoveryDto> DiscoverPlanningBoardOptionsAsync(
        IEnumerable<RepositoryScanTarget> repositories,
        CancellationToken cancellationToken)
    {
        var optionsById = new Dictionary<string, PlanningBoardOptionDto>(StringComparer.Ordinal);
        var totalLinkedProjectCount = 0;
        var inaccessibleLinkedProjectCount = 0;

        foreach (var repository in repositories)
        {
            var ownerLogin = ParseOwnerLogin(repository.FullName);
            var discovery = await gitHubService
                .GetProjectBoardsForRepositoryAsync(ownerLogin, repository.Name, cancellationToken)
                .ConfigureAwait(false);

            totalLinkedProjectCount += discovery.TotalLinkedProjectCount;
            inaccessibleLinkedProjectCount += discovery.InaccessibleLinkedProjectCount;

            foreach (var projectBoard in discovery.SupportedProjectBoards)
            {
                if (!optionsById.ContainsKey(projectBoard.Id))
                {
                    optionsById[projectBoard.Id] = new PlanningBoardOptionDto(
                        projectBoard.Id,
                        projectBoard.Title,
                        projectBoard.OwnerLogin,
                        projectBoard.StatusFieldId);
                }
            }
        }

        var options = optionsById.Values
            .OrderBy(option => option.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PlanningProjectBoardDiscoveryDto(options, totalLinkedProjectCount, inaccessibleLinkedProjectCount);
    }

    private static string ParseOwnerLogin(string fullName)
    {
        var separatorIndex = fullName.IndexOf('/');
        return separatorIndex > 0 ? fullName[..separatorIndex] : fullName;
    }

    private readonly record struct RepositoryScanTarget(string FullName, string Name);
}
