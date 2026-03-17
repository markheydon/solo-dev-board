using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Default implementation of <see cref="IMigrationService"/>.</summary>
public sealed class MigrationService : IMigrationService
{
    private readonly ILabelRepository _labelRepository;
    private readonly IMilestoneRepository _milestoneRepository;

    /// <summary>Initialises a new instance of the <see cref="MigrationService"/> class.</summary>
    /// <param name="labelRepository">The label repository used for migration operations.</param>
    /// <param name="milestoneRepository">The milestone repository used for migration operations.</param>
    public MigrationService(ILabelRepository labelRepository, IMilestoneRepository milestoneRepository)
    {
        ArgumentNullException.ThrowIfNull(labelRepository);
        ArgumentNullException.ThrowIfNull(milestoneRepository);

        _labelRepository = labelRepository;
        _milestoneRepository = milestoneRepository;
    }

    /// <inheritdoc/>
    public async Task<MigrationPreviewDto> PreviewMigrationAsync(
        string sourceRepositoryFullName,
        IReadOnlyList<string> targetRepositoryFullNames,
        MigrationScopeDto scope,
        MigrationConflictStrategy conflictStrategy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRepositoryFullName);
        ArgumentNullException.ThrowIfNull(scope);
        EnsureAtLeastOneScopeSelected(scope);

        var source = SplitRepositoryFullName(sourceRepositoryFullName);
        var normalisedTargets = NormaliseTargetRepositories(targetRepositoryFullNames, sourceRepositoryFullName);

        var sourceLabels = scope.IncludeLabels
            ? await _labelRepository.GetLabelsAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false)
            : [];
        var sourceMilestones = scope.IncludeMilestones
            ? await _milestoneRepository.GetMilestonesAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false)
            : [];

        var labelPreviews = new List<LabelSyncRepositoryPreviewDto>();
        var milestonePreviews = new List<MilestoneSyncRepositoryPreviewDto>();

        foreach (var targetRepositoryFullName in normalisedTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = SplitRepositoryFullName(targetRepositoryFullName);

            if (scope.IncludeLabels)
            {
                var targetLabels = await _labelRepository.GetLabelsAsync(target.Owner, target.Name, cancellationToken).ConfigureAwait(false);
                var labelPreview = BuildLabelPreview(targetRepositoryFullName, sourceLabels, targetLabels, conflictStrategy);
                labelPreviews.Add(labelPreview);
            }

            if (scope.IncludeMilestones)
            {
                var targetMilestones = await _milestoneRepository.GetMilestonesAsync(target.Owner, target.Name, cancellationToken).ConfigureAwait(false);
                var milestonePreview = BuildMilestonePreview(targetRepositoryFullName, sourceMilestones, targetMilestones, conflictStrategy);
                milestonePreviews.Add(milestonePreview);
            }
        }

        return new MigrationPreviewDto(
            conflictStrategy,
            labelPreviews.OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray(),
            milestonePreviews.OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    /// <inheritdoc/>
    public async Task<MigrationResultDto> ApplyMigrationAsync(
        string sourceRepositoryFullName,
        IReadOnlyList<string> targetRepositoryFullNames,
        MigrationScopeDto scope,
        MigrationConflictStrategy conflictStrategy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRepositoryFullName);
        ArgumentNullException.ThrowIfNull(scope);
        EnsureAtLeastOneScopeSelected(scope);

        var source = SplitRepositoryFullName(sourceRepositoryFullName);
        var normalisedTargets = NormaliseTargetRepositories(targetRepositoryFullNames, sourceRepositoryFullName);

        var sourceLabels = scope.IncludeLabels
            ? await _labelRepository.GetLabelsAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false)
            : [];
        var sourceMilestones = scope.IncludeMilestones
            ? await _milestoneRepository.GetMilestonesAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false)
            : [];

        var labelResults = new List<LabelSyncRepositoryResultDto>();
        var milestoneResults = new List<MilestoneSyncRepositoryResultDto>();

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
        }

        return new MigrationResultDto(
            conflictStrategy,
            labelResults.OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray(),
            milestoneResults.OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase).ToArray());
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
        if (!scope.IncludeLabels && !scope.IncludeMilestones)
        {
            throw new ArgumentException("At least one migration item type must be selected.", nameof(scope));
        }
    }

    private sealed record RepositoryCoordinates(string Owner, string Name);
}
