using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Provides repository operations for managing GitHub workflow files.</summary>
public interface IWorkflowFileRepository
{
    /// <summary>Retrieves a workflow file from the specified repository path.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The relative workflow file path.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The workflow file when present; otherwise, <see langword="null" />.</returns>
    Task<WorkflowFile?> GetWorkflowFileAsync(string owner, string repo, string path, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a workflow file in the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The relative workflow file path.</param>
    /// <param name="content">The YAML content to write.</param>
    /// <param name="existingSha">The existing blob SHA when updating a file; otherwise, <see langword="null" />.</param>
    /// <param name="commitMessage">The commit message used when writing the file.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task CreateOrUpdateWorkflowFileAsync(
        string owner,
        string repo,
        string path,
        string content,
        string? existingSha,
        string commitMessage,
        CancellationToken cancellationToken = default);
}
