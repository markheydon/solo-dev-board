using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Provides issue triage operations.</summary>
public interface ITriageService
{
    /// <summary>Retrieves issues that still require triage in the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of issues that require triage.</returns>
    Task<IReadOnlyList<Issue>> GetUntriagedIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Applies a triage label to an issue.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="issueNumber">The repository-scoped issue number.</param>
    /// <param name="labelName">The label name to apply.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous label application operation.</returns>
    Task ApplyTriageLabelAsync(string owner, string repo, int issueNumber, string labelName, CancellationToken cancellationToken = default);
}
