using SoloDevBoard.Application.Services.GitHub;

namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Default implementation of <see cref="IIterationPlanningService"/>.</summary>
public sealed class IterationPlanningService : IIterationPlanningService
{
    private readonly IPmWorkItemCatalogueService _workItemCatalogueService;
    private readonly IProjectItemCatalogueService _projectItemCatalogueService;
    private readonly IGitHubService _gitHubService;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initialises a new instance of the <see cref="IterationPlanningService"/> class.</summary>
    /// <param name="workItemCatalogueService">The cross-repository work-item catalogue.</param>
    /// <param name="projectItemCatalogueService">The project board item catalogue.</param>
    /// <param name="gitHubService">The GitHub service used to add items and update board fields.</param>
    /// <param name="timeProvider">The time provider used to compute stall age.</param>
    public IterationPlanningService(
        IPmWorkItemCatalogueService workItemCatalogueService,
        IProjectItemCatalogueService projectItemCatalogueService,
        IGitHubService gitHubService,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(workItemCatalogueService);
        ArgumentNullException.ThrowIfNull(projectItemCatalogueService);
        ArgumentNullException.ThrowIfNull(gitHubService);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _workItemCatalogueService = workItemCatalogueService;
        _projectItemCatalogueService = projectItemCatalogueService;
        _gitHubService = gitHubService;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<IterationPlanningViewDto> GetPlanningViewAsync(
        string projectId,
        int capacity,
        int stallDays,
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

        var hasFocusOrderField = !string.IsNullOrWhiteSpace(boardCatalogue.FieldIds.FocusOrderFieldId);

        return IterationPlanningViewMapper.Map(
            workItems.Items,
            boardCatalogue.Items,
            workItems.Failures,
            hasFocusOrderField,
            capacity,
            stallDays,
            _timeProvider.GetUtcNow());
    }

    /// <inheritdoc/>
    public async Task<IterationPlanningAddToUpNextResultDto> AddToUpNextAsync(
        string projectId,
        PmWorkItemTypeDto itemType,
        string repositoryFullName,
        int number,
        IReadOnlyList<string> labels,
        int stallDays,
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

        EnsureNoStalledUpNextItemsRemain(catalogue.Items, stallDays, _timeProvider.GetUtcNow());

        var upNextOption = PlanningBoardStatusResolver.ResolveStatusOption(
            catalogue.StatusOptions,
            DailyFocusBoardStateMapper.UpNextStatusName);
        ValidateStatusField(catalogue.FieldIds.StatusFieldId);

        var joinKey = PmWorkItemJoinKey.For(itemType == PmWorkItemTypeDto.PullRequest, repositoryFullName, number);
        var existingItem = catalogue.Items.FirstOrDefault(item =>
            PmWorkItemJoinKey.For(item).Equals(joinKey, StringComparison.OrdinalIgnoreCase));

        var addedBoardCard = existingItem is null;
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

        var assignsFocusOrder = PlanningFocusOrderSequencer.ShouldAssignFocusOrder(labels);
        var hasFocusOrderField = !string.IsNullOrWhiteSpace(catalogue.FieldIds.FocusOrderFieldId);
        double? focusOrderAssigned = null;
        var focusOrderSkipped = !assignsFocusOrder || !hasFocusOrderField;

        if (assignsFocusOrder && hasFocusOrderField)
        {
            var focusOrderFieldId = catalogue.FieldIds.FocusOrderFieldId;
            ArgumentException.ThrowIfNullOrWhiteSpace(focusOrderFieldId);

            var refreshedCatalogue = await _projectItemCatalogueService
                .GetCatalogueAsync(projectId, cancellationToken)
                .ConfigureAwait(false);

            var upNextItems = refreshedCatalogue.Items
                .Where(item => DailyFocusBoardStateMapper.IsUpNextStatus(item.Status?.Name))
                .Where(item => !item.ProjectItemId.Equals(projectItemId, StringComparison.Ordinal))
                .ToArray();
            focusOrderAssigned = PlanningFocusOrderSequencer.GetNextFocusOrder(upNextItems);
            focusOrderSkipped = false;

            await _projectItemCatalogueService
                .UpdateFocusOrderAsync(
                    projectId,
                    projectItemId,
                    focusOrderFieldId,
                    focusOrderAssigned.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return new IterationPlanningAddToUpNextResultDto(
            addedBoardCard,
            focusOrderAssigned,
            focusOrderSkipped);
    }

    /// <inheritdoc/>
    public async Task ReCommitStalledUpNextItemAsync(
        string projectId,
        string projectItemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectItemId);

        var catalogue = await _projectItemCatalogueService
            .GetCatalogueAsync(projectId, cancellationToken)
            .ConfigureAwait(false);

        ValidateStatusField(catalogue.FieldIds.StatusFieldId);
        var todoOption = PlanningBoardStatusResolver.ResolveStatusOption(
            catalogue.StatusOptions,
            PlanningBoardStatusResolver.TodoStatusName);
        var upNextOption = PlanningBoardStatusResolver.ResolveStatusOption(
            catalogue.StatusOptions,
            DailyFocusBoardStateMapper.UpNextStatusName);

        await _gitHubService
            .UpdateProjectBoardItemStatusAsync(
                projectId,
                projectItemId,
                catalogue.FieldIds.StatusFieldId,
                todoOption.OptionId,
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await _gitHubService
                .UpdateProjectBoardItemStatusAsync(
                    projectId,
                    projectItemId,
                    catalogue.FieldIds.StatusFieldId,
                    upNextOption.OptionId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            try
            {
                await _gitHubService
                    .UpdateProjectBoardItemStatusAsync(
                        projectId,
                        projectItemId,
                        catalogue.FieldIds.StatusFieldId,
                        upNextOption.OptionId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception rollbackEx) when (rollbackEx is not OperationCanceledException)
            {
                throw new InvalidOperationException(
                    "Re-commit moved the item to Todo but failed to return it to Up Next. Refresh Iteration Planning and use Re-commit again, or set the item status to Up Next manually on the board.",
                    ex);
            }

            throw new InvalidOperationException(
                "Re-commit moved the item to Todo but failed to return it to Up Next. The item has been restored to Up Next; refresh Iteration Planning and try Re-commit again.",
                ex);
        }

        _projectItemCatalogueService.InvalidateCatalogue(projectId);
    }

    /// <inheritdoc/>
    public async Task MarkStalledUpNextItemBlockedAsync(
        string projectId,
        IterationPlanningStalledItemDto item,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(item);

        var catalogue = await LoadCatalogueForStalledItemUpdateAsync(projectId, item, cancellationToken)
            .ConfigureAwait(false);
        var blockedOption = PlanningBoardStatusResolver.ResolveStatusOption(
            catalogue.StatusOptions,
            DailyFocusRecommendationMapper.BlockedStatusName);

        await UpdateBoardStatusAsync(
            projectId,
            item.ProjectItemId,
            catalogue.FieldIds.StatusFieldId,
            blockedOption.OptionId,
            cancellationToken).ConfigureAwait(false);

        await ApplyWorkItemLabelsAsync(
            item,
            MergeStatusLabel(item.Labels, PmLabelHelpers.BlockedStatusLabel, PmLabelHelpers.IceBoxStatusLabel),
            cancellationToken).ConfigureAwait(false);

        _projectItemCatalogueService.InvalidateCatalogue(projectId);
    }

    /// <inheritdoc/>
    public async Task MoveStalledUpNextItemToIceBoxAsync(
        string projectId,
        IterationPlanningStalledItemDto item,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(item);

        var catalogue = await LoadCatalogueForStalledItemUpdateAsync(projectId, item, cancellationToken)
            .ConfigureAwait(false);
        var iceBoxOption = PlanningBoardStatusResolver.ResolveStatusOption(
            catalogue.StatusOptions,
            DailyFocusRecommendationMapper.IceBoxStatusName);

        await UpdateBoardStatusAsync(
            projectId,
            item.ProjectItemId,
            catalogue.FieldIds.StatusFieldId,
            iceBoxOption.OptionId,
            cancellationToken).ConfigureAwait(false);

        await ClearFocusOrderWhenPresentAsync(projectId, item.ProjectItemId, catalogue, cancellationToken)
            .ConfigureAwait(false);

        await ApplyWorkItemLabelsAsync(
            item,
            MergeStatusLabel(item.Labels, PmLabelHelpers.IceBoxStatusLabel, PmLabelHelpers.BlockedStatusLabel),
            cancellationToken).ConfigureAwait(false);

        _projectItemCatalogueService.InvalidateCatalogue(projectId);
    }

    /// <inheritdoc/>
    public async Task RemoveStalledUpNextItemAsync(
        string projectId,
        IterationPlanningStalledItemDto item,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(item);

        var catalogue = await LoadCatalogueForStalledItemUpdateAsync(projectId, item, cancellationToken)
            .ConfigureAwait(false);
        var todoOption = PlanningBoardStatusResolver.ResolveStatusOption(
            catalogue.StatusOptions,
            PlanningBoardStatusResolver.TodoStatusName);

        await UpdateBoardStatusAsync(
            projectId,
            item.ProjectItemId,
            catalogue.FieldIds.StatusFieldId,
            todoOption.OptionId,
            cancellationToken).ConfigureAwait(false);

        await ClearFocusOrderWhenPresentAsync(projectId, item.ProjectItemId, catalogue, cancellationToken)
            .ConfigureAwait(false);

        _projectItemCatalogueService.InvalidateCatalogue(projectId);
    }

    private async Task<ProjectBoardItemCatalogueDto> LoadCatalogueForStalledItemUpdateAsync(
        string projectId,
        IterationPlanningStalledItemDto item,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item.ProjectItemId);

        var catalogue = await _projectItemCatalogueService
            .GetCatalogueAsync(projectId, cancellationToken)
            .ConfigureAwait(false);

        ValidateStatusField(catalogue.FieldIds.StatusFieldId);
        return catalogue;
    }

    private Task UpdateBoardStatusAsync(
        string projectId,
        string projectItemId,
        string statusFieldId,
        string statusOptionId,
        CancellationToken cancellationToken) =>
        _gitHubService.UpdateProjectBoardItemStatusAsync(
            projectId,
            projectItemId,
            statusFieldId,
            statusOptionId,
            cancellationToken);

    private async Task ClearFocusOrderWhenPresentAsync(
        string projectId,
        string projectItemId,
        ProjectBoardItemCatalogueDto catalogue,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(catalogue.FieldIds.FocusOrderFieldId))
        {
            return;
        }

        await _projectItemCatalogueService
            .ClearFocusOrderAsync(
                projectId,
                projectItemId,
                catalogue.FieldIds.FocusOrderFieldId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ApplyWorkItemLabelsAsync(
        IterationPlanningStalledItemDto item,
        IReadOnlyList<string> labelNames,
        CancellationToken cancellationToken)
    {
        if (!TryParseRepositoryFullName(item.RepositoryFullName, out var owner, out var repo))
        {
            throw new ArgumentException(
                $"Repository scope '{item.RepositoryFullName}' must be in owner/repository format.",
                nameof(item));
        }

        await _gitHubService
            .ApplyLabelsToTriageItemAsync(owner, repo, item.Number, labelNames, cancellationToken)
            .ConfigureAwait(false);
    }

    private static IReadOnlyList<string> MergeStatusLabel(
        IReadOnlyList<string> currentLabels,
        string labelToAdd,
        string labelToRemove)
    {
        ArgumentNullException.ThrowIfNull(currentLabels);

        return currentLabels
            .Where(label => !label.Equals(labelToRemove, StringComparison.OrdinalIgnoreCase))
            .Concat([labelToAdd])
            .Where(static label => !string.IsNullOrWhiteSpace(label))
            .Select(static label => label.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void EnsureNoStalledUpNextItemsRemain(
        IReadOnlyList<ProjectBoardItemDto> boardItems,
        int stallDays,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(boardItems);

        var resolvedStallDays = stallDays > 0 ? stallDays : PmSettingsDefaults.StallDays;

        var hasStalledUpNextItems = boardItems.Any(item =>
            DailyFocusBoardStateMapper.IsUpNextStatus(item.Status?.Name)
            && DailyFocusBoardStateMapper.HasStallClock(item.ActivityTimestamp)
            && DailyFocusBoardStateMapper.GetAgeInDays(item.ActivityTimestamp, utcNow) >= resolvedStallDays);

        if (hasStalledUpNextItems)
        {
            throw new InvalidOperationException(
                "Resolve stalled Up Next items before adding new work.");
        }
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
