using SoloDevBoard.Application.GitHub;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Default implementation of <see cref="ILabelManagerService"/>.</summary>
public sealed class LabelService : ILabelManagerService
{
    private const string DefaultNewLabelColour = "ededed";

    private readonly ILabelRepository _labelRepository;
    private readonly IGitHubService _gitHubService;

    /// <summary>Initialises a new instance of the <see cref="LabelService"/> class.</summary>
    /// <param name="labelRepository">The repository used to manage labels in GitHub repositories.</param>
    /// <param name="gitHubService">The GitHub service used to add and remove labels on issues and pull requests.</param>
    public LabelService(ILabelRepository labelRepository, IGitHubService gitHubService)
    {
        ArgumentNullException.ThrowIfNull(labelRepository);
        ArgumentNullException.ThrowIfNull(gitHubService);
        _labelRepository = labelRepository;
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelDto>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default, bool forceReload = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var labels = await _labelRepository.GetLabelsAsync(owner, repo, cancellationToken, forceReload).ConfigureAwait(false);
        return labels.Select(label => MapToDto(label, repo)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelDto>> GetLabelsForRepositoriesAsync(string owner, IReadOnlyList<string> repositories, CancellationToken cancellationToken = default, bool forceReload = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        var normalisedRepositories = NormaliseRepositories(repositories);

        var labels = new List<LabelDto>();
        foreach (var repository in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var repositoryLabels = await _labelRepository.GetLabelsAsync(owner, repository, cancellationToken, forceReload).ConfigureAwait(false);
            labels.AddRange(repositoryLabels.Select(label => MapToDto(label, repository)));
        }

        return labels.Distinct().ToArray();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Groups the requested repositories by owner, loads labels per owner, rewrites each
    /// <see cref="LabelDto.RepositoryName"/> to <c>owner/repository</c>, then groups labels by name
    /// to compute presence and missing-repository lists.
    /// </remarks>
    public async Task<IReadOnlyList<LabelMatrixRowDto>> GetLabelMatrixAsync(IReadOnlyList<string> repositoryFullNames, CancellationToken cancellationToken = default, bool forceReload = false)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        var selectedFullNames = repositoryFullNames
            .Where(fullName => !string.IsNullOrWhiteSpace(fullName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedFullNames.Length == 0)
        {
            return [];
        }

        var repositoriesByOwner = RepositoryFullName.GroupByOwner(selectedFullNames);
        if (repositoriesByOwner.Count == 0)
        {
            return [];
        }

        var consolidatedLabels = new List<LabelDto>();
        foreach (var ownerGroup in repositoriesByOwner)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var labels = await GetLabelsForRepositoriesAsync(ownerGroup.Key, ownerGroup.Value, cancellationToken, forceReload).ConfigureAwait(false);
            consolidatedLabels.AddRange(labels.Select(label => label with { RepositoryName = $"{ownerGroup.Key}/{label.RepositoryName}" }));
        }

        return BuildMatrixRows(consolidatedLabels, selectedFullNames);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelDto>> CreateLabelAsync(string owner, IReadOnlyList<string> repositories, LabelDto label, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentNullException.ThrowIfNull(label);

        var normalisedRepositories = NormaliseRepositories(repositories);
        var createdLabels = new List<LabelDto>();

        foreach (var repository in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var created = await _labelRepository.CreateLabelAsync(owner, repository, MapToDomain(label, repository), cancellationToken).ConfigureAwait(false);
            createdLabels.Add(MapToDto(created, repository));
        }

        return createdLabels.ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelDto>> UpdateLabelAsync(string owner, IReadOnlyList<string> repositories, string labelName, LabelDto label, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);
        ArgumentNullException.ThrowIfNull(label);

        var normalisedRepositories = NormaliseRepositories(repositories);
        var updatedLabels = new List<LabelDto>();

        foreach (var repository in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var updated = await _labelRepository.UpdateLabelAsync(owner, repository, labelName, MapToDomain(label, repository), cancellationToken).ConfigureAwait(false);
            updatedLabels.Add(MapToDto(updated, repository));
        }

        return updatedLabels.ToArray();
    }

    /// <inheritdoc/>
    public async Task DeleteLabelAsync(string owner, IReadOnlyList<string> repositories, string labelName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);

        var normalisedRepositories = NormaliseRepositories(repositories);
        foreach (var repository in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _labelRepository.DeleteLabelAsync(owner, repository, labelName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<LabelBulkDeleteResultDto> BulkDeleteLabelsAsync(IReadOnlyList<LabelBulkDeleteTargetDto> targets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targets);

        if (targets.Count == 0)
        {
            throw new ArgumentException("At least one bulk delete target must be provided.", nameof(targets));
        }

        var deletedCount = 0;
        var skippedCount = 0;
        var errors = new List<LabelBulkDeleteErrorDto>();

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(target.LabelName))
            {
                continue;
            }

            var repositoryFullNames = target.RepositoryFullNames
                .Where(repositoryFullName => !string.IsNullOrWhiteSpace(repositoryFullName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(repositoryFullName => repositoryFullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var repositoryFullName in repositoryFullNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var repository = SplitRepositoryFullName(repositoryFullName);
                    await _labelRepository
                        .DeleteLabelAsync(repository.Owner, repository.Name, target.LabelName, cancellationToken)
                        .ConfigureAwait(false);
                    deletedCount++;
                }
                catch (KeyNotFoundException)
                {
                    skippedCount++;
                }
                catch (Exception ex) when (ex is HttpRequestException or ArgumentException)
                {
                    errors.Add(new LabelBulkDeleteErrorDto(target.LabelName, repositoryFullName, ex.Message));
                }
            }
        }

        return new LabelBulkDeleteResultDto(deletedCount, skippedCount, errors);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Ensures the destination label exists, then adds it to each labelled item and removes the source.
    /// The source label is deleted only when every item retag succeeded. This path never calls
    /// <see cref="ILabelRepository.UpdateLabelAsync"/>, so rename is not used as a merge.
    /// If adding the destination succeeds but removing the source fails, the item keeps both labels
    /// and the failure is recorded in <see cref="LabelRemapResultDto.Errors"/> without rolling back
    /// the add; callers must surface per-item errors so operators can retry or fix manually.
    /// </remarks>
    public async Task<LabelRemapResultDto> RemapLabelAsync(string owner, string repo, string sourceLabelName, string destinationLabelName, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationLabelName);

        var sourceName = sourceLabelName.Trim();
        var destinationName = destinationLabelName.Trim();
        var repositoryFullName = $"{owner}/{repo}";

        if (string.Equals(sourceName, destinationName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and destination labels must be different names.", nameof(destinationLabelName));
        }

        ReportPreviewProgress(
            progress,
            $"Remapping '{sourceName}' → '{destinationName}' on {repositoryFullName}: finding labelled items...");

        var labels = await _labelRepository.GetLabelsAsync(owner, repo, cancellationToken, forceReload: true).ConfigureAwait(false);
        var source = labels.FirstOrDefault(label => string.Equals(label.Name, sourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Source label '{sourceName}' was not found in '{owner}/{repo}'.");

        var destination = labels.FirstOrDefault(label => string.Equals(label.Name, destinationName, StringComparison.OrdinalIgnoreCase));
        var destinationCreated = false;

        if (destination is null)
        {
            destination = await _labelRepository
                .CreateLabelAsync(
                    owner,
                    repo,
                    new Label
                    {
                        Name = destinationName,
                        Colour = string.IsNullOrWhiteSpace(source.Colour) ? DefaultNewLabelColour : source.Colour,
                        Description = source.Description,
                        RepositoryName = repo,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            destinationCreated = true;
        }

        var workItems = await _labelRepository
            .GetWorkItemsWithLabelAsync(owner, repo, source.Name, cancellationToken)
            .ConfigureAwait(false);

        var succeededItemCount = 0;
        var errors = new List<LabelRemapItemErrorDto>();

        if (workItems.Count == 0)
        {
            ReportPreviewProgress(
                progress,
                $"Remapping '{sourceName}' → '{destinationName}' on {repositoryFullName}: no labelled items found, deleting source label...");
        }

        for (var itemIndex = 0; itemIndex < workItems.Count; itemIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workItem = workItems[itemIndex];
            ReportPreviewProgress(
                progress,
                $"Remapping '{sourceName}' → '{destinationName}' on {repositoryFullName}: retagging item {itemIndex + 1} of {workItems.Count}...");

            try
            {
                await _gitHubService
                    .AddLabelsToTriageItemAsync(owner, repo, workItem.Number, [destination.Name], cancellationToken)
                    .ConfigureAwait(false);
                await _gitHubService
                    .RemoveLabelFromTriageItemAsync(owner, repo, workItem.Number, source.Name, cancellationToken)
                    .ConfigureAwait(false);
                succeededItemCount++;
            }
            catch (Exception ex) when (ex is HttpRequestException or ArgumentException)
            {
                errors.Add(new LabelRemapItemErrorDto(workItem.Number, ex.Message));
            }
        }

        var sourceDeleted = false;
        if (errors.Count == 0)
        {
            await _labelRepository.DeleteLabelAsync(owner, repo, source.Name, cancellationToken).ConfigureAwait(false);
            sourceDeleted = true;
        }

        return new LabelRemapResultDto(
            repo,
            source.Name,
            destination.Name,
            succeededItemCount,
            errors.Count,
            sourceDeleted,
            destinationCreated,
            errors);
    }

    /// <inheritdoc/>
    public async Task<LabelSyncPreviewDto> SyncLabelsAsync(string sourceOwner, string sourceRepo, string targetOwner, string targetRepo, bool applyChanges = false, bool keepAreaLabels = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRepo);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRepo);

        var sourceLabels = await _labelRepository.GetLabelsAsync(sourceOwner, sourceRepo, cancellationToken).ConfigureAwait(false);
        var targetLabels = await _labelRepository.GetLabelsAsync(targetOwner, targetRepo, cancellationToken).ConfigureAwait(false);

        var preview = BuildSyncPreview(targetOwner, targetRepo, sourceLabels, targetLabels, keepAreaLabels);

        if (applyChanges)
        {
            await ApplySyncPreviewAsync(targetOwner, targetRepo, preview, cancellationToken).ConfigureAwait(false);
        }

        return preview;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelSyncRepositoryPreviewDto>> PreviewLabelSynchronisationAsync(string sourceRepositoryFullName, IReadOnlyList<string> targetRepositoryFullNames, bool keepAreaLabels = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRepositoryFullName);
        var source = SplitRepositoryFullName(sourceRepositoryFullName);
        var normalisedTargets = NormaliseRepositories(targetRepositoryFullNames);

        var sourceLabels = await _labelRepository.GetLabelsAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false);
        var previews = new List<LabelSyncRepositoryPreviewDto>();

        foreach (var targetRepositoryFullName in normalisedTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var target = SplitRepositoryFullName(targetRepositoryFullName);
            var targetLabels = await _labelRepository.GetLabelsAsync(target.Owner, target.Name, cancellationToken).ConfigureAwait(false);

            var preview = BuildSyncPreview(target.Owner, target.Name, sourceLabels, targetLabels, keepAreaLabels);
            previews.Add(new LabelSyncRepositoryPreviewDto(
                targetRepositoryFullName,
                preview.ToAdd,
                preview.ToUpdate,
                preview.ToDelete,
                preview.Skipped,
                preview.KeptAreaLabels,
                []));
        }

        return previews
            .OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelSyncRepositoryResultDto>> ApplyLabelSynchronisationAsync(string sourceRepositoryFullName, IReadOnlyList<string> targetRepositoryFullNames, bool keepAreaLabels = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRepositoryFullName);
        var source = SplitRepositoryFullName(sourceRepositoryFullName);
        var normalisedTargets = NormaliseRepositories(targetRepositoryFullNames);

        var sourceLabels = await _labelRepository.GetLabelsAsync(source.Owner, source.Name, cancellationToken).ConfigureAwait(false);
        var results = new List<LabelSyncRepositoryResultDto>();

        foreach (var targetRepositoryFullName in normalisedTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var target = SplitRepositoryFullName(targetRepositoryFullName);
            var createdCount = 0;
            var updatedCount = 0;
            var deletedCount = 0;

            try
            {
                var targetLabels = await _labelRepository.GetLabelsAsync(target.Owner, target.Name, cancellationToken).ConfigureAwait(false);
                var preview = BuildSyncPreview(target.Owner, target.Name, sourceLabels, targetLabels, keepAreaLabels);

                foreach (var label in preview.ToAdd)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _labelRepository.CreateLabelAsync(target.Owner, target.Name, MapToDomain(label, target.Name), cancellationToken).ConfigureAwait(false);
                    createdCount++;
                }

                foreach (var label in preview.ToUpdate)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _labelRepository.UpdateLabelAsync(target.Owner, target.Name, label.Name, MapToDomain(label, target.Name), cancellationToken).ConfigureAwait(false);
                    updatedCount++;
                }

                foreach (var label in preview.ToDelete)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _labelRepository.DeleteLabelAsync(target.Owner, target.Name, label.Name, cancellationToken).ConfigureAwait(false);
                    deletedCount++;
                }

                results.Add(new LabelSyncRepositoryResultDto(
                    targetRepositoryFullName,
                    createdCount,
                    updatedCount,
                    deletedCount,
                    preview.Skipped.Count,
                    null));
            }
            catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
            {
                results.Add(new LabelSyncRepositoryResultDto(
                    targetRepositoryFullName,
                    createdCount,
                    updatedCount,
                    deletedCount,
                    0,
                    ex.Message));
            }
        }

        return results
            .OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LabelDto>> GetRecommendedTaxonomyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<LabelDto>>(RecommendedLabelTaxonomyCatalog.SoloDevBoard.ToArray());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecommendedLabelStrategyDto>> GetRecommendedLabelStrategiesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<RecommendedLabelStrategyDto>>(RecommendedLabelTaxonomyCatalog.Strategies.ToArray());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto>> PreviewRecommendedTaxonomyAsync(string strategyId, IReadOnlyList<string> repositories, bool removeLabelsOutsideTaxonomy = false, bool keepAreaLabels = true, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        var normalisedRepositories = NormaliseRepositories(repositories);

        var strategyLabels = ResolveRecommendedStrategyLabels(strategyId);
        var previews = new List<RecommendedTaxonomyRepositoryPreviewDto>();

        ReportPreviewProgress(progress, "Previewing taxonomy changes...");

        for (var repositoryIndex = 0; repositoryIndex < normalisedRepositories.Count; repositoryIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var repositoryFullName = normalisedRepositories[repositoryIndex];
            var repository = SplitRepositoryFullName(repositoryFullName);
            ReportPreviewProgress(
                progress,
                normalisedRepositories.Count > 1
                    ? $"Loading labels for {repositoryFullName} ({repositoryIndex + 1} of {normalisedRepositories.Count})..."
                    : $"Loading labels for {repositoryFullName}...");

            var existing = await _labelRepository.GetLabelsAsync(repository.Owner, repository.Name, cancellationToken).ConfigureAwait(false);
            var preview = BuildRepositoryPreview(repositoryFullName, strategyLabels, existing, removeLabelsOutsideTaxonomy, keepAreaLabels);

            if (removeLabelsOutsideTaxonomy && preview.ToDelete.Count > 0)
            {
                preview = await ClassifyExtraLabelsByUsageAsync(repository.Owner, repository.Name, preview, progress, cancellationToken).ConfigureAwait(false);
            }

            previews.Add(preview);
        }

        return previews
            .OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecommendedTaxonomyRepositoryResultDto>> ApplyRecommendedTaxonomyAsync(string strategyId, IReadOnlyList<string> repositories, bool removeLabelsOutsideTaxonomy = false, bool keepAreaLabels = true, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        var normalisedRepositories = NormaliseRepositories(repositories);

        var strategyLabels = ResolveRecommendedStrategyLabels(strategyId);
        var results = new List<RecommendedTaxonomyRepositoryResultDto>();

        ReportPreviewProgress(progress, "Applying taxonomy changes...");

        for (var repositoryIndex = 0; repositoryIndex < normalisedRepositories.Count; repositoryIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var repositoryFullName = normalisedRepositories[repositoryIndex];
            ReportPreviewProgress(
                progress,
                normalisedRepositories.Count > 1
                    ? $"Applying changes to {repositoryFullName} ({repositoryIndex + 1} of {normalisedRepositories.Count})..."
                    : $"Applying changes to {repositoryFullName}...");

            var createdCount = 0;
            var updatedCount = 0;
            var deletedCount = 0;
            var deleteErrors = new List<RecommendedTaxonomyLabelDeleteErrorDto>();

            try
            {
                var repository = SplitRepositoryFullName(repositoryFullName);
                var existing = await _labelRepository.GetLabelsAsync(repository.Owner, repository.Name, cancellationToken).ConfigureAwait(false);
                var preview = BuildRepositoryPreview(repositoryFullName, strategyLabels, existing, removeLabelsOutsideTaxonomy, keepAreaLabels);

                if (removeLabelsOutsideTaxonomy && preview.ToDelete.Count > 0)
                {
                    preview = await ClassifyExtraLabelsByUsageAsync(repository.Owner, repository.Name, preview, null, cancellationToken).ConfigureAwait(false);
                }

                foreach (var labelToCreate in preview.ToCreate)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _labelRepository
                        .CreateLabelAsync(repository.Owner, repository.Name, MapToDomain(labelToCreate, repository.Name), cancellationToken)
                        .ConfigureAwait(false);
                    createdCount++;
                }

                foreach (var labelToUpdate in preview.ToUpdate)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _labelRepository
                        .UpdateLabelAsync(repository.Owner, repository.Name, labelToUpdate.Name, MapToDomain(labelToUpdate, repository.Name), cancellationToken)
                        .ConfigureAwait(false);
                    updatedCount++;
                }

                foreach (var labelToDelete in preview.ToDelete)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await _labelRepository
                            .DeleteLabelAsync(repository.Owner, repository.Name, labelToDelete.Name, cancellationToken)
                            .ConfigureAwait(false);
                        deletedCount++;
                    }
                    catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
                    {
                        deleteErrors.Add(new RecommendedTaxonomyLabelDeleteErrorDto(labelToDelete.Name, ex.Message));
                    }
                }

                results.Add(new RecommendedTaxonomyRepositoryResultDto(
                    repositoryFullName,
                    createdCount,
                    updatedCount,
                    deletedCount,
                    preview.Skipped.Count,
                    deleteErrors,
                    null));
            }
            catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
            {
                results.Add(new RecommendedTaxonomyRepositoryResultDto(
                    repositoryFullName,
                    createdCount,
                    updatedCount,
                    deletedCount,
                    0,
                    deleteErrors,
                    ex.Message));
            }
        }

        return results
            .OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Maps a domain label record to the application DTO shape.</summary>
    /// <param name="label">The domain label to map.</param>
    /// <param name="repositoryName">The repository name associated with the label.</param>
    /// <returns>A mapped application label DTO.</returns>
    private static LabelDto MapToDto(Label label, string repositoryName)
        => new(label.Name, label.Colour, label.Description, repositoryName);

    /// <summary>Resolves strategy labels by strategy identifier.</summary>
    /// <param name="strategyId">The strategy identifier to resolve.</param>
    /// <returns>The label set for the requested strategy.</returns>
    /// <exception cref="ArgumentException">Thrown when the strategy identifier is unsupported.</exception>
    private static IReadOnlyList<LabelDto> ResolveRecommendedStrategyLabels(string strategyId)
    {
        if (RecommendedLabelTaxonomyCatalog.TryGetLabels(strategyId, out var labels))
        {
            return labels;
        }

        throw new ArgumentException($"Unsupported recommended strategy '{strategyId}'.", nameof(strategyId));
    }

    /// <summary>Builds a preview for one repository against a strategy label set.</summary>
    /// <param name="repositoryFullName">The owner/repository full name.</param>
    /// <param name="strategyLabels">The strategy labels to compare against.</param>
    /// <param name="existingLabels">The labels currently present in the repository.</param>
    /// <param name="removeLabelsOutsideTaxonomy">When <see langword="true" />, includes labels to delete that are not in the strategy set.</param>
    /// <param name="keepAreaLabels">When <see langword="true" /> and remove-outside is enabled, labels with the <c>area/</c> prefix are kept instead of deleted.</param>
    /// <returns>A repository preview showing create, update, delete, and skip actions.</returns>
    private static RecommendedTaxonomyRepositoryPreviewDto BuildRepositoryPreview(
        string repositoryFullName,
        IReadOnlyList<LabelDto> strategyLabels,
        IReadOnlyList<Label> existingLabels,
        bool removeLabelsOutsideTaxonomy,
        bool keepAreaLabels)
    {
        var existingByName = existingLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);
        var strategyByName = strategyLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);

        var toCreate = strategyLabels
            .Where(label => !existingByName.ContainsKey(label.Name))
            .Select(label => label with { RepositoryName = repositoryFullName })
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var toUpdate = strategyLabels
            .Where(label => existingByName.TryGetValue(label.Name, out var existing)
                && !HasSameValues(MapToDomain(label, repositoryFullName), existing))
            .Select(label => label with { RepositoryName = repositoryFullName })
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var skipped = strategyLabels
            .Where(label => existingByName.TryGetValue(label.Name, out var existing)
                && HasSameValues(MapToDomain(label, repositoryFullName), existing))
            .Select(label => label with { RepositoryName = repositoryFullName })
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var extraLabels = removeLabelsOutsideTaxonomy
            ? existingLabels.Where(label => !strategyByName.ContainsKey(label.Name))
            : [];

        var keptAreaLabels = removeLabelsOutsideTaxonomy && keepAreaLabels
            ? extraLabels
                .Where(label => LabelTaxonomyPrefixes.IsAreaLabel(label.Name))
                .Select(label => MapToDto(label, repositoryFullName))
                .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        var toDelete = removeLabelsOutsideTaxonomy
            ? extraLabels
                .Where(label => !keepAreaLabels || !LabelTaxonomyPrefixes.IsAreaLabel(label.Name))
                .Select(label => MapToDto(label, repositoryFullName))
                .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        return new RecommendedTaxonomyRepositoryPreviewDto(repositoryFullName, toCreate, toUpdate, toDelete, [], skipped, keptAreaLabels);
    }

    /// <summary>Splits extra labels into unused deletes and labelled work items that need remapping.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="preview">The repository preview containing candidate extra labels in <see cref="RecommendedTaxonomyRepositoryPreviewDto.ToDelete"/>.</param>
    /// <param name="progress">Optional callback that receives human-readable progress messages during classification.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A preview with extras moved into <see cref="RecommendedTaxonomyRepositoryPreviewDto.ToRemap"/> when they are in use.</returns>
    private async Task<RecommendedTaxonomyRepositoryPreviewDto> ClassifyExtraLabelsByUsageAsync(
        string owner,
        string repo,
        RecommendedTaxonomyRepositoryPreviewDto preview,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var toDelete = new List<LabelDto>();
        var toRemap = new List<LabelDto>();
        var candidates = preview.ToDelete;

        for (var labelIndex = 0; labelIndex < candidates.Count; labelIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var label = candidates[labelIndex];
            ReportPreviewProgress(
                progress,
                $"Checking extra label usage on {preview.RepositoryFullName} ({labelIndex + 1} of {candidates.Count})...");

            var workItems = await _labelRepository
                .GetWorkItemsWithLabelAsync(owner, repo, label.Name, cancellationToken)
                .ConfigureAwait(false);

            if (workItems.Count > 0)
            {
                toRemap.Add(label);
            }
            else
            {
                toDelete.Add(label);
            }
        }

        return new RecommendedTaxonomyRepositoryPreviewDto(
            preview.RepositoryFullName,
            preview.ToCreate,
            preview.ToUpdate,
            toDelete,
            toRemap,
            preview.Skipped,
            preview.KeptAreaLabels);
    }

    /// <summary>Reports a preview progress message when a listener is attached.</summary>
    /// <param name="progress">The optional progress listener.</param>
    /// <param name="message">The message to report.</param>
    private static void ReportPreviewProgress(IProgress<string>? progress, string message)
        => progress?.Report(message);

    /// <summary>Maps an application label DTO to a domain label record.</summary>
    /// <param name="label">The application label DTO to map.</param>
    /// <param name="repositoryName">The repository name associated with the label.</param>
    /// <returns>A mapped domain label record.</returns>
    private static Label MapToDomain(LabelDto label, string repositoryName)
        => new()
        {
            Name = label.Name,
            Colour = label.Colour,
            Description = label.Description,
            RepositoryName = repositoryName,
        };

    /// <summary>Builds a synchronisation preview by comparing source and target label sets.</summary>
    /// <param name="targetOwner">The target repository owner.</param>
    /// <param name="targetRepo">The target repository name.</param>
    /// <param name="sourceLabels">The labels from the source repository.</param>
    /// <param name="targetLabels">The labels from the target repository.</param>
    /// <param name="keepAreaLabels">When <see langword="true" />, labels with the <c>area/</c> prefix on the target are kept instead of deleted.</param>
    /// <returns>A synchronisation preview containing create, update, delete, and skip actions.</returns>
    private static LabelSyncPreviewDto BuildSyncPreview(string targetOwner, string targetRepo, IReadOnlyList<Label> sourceLabels, IReadOnlyList<Label> targetLabels, bool keepAreaLabels)
    {
        var sourceByName = sourceLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);
        var targetByName = targetLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);
        var repositoryFullName = $"{targetOwner}/{targetRepo}";

        var toAdd = sourceLabels
            .Where(source => !targetByName.ContainsKey(source.Name))
            .Select(source => MapToDto(source, repositoryFullName))
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var toUpdate = sourceLabels
            .Where(source => targetByName.TryGetValue(source.Name, out var target) && !HasSameValues(source, target))
            .Select(source => MapToDto(source, repositoryFullName))
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var extraLabels = targetLabels.Where(target => !sourceByName.ContainsKey(target.Name));

        var keptAreaLabels = keepAreaLabels
            ? extraLabels
                .Where(target => LabelTaxonomyPrefixes.IsAreaLabel(target.Name))
                .Select(target => MapToDto(target, repositoryFullName))
                .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        var toDelete = extraLabels
            .Where(target => !keepAreaLabels || !LabelTaxonomyPrefixes.IsAreaLabel(target.Name))
            .Select(target => MapToDto(target, repositoryFullName))
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var skipped = sourceLabels
            .Where(source => targetByName.TryGetValue(source.Name, out var target) && HasSameValues(source, target))
            .Select(source => MapToDto(source, repositoryFullName))
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new LabelSyncPreviewDto(toAdd, toUpdate, toDelete, skipped, keptAreaLabels);
    }

    /// <summary>Applies a precomputed synchronisation preview to a target repository.</summary>
    /// <param name="targetOwner">The target repository owner.</param>
    /// <param name="targetRepo">The target repository name.</param>
    /// <param name="preview">The preview describing create, update, and delete operations.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that completes when all preview operations have been applied.</returns>
    private async Task ApplySyncPreviewAsync(string targetOwner, string targetRepo, LabelSyncPreviewDto preview, CancellationToken cancellationToken)
    {
        foreach (var label in preview.ToAdd)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _labelRepository.CreateLabelAsync(targetOwner, targetRepo, MapToDomain(label, targetRepo), cancellationToken).ConfigureAwait(false);
        }

        foreach (var label in preview.ToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _labelRepository.UpdateLabelAsync(targetOwner, targetRepo, label.Name, MapToDomain(label, targetRepo), cancellationToken).ConfigureAwait(false);
        }

        foreach (var label in preview.ToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _labelRepository.DeleteLabelAsync(targetOwner, targetRepo, label.Name, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Groups loaded labels by name and computes missing repositories for the current selection.</summary>
    /// <param name="labels">The labels loaded for the selected repositories, with owner-qualified repository names.</param>
    /// <param name="selectedFullNames">The selected repository full names used as the matrix columns.</param>
    /// <returns>A read-only list of matrix rows ordered by label name.</returns>
    private static IReadOnlyList<LabelMatrixRowDto> BuildMatrixRows(IReadOnlyList<LabelDto> labels, IReadOnlyList<string> selectedFullNames)
    {
        var selectedSet = selectedFullNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return labels
            .GroupBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                var repositoriesWithLabel = group
                    .Select(label => label.RepositoryName)
                    .Where(repositoryName => !string.IsNullOrWhiteSpace(repositoryName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(repositoryName => repositoryName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var missingRepositories = selectedSet
                    .Except(repositoriesWithLabel, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(repositoryName => repositoryName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new LabelMatrixRowDto(
                    first.Name,
                    first.Colour,
                    string.IsNullOrWhiteSpace(first.Description) ? LabelMatrixRowDto.MissingDescriptionDisplay : first.Description,
                    repositoriesWithLabel,
                    missingRepositories);
            })
            .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Normalises, validates, and de-duplicates repository names for bulk operations.</summary>
    /// <param name="repositories">The repository names provided by the caller.</param>
    /// <returns>A read-only list of normalised repository names.</returns>
    private static IReadOnlyList<string> NormaliseRepositories(IReadOnlyList<string> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        var normalised = repositories
            .Select(repository => repository?.Trim())
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .Select(repository => repository!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalised.Length == 0)
        {
            throw new ArgumentException("At least one repository must be provided.", nameof(repositories));
        }

        return normalised;
    }

    /// <summary>Splits an owner/repository full name into owner and repository segments.</summary>
    /// <param name="fullName">The full repository name in owner/repository format.</param>
    /// <returns>The split repository coordinates.</returns>
    /// <exception cref="ArgumentException">Thrown when the full name is missing or invalid.</exception>
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

    /// <summary>Determines whether two labels have equivalent values for synchronisation purposes.</summary>
    /// <param name="left">The first label to compare.</param>
    /// <param name="right">The second label to compare.</param>
    /// <returns><see langword="true" /> if labels are equivalent; otherwise, <see langword="false" />.</returns>
    private static bool HasSameValues(Label left, Label right)
        => LabelValueComparer.HaveSameValues(left, right);

    /// <summary>Represents split owner/repository coordinates.</summary>
    private sealed record RepositoryCoordinates(string Owner, string Name);
}
