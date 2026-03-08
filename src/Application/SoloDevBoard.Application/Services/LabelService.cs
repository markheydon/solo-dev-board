using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Default implementation of <see cref="ILabelManagerService"/>.</summary>
public sealed class LabelService : ILabelManagerService
{
    private static readonly IReadOnlyList<LabelDto> RecommendedTaxonomy =
    [
        new("type/epic", "6f42c1", "A high-level grouping of related features (spans a full phase)", string.Empty),
        new("type/feature", "0075ca", "A Feature - groups related stories within an epic", string.Empty),
        new("type/story", "1d76db", "A user-facing Story delivering a discrete piece of value", string.Empty),
        new("type/enabler", "e4e669", "An Enabler - technical prerequisite that unblocks stories", string.Empty),
        new("type/test", "bfd4f2", "A Test issue - test coverage deliverable (unit, component, integration)", string.Empty),
        new("type/bug", "d73a4a", "A bug or unexpected behaviour", string.Empty),
        new("type/chore", "fef2c0", "Maintenance, dependency updates, or technical debt", string.Empty),
        new("type/documentation", "0052cc", "Documentation additions or improvements", string.Empty),

        new("priority/critical", "b60205", "Blocking - must be resolved immediately", string.Empty),
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

        new("size/xs", "dde8c9", "Trivial - less than 1 hour (e.g. typo fix, config change)", string.Empty),
        new("size/s", "c5def5", "Small - less than half a day", string.Empty),
        new("size/m", "fef2c0", "Medium - half a day to one day", string.Empty),
        new("size/l", "f9d0c4", "Large - two to three days", string.Empty),
        new("size/xl", "d4c5f9", "Extra-large - more than three days; consider splitting", string.Empty),
    ];

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

        return createdLabels;
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

        return updatedLabels;
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

        var sourceByName = sourceLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);
        var targetByName = targetLabels.ToDictionary(label => label.Name, StringComparer.OrdinalIgnoreCase);

        var toAdd = sourceLabels
            .Where(source => !targetByName.ContainsKey(source.Name))
            .Select(source => MapToDto(source, targetRepo))
            .ToArray();

        var toUpdate = sourceLabels
            .Where(source => targetByName.TryGetValue(source.Name, out var target) && !HasSameValues(source, target))
            .Select(source => MapToDto(source, targetRepo))
            .ToArray();

        var toDelete = targetLabels
            .Where(target => !sourceByName.ContainsKey(target.Name))
            .Select(target => target.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (applyChanges)
        {
            foreach (var label in toAdd)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _labelRepository.CreateLabelAsync(targetOwner, targetRepo, MapToDomain(label, targetRepo), cancellationToken).ConfigureAwait(false);
            }

            foreach (var label in toUpdate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _labelRepository.UpdateLabelAsync(targetOwner, targetRepo, label.Name, MapToDomain(label, targetRepo), cancellationToken).ConfigureAwait(false);
            }

            foreach (var labelName in toDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _labelRepository.DeleteLabelAsync(targetOwner, targetRepo, labelName, cancellationToken).ConfigureAwait(false);
            }
        }

        return new LabelSyncPreviewDto(toAdd, toUpdate, toDelete);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LabelDto>> GetRecommendedTaxonomyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<LabelDto>>(RecommendedTaxonomy.ToArray());
    }

    /// <summary>Maps a domain label record to the application DTO shape.</summary>
    /// <param name="label">The domain label to map.</param>
    /// <param name="repositoryName">The repository name associated with the label.</param>
    /// <returns>A mapped application label DTO.</returns>
    private static LabelDto MapToDto(Label label, string repositoryName)
        => new(label.Name, label.Colour, label.Description, repositoryName);

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

    /// <summary>Determines whether two labels have equivalent values for synchronisation purposes.</summary>
    /// <param name="left">The first label to compare.</param>
    /// <param name="right">The second label to compare.</param>
    /// <returns><see langword="true" /> if labels are equivalent; otherwise, <see langword="false" />.</returns>
    private static bool HasSameValues(Label left, Label right)
        => string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Colour, right.Colour, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Description, right.Description, StringComparison.Ordinal);
}
