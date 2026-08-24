namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Default implementation of <see cref="IBacklogReviewService"/>.</summary>
public sealed class BacklogReviewService : IBacklogReviewService
{
    private readonly IPlanningWorkItemCatalogueService _workItemCatalogueService;
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;
    private readonly IPlanningSettingsService _pmSettingsService;

    /// <summary>Initialises a new instance of the <see cref="BacklogReviewService"/> class.</summary>
    /// <param name="workItemCatalogueService">The cross-repository work-item catalogue.</param>
    /// <param name="projectItemCatalogueService">The project board item catalogue.</param>
    /// <param name="pmSettingsService">The PM settings service.</param>
    public BacklogReviewService(
        IPlanningWorkItemCatalogueService workItemCatalogueService,
        IProjectItemCatalogueService projectItemCatalogueService,
        IPlanningSettingsService pmSettingsService)
    {
        ArgumentNullException.ThrowIfNull(workItemCatalogueService);
        ArgumentNullException.ThrowIfNull(projectItemCatalogueService);
        ArgumentNullException.ThrowIfNull(pmSettingsService);

        _workItemCatalogueService = workItemCatalogueService;
        _projectItemCatalogueService = projectItemCatalogueService;
        _pmSettingsService = pmSettingsService;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Repository exclusions are applied by <see cref="IPlanningWorkItemCatalogueService"/>.
    /// Board Status joins use the selected planning board catalogue.
    /// Partial catalogue failures still group the remaining items; a total failure (no items and at least
    /// one repository error) throws so the App can show Retry instead of a false empty list.
    /// </remarks>
    public async Task<BacklogReviewResultDto> GetBacklogAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var settings = await _pmSettingsService.GetSettingsAsync().ConfigureAwait(false);
        var neglectDays = settings.NeglectDays > 0 ? settings.NeglectDays : PlanningSettingsDefaults.NeglectDays;

        var workItemsTask = _workItemCatalogueService.GetCatalogueAsync(cancellationToken);
        var boardCatalogueTask = _projectItemCatalogueService.GetCatalogueAsync(projectId, cancellationToken);
        await Task.WhenAll(workItemsTask, boardCatalogueTask).ConfigureAwait(false);

        var workItems = await workItemsTask.ConfigureAwait(false);
        var boardCatalogue = await boardCatalogueTask.ConfigureAwait(false);

        if (workItems.Items.Count == 0 && workItems.Failures.Count > 0)
        {
            throw CreateCatalogueFailureException(workItems.Failures);
        }

        return BacklogReviewGrouping.Group(
            workItems.Items,
            boardCatalogue.Items,
            workItems.RepositorySummaries,
            workItems.Failures,
            neglectDays,
            DateTimeOffset.UtcNow);
    }

    private static InvalidOperationException CreateCatalogueFailureException(
        IReadOnlyList<PlanningRepositoryCatalogueFailureDto> failures)
    {
        var repositories = string.Join(", ", failures.Select(static failure => failure.RepositoryFullName));
        var noun = failures.Count == 1 ? "repository" : "repositories";
        return new InvalidOperationException(
            $"Unable to load the backlog because {failures.Count} {noun} failed to load: {repositories}.");
    }
}
