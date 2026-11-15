namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Default implementation of <see cref="IDailyFocusRecommendationService"/>.</summary>
public sealed class DailyFocusRecommendationService : IDailyFocusRecommendationService
{
    private readonly IPlanningWorkItemCatalogueService _workItemCatalogueService;
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;

    /// <summary>Initialises a new instance of the <see cref="DailyFocusRecommendationService"/> class.</summary>
    /// <param name="workItemCatalogueService">The cross-repository work-item catalogue.</param>
    /// <param name="projectItemCatalogueService">The project board item catalogue.</param>
    public DailyFocusRecommendationService(
        IPlanningWorkItemCatalogueService workItemCatalogueService,
        IProjectItemCatalogueService projectItemCatalogueService)
    {
        ArgumentNullException.ThrowIfNull(workItemCatalogueService);
        ArgumentNullException.ThrowIfNull(projectItemCatalogueService);

        _workItemCatalogueService = workItemCatalogueService;
        _projectItemCatalogueService = projectItemCatalogueService;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Repository exclusions are applied by <see cref="IPlanningWorkItemCatalogueService"/>.
    /// Board Status filtering uses the selected planning board catalogue.
    /// Partial catalogue failures still rank the remaining items; a total failure (no items and at least
    /// one repository error) throws so the App can show Retry instead of a false empty list.
    /// </remarks>
    public async Task<DailyFocusRecommendationResultDto> GetRecommendationsAsync(
        string projectId,
        bool limitToPlanningBoard = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var workItemsTask = _workItemCatalogueService.GetCatalogueAsync(cancellationToken);
        var boardCatalogueTask = _projectItemCatalogueService.GetCatalogueAsync(projectId, cancellationToken);
        await Task.WhenAll(workItemsTask, boardCatalogueTask).ConfigureAwait(false);

        var workItems = await workItemsTask.ConfigureAwait(false);
        var boardCatalogue = await boardCatalogueTask.ConfigureAwait(false);

        if (workItems.Items.Count == 0 && workItems.Failures.Count > 0)
        {
            throw CreateCatalogueFailureException(workItems.Failures);
        }

        var recommendations = DailyFocusRecommendationMapper.SelectTopThree(
            workItems.Items,
            boardCatalogue.Items,
            limitToPlanningBoard);
        return new DailyFocusRecommendationResultDto(recommendations, workItems.Failures);
    }

    private static InvalidOperationException CreateCatalogueFailureException(
        IReadOnlyList<PlanningRepositoryCatalogueFailureDto> failures)
    {
        var repositories = string.Join(", ", failures.Select(static failure => failure.RepositoryFullName));
        var noun = failures.Count == 1 ? "repository" : "repositories";
        return new InvalidOperationException(
            $"Unable to load recommended work because {failures.Count} {noun} failed to load: {repositories}.");
    }
}
