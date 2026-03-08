namespace SoloDevBoard.Application.Services;

/// <summary>Provides label management operations.</summary>
public interface ILabelManagerService
{
    /// <summary>Retrieves labels from the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of labels for the repository.</returns>
    Task<IReadOnlyList<LabelDto>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Synchronises labels from a source repository to a target repository.</summary>
    /// <param name="sourceOwner">The GitHub account owner login for the source repository.</param>
    /// <param name="sourceRepo">The source repository name.</param>
    /// <param name="targetOwner">The GitHub account owner login for the target repository.</param>
    /// <param name="targetRepo">The target repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous synchronisation operation.</returns>
    Task SyncLabelsAsync(string sourceOwner, string sourceRepo, string targetOwner, string targetRepo, CancellationToken cancellationToken = default);
}
