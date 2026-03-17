namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Provides label management operations.</summary>
public interface ILabelManagerService
{
    /// <summary>Retrieves labels from the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of labels for the repository.</returns>
    Task<IReadOnlyList<LabelDto>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves labels across the specified repositories and merges the results.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repositories">The repository names to retrieve labels from.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of labels from the specified repositories.</returns>
    Task<IReadOnlyList<LabelDto>> GetLabelsForRepositoriesAsync(string owner, IReadOnlyList<string> repositories, CancellationToken cancellationToken = default);

    /// <summary>Creates a label in one or more repositories.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repositories">The repository names where the label will be created.</param>
    /// <param name="label">The label details to create.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list containing the created label result for each repository.</returns>
    Task<IReadOnlyList<LabelDto>> CreateLabelAsync(string owner, IReadOnlyList<string> repositories, LabelDto label, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing label across one or more repositories.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repositories">The repository names where the label will be updated.</param>
    /// <param name="labelName">The existing label name to update.</param>
    /// <param name="label">The updated label details.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list containing the updated label result for each repository.</returns>
    Task<IReadOnlyList<LabelDto>> UpdateLabelAsync(string owner, IReadOnlyList<string> repositories, string labelName, LabelDto label, CancellationToken cancellationToken = default);

    /// <summary>Deletes a label from one or more repositories.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repositories">The repository names where the label will be deleted.</param>
    /// <param name="labelName">The label name to delete.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteLabelAsync(string owner, IReadOnlyList<string> repositories, string labelName, CancellationToken cancellationToken = default);

    /// <summary>Synchronises labels from a source repository to a target repository.</summary>
    /// <param name="sourceOwner">The GitHub account owner login for the source repository.</param>
    /// <param name="sourceRepo">The source repository name.</param>
    /// <param name="targetOwner">The GitHub account owner login for the target repository.</param>
    /// <param name="targetRepo">The target repository name.</param>
    /// <param name="applyChanges"><see langword="true" /> to apply the synchronisation; otherwise, <see langword="false" /> to return a preview only.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A preview of labels to add, update, and delete for the target repository.</returns>
    Task<LabelSyncPreviewDto> SyncLabelsAsync(string sourceOwner, string sourceRepo, string targetOwner, string targetRepo, bool applyChanges = false, CancellationToken cancellationToken = default);

    /// <summary>Builds a synchronisation preview for one source repository and multiple target repositories.</summary>
    /// <param name="sourceRepositoryFullName">The source repository in owner/repository format.</param>
    /// <param name="targetRepositoryFullNames">The target repositories in owner/repository format.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-target synchronisation previews.</returns>
    Task<IReadOnlyList<LabelSyncRepositoryPreviewDto>> PreviewLabelSynchronisationAsync(string sourceRepositoryFullName, IReadOnlyList<string> targetRepositoryFullNames, CancellationToken cancellationToken = default);

    /// <summary>Applies synchronisation for one source repository to multiple target repositories.</summary>
    /// <param name="sourceRepositoryFullName">The source repository in owner/repository format.</param>
    /// <param name="targetRepositoryFullNames">The target repositories in owner/repository format.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-target synchronisation results.</returns>
    Task<IReadOnlyList<LabelSyncRepositoryResultDto>> ApplyLabelSynchronisationAsync(string sourceRepositoryFullName, IReadOnlyList<string> targetRepositoryFullNames, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the recommended SoloDevBoard label taxonomy.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of recommended taxonomy labels.</returns>
    Task<IReadOnlyList<LabelDto>> GetRecommendedTaxonomyAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves available built-in recommended label strategies.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of recommended strategy descriptors.</returns>
    Task<IReadOnlyList<RecommendedLabelStrategyDto>> GetRecommendedLabelStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Builds a preview for applying a recommended taxonomy to repositories.</summary>
    /// <param name="strategyId">The recommended strategy identifier.</param>
    /// <param name="repositories">The target repositories in owner/repository format.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of repository previews showing create, update, and skip actions.</returns>
    Task<IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto>> PreviewRecommendedTaxonomyAsync(string strategyId, IReadOnlyList<string> repositories, CancellationToken cancellationToken = default);

    /// <summary>Applies a recommended taxonomy to repositories and returns per-repository summaries.</summary>
    /// <param name="strategyId">The recommended strategy identifier.</param>
    /// <param name="repositories">The target repositories in owner/repository format.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-repository results.</returns>
    Task<IReadOnlyList<RecommendedTaxonomyRepositoryResultDto>> ApplyRecommendedTaxonomyAsync(string strategyId, IReadOnlyList<string> repositories, CancellationToken cancellationToken = default);
}
