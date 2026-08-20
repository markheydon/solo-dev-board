namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default implementation of <see cref="IDailyFocusStalledReviewService"/>.</summary>
public sealed class DailyFocusStalledReviewService : IDailyFocusStalledReviewService
{
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;
    private readonly IPmWorkItemCatalogueService _workItemCatalogueService;

    /// <summary>Initialises a new instance of the <see cref="DailyFocusStalledReviewService"/> class.</summary>
    /// <param name="projectItemCatalogueService">The project board item catalogue service.</param>
    /// <param name="workItemCatalogueService">The cross-repository work-item catalogue service.</param>
    public DailyFocusStalledReviewService(
        IProjectItemCatalogueService projectItemCatalogueService,
        IPmWorkItemCatalogueService workItemCatalogueService)
    {
        _projectItemCatalogueService = projectItemCatalogueService
            ?? throw new ArgumentNullException(nameof(projectItemCatalogueService));
        _workItemCatalogueService = workItemCatalogueService
            ?? throw new ArgumentNullException(nameof(workItemCatalogueService));
    }

    /// <inheritdoc/>
    public async Task<DailyFocusStalledReviewSnapshotDto> GetStalledReviewPullRequestsAsync(
        string projectId,
        int stallDays,
        IReadOnlyList<string> excludedRepositories,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(excludedRepositories);

        var boardCatalogue = await _projectItemCatalogueService
            .GetCatalogueAsync(projectId, cancellationToken)
            .ConfigureAwait(false);

        var utcNow = DateTimeOffset.UtcNow;

        if (DailyFocusStalledReviewDetector.BoardHasInReviewStatus(
                boardCatalogue.StatusOptions,
                boardCatalogue.Items))
        {
            var columnRows = DailyFocusStalledReviewDetector.DetectFromBoardColumn(
                boardCatalogue.Items,
                utcNow,
                stallDays,
                excludedRepositories);
            return new DailyFocusStalledReviewSnapshotDto(columnRows, UsedInReviewColumn: true);
        }

        var workCatalogue = await _workItemCatalogueService
            .GetCatalogueAsync(cancellationToken)
            .ConfigureAwait(false);

        if (workCatalogue.Failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Unable to load pull requests awaiting review because one or more repositories failed to load.");
        }

        var catalogueRows = DailyFocusStalledReviewDetector.DetectFromPendingReviewCatalogue(
            workCatalogue.Items,
            utcNow,
            stallDays,
            excludedRepositories);
        return new DailyFocusStalledReviewSnapshotDto(catalogueRows, UsedInReviewColumn: false);
    }
}
