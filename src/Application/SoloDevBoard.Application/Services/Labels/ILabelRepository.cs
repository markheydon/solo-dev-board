using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Provides repository operations for managing GitHub labels.</summary>
public interface ILabelRepository
{
    /// <summary>Retrieves all labels for the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of labels for the repository.</returns>
    Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Creates a new label in the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="label">The label details to create.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The created label.</returns>
    Task<Label> CreateLabelAsync(string owner, string repo, Label label, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing label in the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="labelName">The current label name to update.</param>
    /// <param name="label">The new label details to apply.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The updated label.</returns>
    Task<Label> UpdateLabelAsync(string owner, string repo, string labelName, Label label, CancellationToken cancellationToken = default);

    /// <summary>Deletes a label from the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="labelName">The label name to delete.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteLabelAsync(string owner, string repo, string labelName, CancellationToken cancellationToken = default);
}
