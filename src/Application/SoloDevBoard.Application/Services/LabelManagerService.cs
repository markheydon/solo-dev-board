using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Stub implementation of <see cref="ILabelManagerService"/>.</summary>
public sealed class LabelManagerService : ILabelManagerService
{
    private readonly IGitHubService _gitHubService;

    /// <summary>Initialises a new instance of the <see cref="LabelManagerService"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used to retrieve label data.</param>
    public LabelManagerService(IGitHubService gitHubService)
    {
        ArgumentNullException.ThrowIfNull(gitHubService);
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelDto>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        var labels = await _gitHubService.GetLabelsAsync(owner, repo, cancellationToken).ConfigureAwait(false);
        return labels.Select(label => MapToDto(label, repo)).ToArray();
    }

    /// <inheritdoc/>
    public Task SyncLabelsAsync(string sourceOwner, string sourceRepo, string targetOwner, string targetRepo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement label synchronisation between repositories.
        return Task.CompletedTask;
    }

    private static LabelDto MapToDto(Label label, string repositoryName)
        => new(label.Name, label.Colour, label.Description, repositoryName);
}
