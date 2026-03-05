using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Stub implementation of <see cref="ILabelManagerService"/>.</summary>
public sealed class LabelManagerService : ILabelManagerService
{
    private readonly IGitHubService _gitHubService;

    public LabelManagerService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default)
        => _gitHubService.GetLabelsAsync(owner, repo, cancellationToken);

    /// <inheritdoc/>
    public Task SyncLabelsAsync(string sourceOwner, string sourceRepo, string targetOwner, string targetRepo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement label synchronisation between repositories.
        return Task.CompletedTask;
    }
}
