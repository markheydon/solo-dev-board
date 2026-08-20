namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default implementation of <see cref="IBacklogReviewService"/>.</summary>
public sealed class BacklogReviewService : IBacklogReviewService
{
    private readonly IPmWorkItemCatalogueService _workItemCatalogueService;
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;

    /// <summary>Initialises a new instance of the <see cref="BacklogReviewService"/> class.</summary>
    /// <param name="workItemCatalogueService">The cross-repository work-item catalogue.</param>
    /// <param name="projectItemCatalogueService">The project board item catalogue.</param>
    public BacklogReviewService(
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

        var workItemsTask = _workItemCatalogueService.GetCatalogueAsync(cancellationToken);
        var boardCatalogueTask = _projectItemCatalogueService.GetCatalogueAsync(projectId, cancellationToken);
        await Task.WhenAll(workItemsTask, boardCatalogueTask).ConfigureAwait(false);

        var workItems = await workItemsTask.ConfigureAwait(false);
        var boardCatalogue = await boardCatalogueTask.ConfigureAwait(false);

        if (workItems.Items.Count == 0 && workItems.Failures.Count > 0)
        {
            throw CreateCatalogueFailureException(workItems.Failures);
        }

        return BacklogReviewGrouping.Group(workItems.Items, boardCatalogue.Items, workItems.Failures);
    }

    private static InvalidOperationException CreateCatalogueFailureException(
        IReadOnlyList<PmRepositoryCatalogueFailureDto> failures)
    {
        var repositories = string.Join(", ", failures.Select(static failure => failure.RepositoryFullName));
        var noun = failures.Count == 1 ? "repository" : "repositories";
        return new InvalidOperationException(
            $"Unable to load the backlog because {failures.Count} {noun} failed to load: {repositories}.");
    }
}
