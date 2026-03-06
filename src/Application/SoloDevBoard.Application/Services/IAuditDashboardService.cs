using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Provides audit dashboard operations.</summary>
public interface IAuditDashboardService
{
    /// <summary>Retrieves a summary of repositories owned by the specified account.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of repositories for the specified owner.</returns>
    Task<IReadOnlyList<Repository>> GetRepositorySummaryAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>Retrieves open issues for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of open issues for the repository.</returns>
    Task<IReadOnlyList<Issue>> GetOpenIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);
}
