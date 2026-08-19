namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default implementation of <see cref="IDailyFocusBoardStateService"/>.</summary>
public sealed class DailyFocusBoardStateService : IDailyFocusBoardStateService
{
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;

    /// <summary>Initialises a new instance of the <see cref="DailyFocusBoardStateService"/> class.</summary>
    /// <param name="projectItemCatalogueService">The project board item catalogue service.</param>
    public DailyFocusBoardStateService(IProjectItemCatalogueService projectItemCatalogueService)
    {
        _projectItemCatalogueService = projectItemCatalogueService
            ?? throw new ArgumentNullException(nameof(projectItemCatalogueService));
    }

    /// <inheritdoc/>
    public async Task<DailyFocusBoardStateDto> GetBoardStateAsync(
        string projectId,
        int capacity,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var catalogue = await _projectItemCatalogueService
            .GetCatalogueAsync(projectId, cancellationToken)
            .ConfigureAwait(false);

        return DailyFocusBoardStateMapper.Map(catalogue.StatusOptions, catalogue.Items, capacity);
    }
}
