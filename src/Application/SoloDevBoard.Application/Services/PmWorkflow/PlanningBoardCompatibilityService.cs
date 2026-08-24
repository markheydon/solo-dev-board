namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default implementation of <see cref="IPlanningBoardCompatibilityService"/>.</summary>
public sealed class PlanningBoardCompatibilityService : IPlanningBoardCompatibilityService
{
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;

    /// <summary>Initialises a new instance of the <see cref="PlanningBoardCompatibilityService"/> class.</summary>
    /// <param name="projectItemCatalogueService">The project board catalogue service.</param>
    public PlanningBoardCompatibilityService(IProjectItemCatalogueService projectItemCatalogueService)
    {
        ArgumentNullException.ThrowIfNull(projectItemCatalogueService);
        _projectItemCatalogueService = projectItemCatalogueService;
    }

    /// <inheritdoc/>
    public async Task<PlanningBoardCompatibilityReportDto> GetReportAsync(
        string projectId,
        bool forceReload = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        if (forceReload)
        {
            _projectItemCatalogueService.InvalidateCatalogue(projectId);
        }

        var catalogue = await _projectItemCatalogueService
            .GetCatalogueAsync(projectId, cancellationToken)
            .ConfigureAwait(false);

        return PlanningBoardCompatibilityEvaluator.Evaluate(
            projectId,
            catalogue.FieldIds,
            catalogue.StatusOptions);
    }
}
