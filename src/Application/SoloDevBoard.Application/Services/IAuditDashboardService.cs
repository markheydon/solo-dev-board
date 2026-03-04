using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Provides audit dashboard operations.</summary>
public interface IAuditDashboardService
{
    Task<IReadOnlyList<Repository>> GetRepositorySummaryAsync(string owner, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issue>> GetOpenIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);
}
