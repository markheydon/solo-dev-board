using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Builds shared milestone options and skip lists for Iteration Planning bulk assignment.</summary>
public static class PlanningBulkMilestoneAssigner
{
    /// <summary>
    /// Builds milestone titles that exist on every repository represented by the selected items.
    /// </summary>
    /// <param name="selectedItems">Checked Up Next items in the current batch.</param>
    /// <param name="milestonesByRepository">Milestone catalogues keyed by repository full name.</param>
    /// <returns>Milestone titles shared across all selected repositories, ordered by title.</returns>
    public static IReadOnlyList<IterationPlanningMilestoneOptionDto> BuildMilestoneOptions(
        IReadOnlyList<IterationPlanningUpNextItemDto> selectedItems,
        IReadOnlyDictionary<string, IReadOnlyList<Milestone>> milestonesByRepository)
    {
        ArgumentNullException.ThrowIfNull(selectedItems);
        ArgumentNullException.ThrowIfNull(milestonesByRepository);

        if (selectedItems.Count == 0)
        {
            return [];
        }

        var requiredRepositories = selectedItems
            .Select(static item => item.RepositoryFullName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var titlesByRepository = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in selectedItems)
        {
            if (!milestonesByRepository.TryGetValue(item.RepositoryFullName, out var milestones))
            {
                continue;
            }

            foreach (var milestone in milestones)
            {
                if (string.IsNullOrWhiteSpace(milestone.Title))
                {
                    continue;
                }

                if (!titlesByRepository.TryGetValue(milestone.Title, out var repositories))
                {
                    repositories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    titlesByRepository[milestone.Title] = repositories;
                }

                repositories.Add(item.RepositoryFullName);
            }
        }

        return titlesByRepository
            .Where(entry => requiredRepositories.All(repository => entry.Value.Contains(repository)))
            .OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new IterationPlanningMilestoneOptionDto(
                entry.Key,
                requiredRepositories
                    .Where(repository => entry.Value.Contains(repository))
                    .OrderBy(static repository => repository, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .ToArray();
    }

    /// <summary>
    /// Returns repositories in the selection that do not expose the requested milestone title.
    /// </summary>
    /// <param name="selectedItems">Checked Up Next items in the current batch.</param>
    /// <param name="milestonesByRepository">Milestone catalogues keyed by repository full name.</param>
    /// <param name="milestoneTitle">The milestone title to assign.</param>
    /// <returns>Distinct repository full names to skip, ordered alphabetically.</returns>
    public static IReadOnlyList<string> BuildSkipList(
        IReadOnlyList<IterationPlanningUpNextItemDto> selectedItems,
        IReadOnlyDictionary<string, IReadOnlyList<Milestone>> milestonesByRepository,
        string milestoneTitle)
    {
        ArgumentNullException.ThrowIfNull(selectedItems);
        ArgumentNullException.ThrowIfNull(milestonesByRepository);
        ArgumentException.ThrowIfNullOrWhiteSpace(milestoneTitle);

        var skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in selectedItems)
        {
            if (!milestonesByRepository.TryGetValue(item.RepositoryFullName, out var milestones)
                || !milestones.Any(milestone =>
                    milestone.Title.Equals(milestoneTitle, StringComparison.OrdinalIgnoreCase)))
            {
                skipped.Add(item.RepositoryFullName);
            }
        }

        return skipped
            .OrderBy(static repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Resolves the repository-scoped milestone number for a title, when present.
    /// </summary>
    /// <param name="milestones">Milestones on the target repository.</param>
    /// <param name="milestoneTitle">The milestone title to resolve.</param>
    /// <returns>The milestone number when found; otherwise, <see langword="null"/>.</returns>
    public static int? ResolveMilestoneNumber(
        IReadOnlyList<Milestone> milestones,
        string milestoneTitle)
    {
        ArgumentNullException.ThrowIfNull(milestones);
        ArgumentException.ThrowIfNullOrWhiteSpace(milestoneTitle);

        return milestones
            .FirstOrDefault(milestone =>
                milestone.Title.Equals(milestoneTitle, StringComparison.OrdinalIgnoreCase))
            ?.Number;
    }
}
