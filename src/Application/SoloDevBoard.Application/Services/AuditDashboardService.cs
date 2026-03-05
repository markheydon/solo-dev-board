using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Stub implementation of <see cref="IAuditDashboardService"/>.</summary>
public sealed class AuditDashboardService : IAuditDashboardService
{
    private readonly IGitHubService _gitHubService;

    public AuditDashboardService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Repository>> GetRepositorySummaryAsync(string owner, CancellationToken cancellationToken = default)
        => _gitHubService.GetRepositoriesAsync(owner, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Issue>> GetOpenIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default)
        => _gitHubService.GetIssuesAsync(owner, repo, cancellationToken);
}
