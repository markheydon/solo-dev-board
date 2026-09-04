using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Default implementation of <see cref="ILabelManagerService"/>.</summary>
public sealed class LabelService : ILabelManagerService
{
    private readonly ILabelRepository _labelRepository;

    /// <summary>Initialises a new instance of the <see cref="LabelService"/> class.</summary>
    /// <param name="labelRepository">The repository used to manage labels in GitHub repositories.</param>
    public LabelService(ILabelRepository labelRepository)
    {
        ArgumentNullException.ThrowIfNull(labelRepository);
        _labelRepository = labelRepository;
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
    public async Task<IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto>> PreviewRecommendedTaxonomyAsync(string strategyId, IReadOnlyList<string> repositories, bool removeLabelsOutsideTaxonomy = false, bool keepAreaLabels = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        var normalisedRepositories = NormaliseRepositories(repositories);

        var strategyLabels = ResolveRecommendedStrategyLabels(strategyId);
        var previews = new List<RecommendedTaxonomyRepositoryPreviewDto>();

        foreach (var repositoryFullName in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var repository = SplitRepositoryFullName(repositoryFullName);
            var existing = await _labelRepository.GetLabelsAsync(repository.Owner, repository.Name, cancellationToken).ConfigureAwait(false);
            previews.Add(BuildRepositoryPreview(repositoryFullName, strategyLabels, existing, removeLabelsOutsideTaxonomy, keepAreaLabels));
        }

        return previews
            .OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecommendedTaxonomyRepositoryResultDto>> ApplyRecommendedTaxonomyAsync(string strategyId, IReadOnlyList<string> repositories, bool removeLabelsOutsideTaxonomy = false, bool keepAreaLabels = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        var normalisedRepositories = NormaliseRepositories(repositories);

        var strategyLabels = ResolveRecommendedStrategyLabels(strategyId);
        var results = new List<RecommendedTaxonomyRepositoryResultDto>();

        foreach (var repositoryFullName in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var createdCount = 0;
            var updatedCount = 0;
            var deletedCount = 0;
            var deleteErrors = new List<RecommendedTaxonomyLabelDeleteErrorDto>();

            try
            {
                var repository = SplitRepositoryFullName(repositoryFullName);
                var existing = await _labelRepository.GetLabelsAsync(repository.Owner, repository.Name, cancellationToken).ConfigureAwait(false);
                var preview = BuildRepositoryPreview(repositoryFullName, strategyLabels, existing, removeLabelsOutsideTaxonomy, keepAreaLabels);

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

        return new RecommendedTaxonomyRepositoryPreviewDto(repositoryFullName, toCreate, toUpdate, toDelete, skipped, keptAreaLabels);
    }

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
