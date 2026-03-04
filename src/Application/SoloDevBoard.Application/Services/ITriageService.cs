using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Provides issue triage operations.</summary>
public interface ITriageService
{
    Task<IReadOnlyList<Issue>> GetUntriagedIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task ApplyTriageLabelAsync(string owner, string repo, int issueNumber, string labelName, CancellationToken cancellationToken = default);
}
