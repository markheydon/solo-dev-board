namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default implementation of <see cref="IDailyFocusBoardStateService"/>.</summary>
public sealed class DailyFocusBoardStateService : IDailyFocusBoardStateService
{
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initialises a new instance of the <see cref="DailyFocusBoardStateService"/> class.</summary>
    /// <param name="projectItemCatalogueService">The project board item catalogue service.</param>
    /// <param name="timeProvider">The time provider used to compute stall age.</param>
    public DailyFocusBoardStateService(
        IProjectItemCatalogueService projectItemCatalogueService,
        TimeProvider timeProvider)
    {
        _projectItemCatalogueService = projectItemCatalogueService
            ?? throw new ArgumentNullException(nameof(projectItemCatalogueService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public async Task<DailyFocusBoardStateDto> GetBoardStateAsync(
        string projectId,
        int capacity,
        int stallDays,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var catalogue = await _projectItemCatalogueService
            .GetCatalogueAsync(projectId, cancellationToken)
            .ConfigureAwait(false);

        return DailyFocusBoardStateMapper.Map(
            catalogue.StatusOptions,
            catalogue.Items,
            capacity,
            stallDays,
            _timeProvider.GetUtcNow());
    }
}
