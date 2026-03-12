namespace SoloDevBoard.Application.Services;

/// <summary>Provides repository migration operations.</summary>
public interface IMigrationService
{
    /// <summary>Builds a migration preview for one source repository and multiple target repositories.</summary>
    /// <param name="sourceRepositoryFullName">The source repository in owner/repository format.</param>
    /// <param name="targetRepositoryFullNames">The target repositories in owner/repository format.</param>
    /// <param name="scope">The migration item types to include.</param>
    /// <param name="conflictStrategy">The conflict strategy applied to existing target items.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A preview describing label and milestone changes for each target repository.</returns>
    Task<MigrationPreviewDto> PreviewMigrationAsync(
        string sourceRepositoryFullName,
        IReadOnlyList<string> targetRepositoryFullNames,
        MigrationScopeDto scope,
        MigrationConflictStrategy conflictStrategy,
        CancellationToken cancellationToken = default);

    /// <summary>Applies migration for one source repository to multiple target repositories.</summary>
    /// <param name="sourceRepositoryFullName">The source repository in owner/repository format.</param>
    /// <param name="targetRepositoryFullNames">The target repositories in owner/repository format.</param>
    /// <param name="scope">The migration item types to include.</param>
    /// <param name="conflictStrategy">The conflict strategy applied to existing target items.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>Per-repository results for label and milestone migration.</returns>
    Task<MigrationResultDto> ApplyMigrationAsync(
        string sourceRepositoryFullName,
        IReadOnlyList<string> targetRepositoryFullNames,
        MigrationScopeDto scope,
        MigrationConflictStrategy conflictStrategy,
        CancellationToken cancellationToken = default);
}
