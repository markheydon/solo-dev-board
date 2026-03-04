using SoloDevBoard.Domain.Entities;
using SoloDevBoard.Infrastructure;

namespace SoloDevBoard.Application.Services;

/// <summary>Stub implementation of <see cref="ITriageService"/>.</summary>
public sealed class TriageService : ITriageService
{
    private readonly IGitHubService _gitHubService;

    public TriageService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Issue>> GetUntriagedIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default)
        => _gitHubService.GetIssuesAsync(owner, repo, cancellationToken);

    /// <inheritdoc/>
    public Task ApplyTriageLabelAsync(string owner, string repo, int issueNumber, string labelName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement applying a triage label to a specific issue.
        return Task.CompletedTask;
    }
}
