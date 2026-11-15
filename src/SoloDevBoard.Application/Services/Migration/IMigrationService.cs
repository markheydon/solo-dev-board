namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Provides repository migration operations.</summary>
public interface IMigrationService
{
    /// <summary>Builds a migration preview for one source repository and multiple target repositories.</summary>
    /// <param name="sourceRepositoryFullName">The source repository in owner/repository format.</param>
    /// <param name="targetRepositoryFullNames">The target repositories in owner/repository format.</param>
    /// <param name="scope">The migration item types to include.</param>
    /// <param name="conflictStrategy">The conflict strategy applied to existing target items.</param>
    /// <param name="boardSelection">The project board selections when <paramref name="scope"/> includes project board columns.</param>
    /// <param name="keepAreaLabels">When <see langword="true" /> and <paramref name="conflictStrategy"/> is <see cref="MigrationConflictStrategy.Overwrite"/>, labels with the <c>area/</c> prefix on targets are kept instead of deleted.</param>
    /// <param name="ignoreAreaLabels">When <see langword="true" />, source labels with the <c>area/</c> prefix are excluded from create and update operations.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A preview describing label, milestone, and Status column changes for each target repository.</returns>
    Task<MigrationPreviewDto> PreviewMigrationAsync(
        string sourceRepositoryFullName,
        IReadOnlyList<string> targetRepositoryFullNames,
        MigrationScopeDto scope,
        MigrationConflictStrategy conflictStrategy,
        MigrationBoardSelectionDto? boardSelection = null,
        bool keepAreaLabels = true,
        bool ignoreAreaLabels = true,
        CancellationToken cancellationToken = default);

    /// <summary>Applies migration for one source repository to multiple target repositories.</summary>
    /// <param name="sourceRepositoryFullName">The source repository in owner/repository format.</param>
    /// <param name="targetRepositoryFullNames">The target repositories in owner/repository format.</param>
    /// <param name="scope">The migration item types to include.</param>
    /// <param name="conflictStrategy">The conflict strategy applied to existing target items.</param>
    /// <param name="boardSelection">The project board selections when <paramref name="scope"/> includes project board columns.</param>
    /// <param name="keepAreaLabels">When <see langword="true" /> and <paramref name="conflictStrategy"/> is <see cref="MigrationConflictStrategy.Overwrite"/>, labels with the <c>area/</c> prefix on targets are kept instead of deleted.</param>
    /// <param name="ignoreAreaLabels">When <see langword="true" />, source labels with the <c>area/</c> prefix are excluded from create and update operations.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Per-repository results for label, milestone, and Status column migration.</returns>
    Task<MigrationResultDto> ApplyMigrationAsync(
        string sourceRepositoryFullName,
        IReadOnlyList<string> targetRepositoryFullNames,
        MigrationScopeDto scope,
        MigrationConflictStrategy conflictStrategy,
        MigrationBoardSelectionDto? boardSelection = null,
        bool keepAreaLabels = true,
        bool ignoreAreaLabels = true,
        CancellationToken cancellationToken = default);

    /// <summary>Discovers supported project boards for column migration on a repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Supported boards and linked-project visibility metadata.</returns>
    Task<MigrationProjectBoardDiscoveryDto> GetProjectBoardOptionsAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default);
}
