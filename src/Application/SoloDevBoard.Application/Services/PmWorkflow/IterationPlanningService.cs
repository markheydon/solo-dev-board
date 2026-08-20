using SoloDevBoard.Application.Services.GitHub;

namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default implementation of <see cref="IIterationPlanningService"/>.</summary>
public sealed class IterationPlanningService : IIterationPlanningService
{
    private readonly IPmWorkItemCatalogueService _workItemCatalogueService;
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;
    private readonly IGitHubService _gitHubService;

    /// <summary>Initialises a new instance of the <see cref="IterationPlanningService"/> class.</summary>
    /// <param name="workItemCatalogueService">The cross-repository work-item catalogue.</param>
    /// <param name="projectItemCatalogueService">The project board item catalogue.</param>
    /// <param name="gitHubService">The GitHub service used to add items and update board fields.</param>
    public IterationPlanningService(
        IPmWorkItemCatalogueService workItemCatalogueService,
        IProjectItemCatalogueService projectItemCatalogueService,
        IGitHubService gitHubService)
    {
        ArgumentNullException.ThrowIfNull(workItemCatalogueService);
        ArgumentNullException.ThrowIfNull(projectItemCatalogueService);
        ArgumentNullException.ThrowIfNull(gitHubService);

        _workItemCatalogueService = workItemCatalogueService;
        _projectItemCatalogueService = projectItemCatalogueService;
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public async Task<IterationPlanningViewDto> GetPlanningViewAsync(
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

        return IterationPlanningViewMapper.Map(
            workItems.Items,
            boardCatalogue.Items,
            workItems.Failures);
    }

    /// <inheritdoc/>
    public async Task AddToUpNextAsync(
        string projectId,
        PmWorkItemTypeDto itemType,
        string repositoryFullName,
        int number,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryFullName);
        ArgumentNullException.ThrowIfNull(labels);

        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "The item number must be positive.");
        }

        if (!TryParseRepositoryFullName(repositoryFullName, out var owner, out var repo))
        {
            throw new ArgumentException(
                $"Repository scope '{repositoryFullName}' must be in owner/repository format.",
                nameof(repositoryFullName));
        }

        var catalogue = await _projectItemCatalogueService
            .GetCatalogueAsync(projectId, cancellationToken)
            .ConfigureAwait(false);

        var upNextOption = ResolveUpNextStatusOption(catalogue.StatusOptions);
        ValidateStatusField(catalogue.FieldIds.StatusFieldId);

        var joinKey = PmWorkItemJoinKey.For(itemType == PmWorkItemTypeDto.PullRequest, repositoryFullName, number);
        var existingItem = catalogue.Items.FirstOrDefault(item =>
            PmWorkItemJoinKey.For(item).Equals(joinKey, StringComparison.OrdinalIgnoreCase));

        var projectItemId = existingItem?.ProjectItemId
            ?? await _gitHubService
                .AddTriageItemToProjectBoardAsync(owner, repo, number, projectId, cancellationToken)
                .ConfigureAwait(false);

        await _gitHubService
            .UpdateProjectBoardItemStatusAsync(
                projectId,
                projectItemId,
                catalogue.FieldIds.StatusFieldId,
                upNextOption.OptionId,
                cancellationToken)
            .ConfigureAwait(false);

        _projectItemCatalogueService.InvalidateCatalogue(projectId);

        if (PlanningFocusOrderSequencer.ShouldAssignFocusOrder(labels))
        {
            if (string.IsNullOrWhiteSpace(catalogue.FieldIds.FocusOrderFieldId))
            {
                throw new InvalidOperationException("The project board does not expose a Focus Order field.");
            }

            var upNextItems = catalogue.Items
                .Where(item => DailyFocusBoardStateMapper.IsUpNextStatus(item.Status?.Name))
                .ToArray();
            var nextFocusOrder = PlanningFocusOrderSequencer.GetNextFocusOrder(upNextItems);

            await _projectItemCatalogueService
                .UpdateFocusOrderAsync(
                    projectId,
                    projectItemId,
                    catalogue.FieldIds.FocusOrderFieldId,
                    nextFocusOrder,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static ProjectBoardStatusOptionDto ResolveUpNextStatusOption(
        IReadOnlyList<ProjectBoardStatusOptionDto> statusOptions)
    {
        ArgumentNullException.ThrowIfNull(statusOptions);

        var upNextOption = statusOptions.FirstOrDefault(option =>
            option.Name.Equals(DailyFocusBoardStateMapper.UpNextStatusName, StringComparison.OrdinalIgnoreCase));

        if (upNextOption is null)
        {
            throw new InvalidOperationException(
                "The planning board does not expose an Up Next Status option.");
        }

        return upNextOption;
    }

    private static void ValidateStatusField(string statusFieldId)
    {
        if (string.IsNullOrWhiteSpace(statusFieldId))
        {
            throw new InvalidOperationException("The planning board does not expose a Status field.");
        }
    }

    private static bool TryParseRepositoryFullName(string repositoryFullName, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        var slashIndex = repositoryFullName.IndexOf('/');
        if (slashIndex <= 0 || slashIndex >= repositoryFullName.Length - 1)
        {
            return false;
        }

        owner = repositoryFullName[..slashIndex];
        repo = repositoryFullName[(slashIndex + 1)..];
        return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
    }

    private static InvalidOperationException CreateCatalogueFailureException(
        IReadOnlyList<PmRepositoryCatalogueFailureDto> failures)
    {
        var repositories = string.Join(", ", failures.Select(static failure => failure.RepositoryFullName));
        var noun = failures.Count == 1 ? "repository" : "repositories";
        return new InvalidOperationException(
            $"Unable to load Iteration Planning because {failures.Count} {noun} failed to load: {repositories}.");
    }
}
