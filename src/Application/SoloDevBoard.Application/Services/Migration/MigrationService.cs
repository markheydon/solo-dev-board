using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Migration;
using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Default implementation of <see cref="IMigrationService"/>.</summary>
public sealed class MigrationService : IMigrationService
{
    private readonly ILabelRepository _labelRepository;
    private readonly IMilestoneRepository _milestoneRepository;
    private readonly IProjectBoardStructureRepository _projectBoardStructureRepository;

    /// <summary>Initialises a new instance of the <see cref="MigrationService"/> class.</summary>
    /// <param name="labelRepository">The label repository used for migration operations.</param>
    /// <param name="milestoneRepository">The milestone repository used for migration operations.</param>
    /// <param name="projectBoardStructureRepository">The project board structure repository used for Status column migration.</param>
    public MigrationService(
        ILabelRepository labelRepository,
        IMilestoneRepository milestoneRepository,
        IProjectBoardStructureRepository projectBoardStructureRepository)
    {
        ArgumentNullException.ThrowIfNull(labelRepository);
        ArgumentNullException.ThrowIfNull(milestoneRepository);
        ArgumentNullException.ThrowIfNull(projectBoardStructureRepository);

        _labelRepository = labelRepository;
        _milestoneRepository = milestoneRepository;
        _projectBoardStructureRepository = projectBoardStructureRepository;
    }

    /// <inheritdoc/>
    public async Task<MigrationPreviewDto> PreviewMigrationAsync(
        string sourceRepositoryFullName,
        IReadOnlyList<string> targetRepositoryFullNames,
        MigrationScopeDto scope,
        MigrationConflictStrategy conflictStrategy,
        MigrationBoardSelectionDto? boardSelection = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRepositoryFullName);
        ArgumentNullException.ThrowIfNull(scope);
        EnsureAtLeastOneScopeSelected(scope);
        EnsureBoardSelectionWhenRequired(scope, boardSelection);

        var source = SplitRepositoryFullName(sourceRepositoryFullName);
        var normalisedTargets = NormaliseTargetRepositories(targetRepositoryFullNames, sourceRepositoryFullName);

        var sourceLabels = scope.IncludeLabels
            ? await _labelRepository.GetLabelsAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false)
            : [];
        var sourceMilestones = scope.IncludeMilestones
            ? await _milestoneRepository.GetMilestonesAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false)
            : [];

        ProjectBoardStatusStructure? sourceStatusStructure = null;
        if (scope.IncludeProjectBoardColumns)
        {
            sourceStatusStructure = await _projectBoardStructureRepository
                .GetStatusStructureAsync(boardSelection!.SourceProjectId, cancellationToken)
                .ConfigureAwait(false);
        }

        var labelPreviews = new List<LabelSyncRepositoryPreviewDto>();
        var milestonePreviews = new List<MilestoneSyncRepositoryPreviewDto>();
        var statusPreviews = new List<ProjectBoardStatusSyncRepositoryPreviewDto>();

        foreach (var targetRepositoryFullName in normalisedTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = SplitRepositoryFullName(targetRepositoryFullName);

            if (scope.IncludeLabels)
            {
                var targetLabels = await _labelRepository.GetLabelsAsync(target.Owner, target.Name, cancellationToken).ConfigureAwait(false);
                labelPreviews.Add(BuildLabelPreview(targetRepositoryFullName, sourceLabels, targetLabels, conflictStrategy));
            }

            if (scope.IncludeMilestones)
            {
                var targetMilestones = await _milestoneRepository.GetMilestonesAsync(target.Owner, target.Name, cancellationToken).ConfigureAwait(false);
                milestonePreviews.Add(BuildMilestonePreview(targetRepositoryFullName, sourceMilestones, targetMilestones, conflictStrategy));
            }

            if (scope.IncludeProjectBoardColumns)
            {
                var targetSelection = ResolveTargetBoardSelection(boardSelection!, targetRepositoryFullName);
                var discovery = await _projectBoardStructureRepository
                    .DiscoverBoardsAsync(target.Owner, target.Name, cancellationToken)
                    .ConfigureAwait(false);

                IReadOnlySet<string> optionIdsInUse = EmptyOptionIdSet;
                ProjectBoardStatusStructure? targetStatusStructure = null;
                if (!string.IsNullOrWhiteSpace(targetSelection.TargetProjectId))
                {
                    targetStatusStructure = await _projectBoardStructureRepository
                        .GetStatusStructureAsync(targetSelection.TargetProjectId, cancellationToken)
                        .ConfigureAwait(false);
                    optionIdsInUse = await _projectBoardStructureRepository
                        .GetStatusOptionIdsInUseAsync(targetSelection.TargetProjectId, cancellationToken)
                        .ConfigureAwait(false);
                }

                statusPreviews.Add(BuildStatusPreview(
                    targetRepositoryFullName,
                    sourceStatusStructure!,
                    targetStatusStructure,
                    targetSelection,
                    discovery.TotalLinkedProjectCount,
                    discovery.InaccessibleLinkedProjectCount,
                    optionIdsInUse,
                    conflictStrategy));
            }
        }

        return new MigrationPreviewDto(
            conflictStrategy,
            labelPreviews.OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray(),
            milestonePreviews.OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray(),
            statusPreviews.OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    /// <inheritdoc/>
    public async Task<MigrationResultDto> ApplyMigrationAsync(
        string sourceRepositoryFullName,
        IReadOnlyList<string> targetRepositoryFullNames,
        MigrationScopeDto scope,
        MigrationConflictStrategy conflictStrategy,
        MigrationBoardSelectionDto? boardSelection = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRepositoryFullName);
        ArgumentNullException.ThrowIfNull(scope);
        EnsureAtLeastOneScopeSelected(scope);
        EnsureBoardSelectionWhenRequired(scope, boardSelection);

        var source = SplitRepositoryFullName(sourceRepositoryFullName);
        var normalisedTargets = NormaliseTargetRepositories(targetRepositoryFullNames, sourceRepositoryFullName);

        var sourceLabels = scope.IncludeLabels
            ? await _labelRepository.GetLabelsAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false)
            : [];
        var sourceMilestones = scope.IncludeMilestones
            ? await _milestoneRepository.GetMilestonesAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false)
            : [];

        ProjectBoardStatusStructure? sourceStatusStructure = null;
        if (scope.IncludeProjectBoardColumns)
        {
            sourceStatusStructure = await _projectBoardStructureRepository
                .GetStatusStructureAsync(boardSelection!.SourceProjectId, cancellationToken)
                .ConfigureAwait(false);
        }

        var labelResults = new List<LabelSyncRepositoryResultDto>();
        var milestoneResults = new List<MilestoneSyncRepositoryResultDto>();
        var statusResults = new List<ProjectBoardStatusSyncRepositoryResultDto>();

        foreach (var targetRepositoryFullName in normalisedTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = SplitRepositoryFullName(targetRepositoryFullName);

            if (scope.IncludeLabels)
            {
                labelResults.Add(await ApplyLabelMigrationAsync(targetRepositoryFullName, target.Owner, target.Name, sourceLabels, conflictStrategy, cancellationToken).ConfigureAwait(false));
            }

            if (scope.IncludeMilestones)
            {
                milestoneResults.Add(await ApplyMilestoneMigrationAsync(targetRepositoryFullName, target.Owner, target.Name, sourceMilestones, conflictStrategy, cancellationToken).ConfigureAwait(false));
            }

            if (scope.IncludeProjectBoardColumns)
            {
                var targetSelection = ResolveTargetBoardSelection(boardSelection!, targetRepositoryFullName);
                statusResults.Add(await ApplyStatusMigrationAsync(
                    targetRepositoryFullName,
                    target.Owner,
                    target.Name,
                    sourceStatusStructure!,
                    targetSelection,
                    conflictStrategy,
                    cancellationToken).ConfigureAwait(false));
            }
        }

        return new MigrationResultDto(
            conflictStrategy,
            labelResults.OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray(),
            milestoneResults.OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray(),
            statusResults.OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private async Task<ProjectBoardStatusSyncRepositoryResultDto> ApplyStatusMigrationAsync(
        string targetRepositoryFullName,
        string targetOwner,
        string targetRepo,
        ProjectBoardStatusStructure sourceStatusStructure,
        MigrationTargetBoardSelectionDto targetSelection,
        MigrationConflictStrategy conflictStrategy,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await BuildStatusPreviewForApplyAsync(
                targetRepositoryFullName,
                targetOwner,
                targetRepo,
                sourceStatusStructure,
                targetSelection,
                conflictStrategy,
                cancellationToken).ConfigureAwait(false);

            if (preview.CreateNewBoard)
            {
                var title = string.IsNullOrWhiteSpace(targetSelection.NewBoardTitle)
                    ? $"{targetRepo} board"
                    : targetSelection.NewBoardTitle.Trim();
                var createdBoard = await _projectBoardStructureRepository
                    .CreateLinkedProjectAsync(targetOwner, targetRepo, title, cancellationToken)
                    .ConfigureAwait(false);

                var finalOptions = BuildFinalStatusOptionsForApply(
                    sourceStatusStructure.Options,
                    createdBoard.Options,
                    MigrationConflictStrategy.Merge,
                    EmptyOptionIdSet);

                await _projectBoardStructureRepository
                    .UpdateStatusOptionsAsync(createdBoard.ProjectId, createdBoard.StatusFieldId, finalOptions, cancellationToken)
                    .ConfigureAwait(false);

                return new ProjectBoardStatusSyncRepositoryResultDto(
                    targetRepositoryFullName,
                    preview.ToCreate.Count,
                    preview.ToUpdate.Count,
                    preview.ToDelete.Count,
                    preview.Skipped.Count,
                    createdBoard.ProjectId,
                    preview.Warnings,
                    null);
            }

            var targetProjectId = targetSelection.TargetProjectId
                ?? throw new InvalidOperationException("A target project board must be selected before applying Status column migration.");

            var targetStatusStructure = await _projectBoardStructureRepository
                .GetStatusStructureAsync(targetProjectId, cancellationToken)
                .ConfigureAwait(false);
            var optionIdsInUse = await _projectBoardStructureRepository
                .GetStatusOptionIdsInUseAsync(targetProjectId, cancellationToken)
                .ConfigureAwait(false);

            var optionsToPersist = BuildFinalStatusOptionsForApply(
                sourceStatusStructure.Options,
                targetStatusStructure.Options,
                conflictStrategy,
                optionIdsInUse);

            await _projectBoardStructureRepository
                .UpdateStatusOptionsAsync(targetProjectId, targetStatusStructure.StatusFieldId, optionsToPersist, cancellationToken)
                .ConfigureAwait(false);

            return new ProjectBoardStatusSyncRepositoryResultDto(
                targetRepositoryFullName,
                preview.ToCreate.Count,
                preview.ToUpdate.Count,
                preview.ToDelete.Count,
                preview.Skipped.Count,
                null,
                preview.Warnings,
                null);
        }
        catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException or InvalidOperationException)
        {
            return new ProjectBoardStatusSyncRepositoryResultDto(
                targetRepositoryFullName,
                0,
                0,
                0,
                0,
                null,
                [],
                ex.Message);
        }
    }

    private async Task<ProjectBoardStatusSyncRepositoryPreviewDto> BuildStatusPreviewForApplyAsync(
        string targetRepositoryFullName,
        string targetOwner,
        string targetRepo,
        ProjectBoardStatusStructure sourceStatusStructure,
        MigrationTargetBoardSelectionDto targetSelection,
        MigrationConflictStrategy conflictStrategy,
        CancellationToken cancellationToken)
    {
        var discovery = await _projectBoardStructureRepository
            .DiscoverBoardsAsync(targetOwner, targetRepo, cancellationToken)
            .ConfigureAwait(false);

        if (targetSelection.TargetProjectId is null)
        {
            return BuildStatusPreview(
                targetRepositoryFullName,
                sourceStatusStructure,
                null,
                targetSelection,
                discovery.TotalLinkedProjectCount,
                discovery.InaccessibleLinkedProjectCount,
                EmptyOptionIdSet,
                conflictStrategy);
        }

        var targetStatusStructure = await _projectBoardStructureRepository
            .GetStatusStructureAsync(targetSelection.TargetProjectId, cancellationToken)
            .ConfigureAwait(false);
        var optionIdsInUse = await _projectBoardStructureRepository
            .GetStatusOptionIdsInUseAsync(targetSelection.TargetProjectId, cancellationToken)
            .ConfigureAwait(false);

        return BuildStatusPreview(
            targetRepositoryFullName,
            sourceStatusStructure,
            targetStatusStructure,
            targetSelection,
            discovery.TotalLinkedProjectCount,
            discovery.InaccessibleLinkedProjectCount,
            optionIdsInUse,
            conflictStrategy);
    }

    private static ProjectBoardStatusSyncRepositoryPreviewDto BuildStatusPreview(
        string targetRepositoryFullName,
        ProjectBoardStatusStructure sourceStatusStructure,
        ProjectBoardStatusStructure? targetStatusStructure,
        MigrationTargetBoardSelectionDto targetSelection,
        int totalLinkedProjectCount,
        int inaccessibleLinkedProjectCount,
        IReadOnlySet<string> optionIdsInUse,
        MigrationConflictStrategy conflictStrategy)
    {
        var createNewBoard = string.IsNullOrWhiteSpace(targetSelection.TargetProjectId);
        if (createNewBoard || targetStatusStructure is null)
        {
            var createOptions = sourceStatusStructure.Options
                .Select(MapToStatusOptionDto)
                .OrderBy(option => option.Order)
                .ToArray();

            return new ProjectBoardStatusSyncRepositoryPreviewDto(
                targetRepositoryFullName,
                targetSelection.TargetProjectId,
                true,
                createOptions,
                [],
                [],
                [],
                [],
                totalLinkedProjectCount,
                inaccessibleLinkedProjectCount);
        }

        var sourceByName = sourceStatusStructure.Options.ToDictionary(option => option.Name, StringComparer.OrdinalIgnoreCase);
        var targetByName = targetStatusStructure.Options.ToDictionary(option => option.Name, StringComparer.OrdinalIgnoreCase);

        var toCreate = sourceStatusStructure.Options
            .Where(source => !targetByName.ContainsKey(source.Name))
            .Select(MapToStatusOptionDto)
            .OrderBy(option => option.Order)
            .ToArray();

        var toUpdate = conflictStrategy switch
        {
            MigrationConflictStrategy.Skip => Array.Empty<ProjectBoardStatusOptionDto>(),
            _ => sourceStatusStructure.Options
                .Where(source => targetByName.TryGetValue(source.Name, out var target) && !HasSameStatusValues(source, target))
                .Select(source =>
                {
                    var targetOption = targetByName[source.Name];
                    return MapToStatusOptionDto(source with { Id = targetOption.Id });
                })
                .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
        };

        var warnings = new List<string>();
        var toDelete = new List<ProjectBoardStatusOptionDto>();
        if (conflictStrategy == MigrationConflictStrategy.Overwrite)
        {
            foreach (var target in targetStatusStructure.Options.Where(target => !sourceByName.ContainsKey(target.Name)))
            {
                if (optionIdsInUse.Contains(target.Id))
                {
                    warnings.Add($"Status option '{target.Name}' was not removed because board items still use it.");
                    continue;
                }

                toDelete.Add(MapToStatusOptionDto(target));
            }
        }

        var skipped = BuildSkippedStatusItems(sourceStatusStructure.Options, targetByName, conflictStrategy);

        return new ProjectBoardStatusSyncRepositoryPreviewDto(
            targetRepositoryFullName,
            targetSelection.TargetProjectId,
            false,
            toCreate,
            toUpdate,
            toDelete.OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            skipped,
            warnings,
            totalLinkedProjectCount,
            inaccessibleLinkedProjectCount);
    }

    private static IReadOnlyList<ProjectBoardStatusOptionDto> BuildSkippedStatusItems(
        IReadOnlyList<ProjectBoardStatusStructureOption> sourceOptions,
        IReadOnlyDictionary<string, ProjectBoardStatusStructureOption> targetByName,
        MigrationConflictStrategy conflictStrategy)
    {
        return sourceOptions
            .Where(source => targetByName.TryGetValue(source.Name, out var target)
                && (conflictStrategy == MigrationConflictStrategy.Skip || HasSameStatusValues(source, target)))
            .Select(source =>
            {
                var targetOption = targetByName[source.Name];
                return MapToStatusOptionDto(targetOption);
            })
            .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ProjectBoardStatusStructureOption> BuildFinalStatusOptionsForApply(
        IReadOnlyList<ProjectBoardStatusStructureOption> sourceOptions,
        IReadOnlyList<ProjectBoardStatusStructureOption> targetOptions,
        MigrationConflictStrategy conflictStrategy,
        IReadOnlySet<string> optionIdsInUse)
    {
        var targetByName = targetOptions.ToDictionary(option => option.Name, StringComparer.OrdinalIgnoreCase);
        var sourceByName = sourceOptions.ToDictionary(option => option.Name, StringComparer.OrdinalIgnoreCase);
        var result = new List<ProjectBoardStatusStructureOption>();

        foreach (var source in sourceOptions.OrderBy(option => option.Order))
        {
            if (targetByName.TryGetValue(source.Name, out var target))
            {
                if (conflictStrategy == MigrationConflictStrategy.Skip)
                {
                    result.Add(target with { Order = result.Count });
                }
                else
                {
                    result.Add(source with { Id = target.Id, Order = result.Count });
                }
            }
            else
            {
                result.Add(source with { Id = string.Empty, Order = result.Count });
            }
        }

        if (conflictStrategy != MigrationConflictStrategy.Overwrite)
        {
            foreach (var target in targetOptions.OrderBy(option => option.Order))
            {
                if (!sourceByName.ContainsKey(target.Name))
                {
                    result.Add(target with { Order = result.Count });
                }
            }

            return result;
        }

        foreach (var target in targetOptions.OrderBy(option => option.Order))
        {
            if (!sourceByName.ContainsKey(target.Name) && optionIdsInUse.Contains(target.Id))
            {
                result.Add(target with { Order = result.Count });
            }
        }

        return result;
    }

    private async Task<LabelSyncRepositoryResultDto> ApplyLabelMigrationAsync(
        string targetRepositoryFullName,
        string targetOwner,
        string targetRepo,
        IReadOnlyList<Label> sourceLabels,
        MigrationConflictStrategy conflictStrategy,
        CancellationToken cancellationToken)
    {
        var createdCount = 0;
        var updatedCount = 0;
        var deletedCount = 0;

        try
        {
            var targetLabels = await _labelRepository.GetLabelsAsync(targetOwner, targetRepo, cancellationToken).ConfigureAwait(false);
            var preview = BuildLabelPreview(targetRepositoryFullName, sourceLabels, targetLabels, conflictStrategy);

            foreach (var label in preview.ToCreate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _labelRepository.CreateLabelAsync(targetOwner, targetRepo, MapToLabelDomain(label, targetRepo), cancellationToken).ConfigureAwait(false);
                createdCount++;
            }

            foreach (var label in preview.ToUpdate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _labelRepository.UpdateLabelAsync(targetOwner, targetRepo, label.Name, MapToLabelDomain(label, targetRepo), cancellationToken).ConfigureAwait(false);
                updatedCount++;
            }

            foreach (var label in preview.ToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _labelRepository.DeleteLabelAsync(targetOwner, targetRepo, label.Name, cancellationToken).ConfigureAwait(false);
                deletedCount++;
            }

            return new LabelSyncRepositoryResultDto(targetRepositoryFullName, createdCount, updatedCount, deletedCount, preview.Skipped.Count, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
        {
            return new LabelSyncRepositoryResultDto(targetRepositoryFullName, createdCount, updatedCount, deletedCount, 0, ex.Message);
        }
    }

    private async Task<MilestoneSyncRepositoryResultDto> ApplyMilestoneMigrationAsync(
        string targetRepositoryFullName,
        string targetOwner,
        string targetRepo,
        IReadOnlyList<Milestone> sourceMilestones,
        MigrationConflictStrategy conflictStrategy,
        CancellationToken cancellationToken)
    {
        var createdCount = 0;
        var updatedCount = 0;
        var deletedCount = 0;

        try
        {
            var targetMilestones = await _milestoneRepository.GetMilestonesAsync(targetOwner, targetRepo, cancellationToken).ConfigureAwait(false);
            var preview = BuildMilestonePreview(targetRepositoryFullName, sourceMilestones, targetMilestones, conflictStrategy);

            foreach (var milestone in preview.ToCreate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _milestoneRepository.CreateMilestoneAsync(targetOwner, targetRepo, MapToMilestoneDomain(milestone), cancellationToken).ConfigureAwait(false);
                createdCount++;
            }

            foreach (var milestone in preview.ToUpdate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _milestoneRepository.UpdateMilestoneAsync(targetOwner, targetRepo, milestone.Number, MapToMilestoneDomain(milestone), cancellationToken).ConfigureAwait(false);
                updatedCount++;
            }

            foreach (var milestone in preview.ToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _milestoneRepository.DeleteMilestoneAsync(targetOwner, targetRepo, milestone.Number, cancellationToken).ConfigureAwait(false);
                deletedCount++;
            }

            return new MilestoneSyncRepositoryResultDto(targetRepositoryFullName, createdCount, updatedCount, deletedCount, preview.Skipped.Count, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
        {
            return new MilestoneSyncRepositoryResultDto(targetRepositoryFullName, createdCount, updatedCount, deletedCount, 0, ex.Message);
        }
    }

    private static LabelSyncRepositoryPreviewDto BuildLabelPreview(
        string targetRepositoryFullName,
        IReadOnlyList<Label> sourceLabels,
        IReadOnlyList<Label> targetLabels,
        MigrationConflictStrategy conflictStrategy)
    {
        var sourceByName = sourceLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);
        var targetByName = targetLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);

        var toCreate = sourceLabels
            .Where(source => !targetByName.ContainsKey(source.Name))
            .Select(source => MapToLabelDto(source, targetRepositoryFullName))
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var toUpdate = conflictStrategy switch
        {
            MigrationConflictStrategy.Skip => Array.Empty<LabelDto>(),
            _ => sourceLabels
                .Where(source => targetByName.TryGetValue(source.Name, out var target) && !HasSameLabelValues(source, target))
                .Select(source => MapToLabelDto(source, targetRepositoryFullName))
                .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
        };

        var toDelete = conflictStrategy == MigrationConflictStrategy.Overwrite
            ? targetLabels
                .Where(target => !sourceByName.ContainsKey(target.Name))
                .Select(target => MapToLabelDto(target, targetRepositoryFullName))
                .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<LabelDto>();

        var skipped = BuildSkippedItems(
            sourceLabels,
            targetByName,
            conflictStrategy,
            static item => item.Name,
            static (left, right) => HasSameLabelValues(left, right),
            source => MapToLabelDto(source, targetRepositoryFullName));

        return new LabelSyncRepositoryPreviewDto(targetRepositoryFullName, toCreate, toUpdate, toDelete, skipped);
    }

    private static MilestoneSyncRepositoryPreviewDto BuildMilestonePreview(
        string targetRepositoryFullName,
        IReadOnlyList<Milestone> sourceMilestones,
        IReadOnlyList<Milestone> targetMilestones,
        MigrationConflictStrategy conflictStrategy)
    {
        var sourceByTitle = sourceMilestones.ToDictionary(milestone => milestone.Title, StringComparer.OrdinalIgnoreCase);
        var targetByTitle = targetMilestones.ToDictionary(milestone => milestone.Title, StringComparer.OrdinalIgnoreCase);

        var toCreate = sourceMilestones
            .Where(source => !targetByTitle.ContainsKey(source.Title))
            .Select(MapToMilestoneDto)
            .OrderBy(milestone => milestone.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var toUpdate = conflictStrategy switch
        {
            MigrationConflictStrategy.Skip => Array.Empty<MilestoneDto>(),
            _ => sourceMilestones
                .Where(source => targetByTitle.TryGetValue(source.Title, out var target) && !HasSameMilestoneValues(source, target))
                .Select(source =>
                {
                    var sourceDto = MapToMilestoneDto(source);
                    var targetMilestone = targetByTitle[source.Title];
                    return sourceDto with { Number = targetMilestone.Number };
                })
                .OrderBy(milestone => milestone.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
        };

        var toDelete = conflictStrategy == MigrationConflictStrategy.Overwrite
            ? targetMilestones
                .Where(target => !sourceByTitle.ContainsKey(target.Title))
                .Select(MapToMilestoneDto)
                .OrderBy(milestone => milestone.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<MilestoneDto>();

        var skipped = BuildSkippedItems(
            sourceMilestones,
            targetByTitle,
            conflictStrategy,
            static item => item.Title,
            static (left, right) => HasSameMilestoneValues(left, right),
            MapToMilestoneDto);

        return new MilestoneSyncRepositoryPreviewDto(targetRepositoryFullName, toCreate, toUpdate, toDelete, skipped);
    }

    private static IReadOnlyList<TDto> BuildSkippedItems<TDomain, TDto>(
        IReadOnlyList<TDomain> sourceItems,
        IReadOnlyDictionary<string, TDomain> targetByKey,
        MigrationConflictStrategy conflictStrategy,
        Func<TDomain, string> getKey,
        Func<TDomain, TDomain, bool> hasSameValues,
        Func<TDomain, TDto> mapToDto)
    {
        return sourceItems
            .Where(source => targetByKey.TryGetValue(getKey(source), out var target)
                && (conflictStrategy == MigrationConflictStrategy.Skip || hasSameValues(source, target)))
            .Select(mapToDto)
            .ToArray();
    }

    private static MigrationTargetBoardSelectionDto ResolveTargetBoardSelection(
        MigrationBoardSelectionDto boardSelection,
        string targetRepositoryFullName)
    {
        var selection = boardSelection.TargetSelections
            .FirstOrDefault(target => target.RepositoryFullName.Equals(targetRepositoryFullName, StringComparison.OrdinalIgnoreCase));

        if (selection is null)
        {
            throw new ArgumentException(
                $"Board selection is missing for target repository '{targetRepositoryFullName}'.",
                nameof(boardSelection));
        }

        return selection;
    }

    private static ProjectBoardStatusOptionDto MapToStatusOptionDto(ProjectBoardStatusStructureOption option)
        => new(option.Id, option.Name, option.Colour, option.Description, option.Order);

    private static bool HasSameStatusValues(ProjectBoardStatusStructureOption left, ProjectBoardStatusStructureOption right)
        => string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Colour, right.Colour, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Description, right.Description, StringComparison.Ordinal)
            && left.Order == right.Order;

    private static LabelDto MapToLabelDto(Label label, string repositoryFullName)
        => new(label.Name, label.Colour, label.Description, repositoryFullName);

    private static Label MapToLabelDomain(LabelDto label, string repositoryName)
        => new()
        {
            Name = label.Name,
            Colour = label.Colour,
            Description = label.Description,
            RepositoryName = repositoryName,
        };

    private static MilestoneDto MapToMilestoneDto(Milestone milestone)
        => new(
            milestone.Id,
            milestone.Number,
            milestone.Title,
            milestone.Description,
            milestone.State,
            milestone.DueOn,
            milestone.OpenIssues,
            milestone.ClosedIssues);

    private static Milestone MapToMilestoneDomain(MilestoneDto milestone)
        => new()
        {
            Id = milestone.Id,
            Number = milestone.Number,
            Title = milestone.Title,
            Description = milestone.Description,
            State = milestone.State,
            DueOn = milestone.DueOn,
            OpenIssues = milestone.OpenIssues,
            ClosedIssues = milestone.ClosedIssues,
        };

    private static bool HasSameLabelValues(Label left, Label right)
        => string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Colour, right.Colour, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Description, right.Description, StringComparison.Ordinal);

    private static bool HasSameMilestoneValues(Milestone left, Milestone right)
        => string.Equals(left.Title, right.Title, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Description, right.Description, StringComparison.Ordinal)
            && string.Equals(left.State, right.State, StringComparison.OrdinalIgnoreCase)
            && left.DueOn == right.DueOn;

    private static IReadOnlyList<string> NormaliseTargetRepositories(IReadOnlyList<string> targetRepositoryFullNames, string sourceRepositoryFullName)
    {
        ArgumentNullException.ThrowIfNull(targetRepositoryFullNames);

        var normalised = targetRepositoryFullNames
            .Select(repository => repository?.Trim())
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .Select(repository => repository!)
            .Where(repository => !repository.Equals(sourceRepositoryFullName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalised.Length == 0)
        {
            throw new ArgumentException("At least one target repository must be provided.", nameof(targetRepositoryFullNames));
        }

        return normalised;
    }

    private static RepositoryCoordinates SplitRepositoryFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Repository full name must be provided.", nameof(fullName));
        }

        var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Repository '{fullName}' must be in owner/repository format.", nameof(fullName));
        }

        return new RepositoryCoordinates(parts[0], parts[1]);
    }

    private static void EnsureAtLeastOneScopeSelected(MigrationScopeDto scope)
    {
        if (!scope.IncludeLabels && !scope.IncludeMilestones && !scope.IncludeProjectBoardColumns)
        {
            throw new ArgumentException("At least one migration item type must be selected.", nameof(scope));
        }
    }

    private static void EnsureBoardSelectionWhenRequired(MigrationScopeDto scope, MigrationBoardSelectionDto? boardSelection)
    {
        if (!scope.IncludeProjectBoardColumns)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(boardSelection);

        if (string.IsNullOrWhiteSpace(boardSelection.SourceProjectId))
        {
            throw new ArgumentException("A source project board must be selected when project board columns are included.", nameof(boardSelection));
        }

        if (boardSelection.TargetSelections.Count == 0)
        {
            throw new ArgumentException("At least one target board selection must be provided.", nameof(boardSelection));
        }
    }

    private static readonly IReadOnlySet<string> EmptyOptionIdSet = new HashSet<string>(StringComparer.Ordinal);

    private sealed record RepositoryCoordinates(string Owner, string Name);
}
