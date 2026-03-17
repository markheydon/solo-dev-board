using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Default implementation of <see cref="ILabelManagerService"/>.</summary>
public sealed class LabelService : ILabelManagerService
{
    private static readonly IReadOnlyList<LabelDto> SoloDevBoardRecommendedTaxonomy =
    [
        new("type/epic", "6f42c1", "A high-level grouping of related features (spans a full phase)", string.Empty),
        new("type/feature", "0075ca", "A Feature — groups related stories within an epic", string.Empty),
        new("type/story", "1d76db", "A user-facing Story delivering a discrete piece of value", string.Empty),
        new("type/enabler", "e4e669", "An Enabler — technical prerequisite that unblocks stories", string.Empty),
        new("type/test", "bfd4f2", "A Test issue — test coverage deliverable (unit, component, integration)", string.Empty),
        new("type/bug", "d73a4a", "A bug or unexpected behaviour", string.Empty),
        new("type/chore", "fef2c0", "Maintenance, dependency updates, or technical debt", string.Empty),
        new("type/documentation", "0052cc", "Documentation additions or improvements", string.Empty),

        new("priority/critical", "b60205", "Blocking — must be resolved immediately", string.Empty),
        new("priority/high", "d93f0b", "Should be addressed in the current sprint or release", string.Empty),
        new("priority/medium", "fbca04", "Should be addressed soon but is not blocking", string.Empty),
        new("priority/low", "c2e0c6", "Nice to have; can be deferred", string.Empty),

        new("status/todo", "ffffff", "Ready to be worked on; not yet started", string.Empty),
        new("status/in-progress", "0e8a16", "Currently being worked on", string.Empty),
        new("status/blocked", "e11d48", "Cannot proceed; waiting on something external", string.Empty),
        new("status/in-review", "1d76db", "Pull request open; awaiting code review", string.Empty),
        new("status/done", "cfd3d7", "Completed and closed", string.Empty),

        new("area/dashboard", "bfd4f2", "Audit Dashboard feature", string.Empty),
        new("area/migration", "d4c5f9", "One-Click Migration feature", string.Empty),
        new("area/labels", "c5def5", "Label Manager feature", string.Empty),
        new("area/board-rules", "fef2c0", "Board Rules Visualiser feature", string.Empty),
        new("area/triage", "f9d0c4", "Triage UI feature", string.Empty),
        new("area/workflows", "c5def5", "Workflow Templates feature", string.Empty),
        new("area/infrastructure", "e4e669", "Azure infrastructure, CI/CD, deployment", string.Empty),
        new("area/docs", "0052cc", "Documentation, user guides, ADRs, planning docs", string.Empty),

        new("size/xs", "dde8c9", "Trivial — less than 1 hour (e.g. typo fix, config change)", string.Empty),
        new("size/s", "c5def5", "Small - less than half a day", string.Empty),
        new("size/m", "fef2c0", "Medium - half a day to one day", string.Empty),
        new("size/l", "f9d0c4", "Large - two to three days", string.Empty),
        new("size/xl", "d4c5f9", "Extra-large - more than three days; consider splitting", string.Empty),
    ];

    private static readonly IReadOnlyList<LabelDto> GitHubDefaultTaxonomy =
    [
        new("bug", "d73a4a", "Something is not working", string.Empty),
        new("documentation", "0075ca", "Improvements or additions to documentation", string.Empty),
        new("duplicate", "cfd3d7", "This issue or pull request already exists", string.Empty),
        new("enhancement", "a2eeef", "New feature or request", string.Empty),
        new("good first issue", "7057ff", "Good for newcomers", string.Empty),
        new("help wanted", "008672", "Extra attention is needed", string.Empty),
        new("invalid", "e4e669", "This does not appear to be valid", string.Empty),
        new("question", "d876e3", "Further information is requested", string.Empty),
        new("wontfix", "ffffff", "This will not be worked on", string.Empty),
    ];

    private static readonly IReadOnlyList<RecommendedLabelStrategyDto> RecommendedStrategies =
        BuildRecommendedStrategies();

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<LabelDto>> RecommendedStrategyLabelsById =
        BuildRecommendedStrategyLabelsById();

    private readonly ILabelRepository _labelRepository;

    /// <summary>Initialises a new instance of the <see cref="LabelService"/> class.</summary>
    /// <param name="labelRepository">The repository used to manage labels in GitHub repositories.</param>
    public LabelService(ILabelRepository labelRepository)
    {
        ArgumentNullException.ThrowIfNull(labelRepository);
        _labelRepository = labelRepository;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelDto>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var labels = await _labelRepository.GetLabelsAsync(owner, repo, cancellationToken).ConfigureAwait(false);
        return labels.Select(label => MapToDto(label, repo)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelDto>> GetLabelsForRepositoriesAsync(string owner, IReadOnlyList<string> repositories, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        var normalisedRepositories = NormaliseRepositories(repositories);

        var labels = new List<LabelDto>();
        foreach (var repository in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var repositoryLabels = await _labelRepository.GetLabelsAsync(owner, repository, cancellationToken).ConfigureAwait(false);
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
    public async Task<LabelSyncPreviewDto> SyncLabelsAsync(string sourceOwner, string sourceRepo, string targetOwner, string targetRepo, bool applyChanges = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRepo);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRepo);

        var sourceLabels = await _labelRepository.GetLabelsAsync(sourceOwner, sourceRepo, cancellationToken).ConfigureAwait(false);
        var targetLabels = await _labelRepository.GetLabelsAsync(targetOwner, targetRepo, cancellationToken).ConfigureAwait(false);

        var preview = BuildSyncPreview(targetOwner, targetRepo, sourceLabels, targetLabels);

        if (applyChanges)
        {
            await ApplySyncPreviewAsync(targetOwner, targetRepo, preview, cancellationToken).ConfigureAwait(false);
        }

        return preview;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelSyncRepositoryPreviewDto>> PreviewLabelSynchronisationAsync(string sourceRepositoryFullName, IReadOnlyList<string> targetRepositoryFullNames, CancellationToken cancellationToken = default)
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

            var preview = BuildSyncPreview(target.Owner, target.Name, sourceLabels, targetLabels);
            previews.Add(new LabelSyncRepositoryPreviewDto(
                targetRepositoryFullName,
                preview.ToAdd,
                preview.ToUpdate,
                preview.ToDelete,
                preview.Skipped));
        }

        return previews
            .OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelSyncRepositoryResultDto>> ApplyLabelSynchronisationAsync(string sourceRepositoryFullName, IReadOnlyList<string> targetRepositoryFullNames, CancellationToken cancellationToken = default)
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
                var preview = BuildSyncPreview(target.Owner, target.Name, sourceLabels, targetLabels);

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
        return Task.FromResult<IReadOnlyList<LabelDto>>(SoloDevBoardRecommendedTaxonomy.ToArray());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecommendedLabelStrategyDto>> GetRecommendedLabelStrategiesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<RecommendedLabelStrategyDto>>(RecommendedStrategies.ToArray());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto>> PreviewRecommendedTaxonomyAsync(string strategyId, IReadOnlyList<string> repositories, CancellationToken cancellationToken = default)
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
            previews.Add(BuildRepositoryPreview(repositoryFullName, strategyLabels, existing));
        }

        return previews
            .OrderBy(preview => preview.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecommendedTaxonomyRepositoryResultDto>> ApplyRecommendedTaxonomyAsync(string strategyId, IReadOnlyList<string> repositories, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        var normalisedRepositories = NormaliseRepositories(repositories);

        var strategyLabels = ResolveRecommendedStrategyLabels(strategyId);
        var results = new List<RecommendedTaxonomyRepositoryResultDto>();

        foreach (var repositoryFullName in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var repository = SplitRepositoryFullName(repositoryFullName);
                var existing = await _labelRepository.GetLabelsAsync(repository.Owner, repository.Name, cancellationToken).ConfigureAwait(false);
                var preview = BuildRepositoryPreview(repositoryFullName, strategyLabels, existing);

                foreach (var labelToCreate in preview.ToCreate)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _labelRepository
                        .CreateLabelAsync(repository.Owner, repository.Name, MapToDomain(labelToCreate, repository.Name), cancellationToken)
                        .ConfigureAwait(false);
                }

                foreach (var labelToUpdate in preview.ToUpdate)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _labelRepository
                        .UpdateLabelAsync(repository.Owner, repository.Name, labelToUpdate.Name, MapToDomain(labelToUpdate, repository.Name), cancellationToken)
                        .ConfigureAwait(false);
                }

                results.Add(new RecommendedTaxonomyRepositoryResultDto(
                    repositoryFullName,
                    preview.ToCreate.Count,
                    preview.ToUpdate.Count,
                    preview.Skipped.Count,
                    null));
            }
            catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
            {
                results.Add(new RecommendedTaxonomyRepositoryResultDto(
                    repositoryFullName,
                    0,
                    0,
                    0,
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
        if (RecommendedStrategyLabelsById.TryGetValue(strategyId, out var labels))
        {
            return labels;
        }

        throw new ArgumentException($"Unsupported recommended strategy '{strategyId}'.", nameof(strategyId));
    }

    /// <summary>Builds a preview for one repository against a strategy label set.</summary>
    /// <param name="repositoryFullName">The owner/repository full name.</param>
    /// <param name="strategyLabels">The strategy labels to compare against.</param>
    /// <param name="existingLabels">The labels currently present in the repository.</param>
    /// <returns>A repository preview showing create, update, and skip actions.</returns>
    private static RecommendedTaxonomyRepositoryPreviewDto BuildRepositoryPreview(string repositoryFullName, IReadOnlyList<LabelDto> strategyLabels, IReadOnlyList<Label> existingLabels)
    {
        var existingByName = existingLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);

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

        return new RecommendedTaxonomyRepositoryPreviewDto(repositoryFullName, toCreate, toUpdate, skipped);
    }

    /// <summary>Builds available recommended strategy descriptors.</summary>
    /// <returns>A read-only list of recommended strategy descriptors.</returns>
    private static IReadOnlyList<RecommendedLabelStrategyDto> BuildRecommendedStrategies()
        =>
        [
            new("solodevboard", "SoloDevBoard", "The SoloDevBoard canonical taxonomy covering type, priority, status, area, and size labels."),
            new("github-default", "GitHub default", "GitHub's default label set for new repositories."),
        ];

    /// <summary>Builds a strategy-to-label-set map keyed by strategy identifier.</summary>
    /// <returns>A read-only dictionary from strategy identifier to strategy labels.</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<LabelDto>> BuildRecommendedStrategyLabelsById()
    {
        var labelsById = new Dictionary<string, IReadOnlyList<LabelDto>>(StringComparer.OrdinalIgnoreCase)
        {
            ["solodevboard"] = SoloDevBoardRecommendedTaxonomy,
            ["github-default"] = GitHubDefaultTaxonomy,
        };

        var strategyIds = RecommendedStrategies.Select(strategy => strategy.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unresolvedIds = strategyIds
            .Where(strategyId => !labelsById.ContainsKey(strategyId))
            .ToArray();

        if (unresolvedIds.Length > 0)
        {
            throw new InvalidOperationException($"Missing label definitions for recommended strategies: {string.Join(", ", unresolvedIds)}.");
        }

        return labelsById;
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
    /// <returns>A synchronisation preview containing create, update, delete, and skip actions.</returns>
    private static LabelSyncPreviewDto BuildSyncPreview(string targetOwner, string targetRepo, IReadOnlyList<Label> sourceLabels, IReadOnlyList<Label> targetLabels)
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

        var toDelete = targetLabels
            .Where(target => !sourceByName.ContainsKey(target.Name))
            .Select(target => MapToDto(target, repositoryFullName))
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var skipped = sourceLabels
            .Where(source => targetByName.TryGetValue(source.Name, out var target) && HasSameValues(source, target))
            .Select(source => MapToDto(source, repositoryFullName))
            .OrderBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new LabelSyncPreviewDto(toAdd, toUpdate, toDelete, skipped);
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
        => string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Colour, right.Colour, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Description, right.Description, StringComparison.Ordinal);

    /// <summary>Represents split owner/repository coordinates.</summary>
    private sealed record RepositoryCoordinates(string Owner, string Name);
}
