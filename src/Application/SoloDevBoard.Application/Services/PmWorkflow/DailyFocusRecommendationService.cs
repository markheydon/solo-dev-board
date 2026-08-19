namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default implementation of <see cref="IDailyFocusRecommendationService"/>.</summary>
public sealed class DailyFocusRecommendationService : IDailyFocusRecommendationService
{
    private readonly IPmWorkItemCatalogueService _workItemCatalogueService;
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;

    /// <summary>Initialises a new instance of the <see cref="DailyFocusRecommendationService"/> class.</summary>
    /// <param name="workItemCatalogueService">The cross-repository work-item catalogue.</param>
    /// <param name="projectItemCatalogueService">The project board item catalogue.</param>
    public DailyFocusRecommendationService(
        IPmWorkItemCatalogueService workItemCatalogueService,
        IProjectItemCatalogueService projectItemCatalogueService)
    {
        ArgumentNullException.ThrowIfNull(workItemCatalogueService);
        ArgumentNullException.ThrowIfNull(projectItemCatalogueService);

        _workItemCatalogueService = workItemCatalogueService;
        _projectItemCatalogueService = projectItemCatalogueService;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Repository exclusions are applied by <see cref="IPmWorkItemCatalogueService"/>.
    /// Board Status filtering uses the selected planning board catalogue.
    /// </remarks>
    public async Task<IReadOnlyList<DailyFocusRecommendationDto>> GetRecommendationsAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var workItemsTask = _workItemCatalogueService.GetCatalogueAsync(cancellationToken);
        var boardCatalogueTask = _projectItemCatalogueService.GetCatalogueAsync(projectId, cancellationToken);
        await Task.WhenAll(workItemsTask, boardCatalogueTask).ConfigureAwait(false);

        return DailyFocusRecommendationMapper.SelectTopThree(
            workItemsTask.Result.Items,
            boardCatalogueTask.Result.Items);
    }
}
