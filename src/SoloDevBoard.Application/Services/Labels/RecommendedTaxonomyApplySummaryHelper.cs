namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Merges remap and unused-extra deletions into recommended taxonomy apply summaries.</summary>
public static class RecommendedTaxonomyApplySummaryHelper
{
    /// <summary>
    /// Adds remap and unused-extra label deletions to each repository apply result so summary counts
    /// reflect labels removed during a remap workflow.
    /// </summary>
    /// <param name="applyResults">The base apply results from create, update, and direct delete steps.</param>
    /// <param name="remapResults">The per-label remap outcomes for the apply batch.</param>
    /// <param name="unusedDeletedByRepository">Unused extra labels deleted per repository full name.</param>
    /// <returns>Apply results with merged delete counts.</returns>
    public static IReadOnlyList<RecommendedTaxonomyRepositoryResultDto> EnrichDeletedCounts(
        IReadOnlyList<RecommendedTaxonomyRepositoryResultDto> applyResults,
        IReadOnlyList<LabelRemapResultDto> remapResults,
        IReadOnlyDictionary<string, int> unusedDeletedByRepository)
    {
        ArgumentNullException.ThrowIfNull(applyResults);
        ArgumentNullException.ThrowIfNull(remapResults);
        ArgumentNullException.ThrowIfNull(unusedDeletedByRepository);

        return applyResults
            .Select(result =>
            {
                var additionalDeletedCount = CountRemapLabelDeletions(remapResults, result.RepositoryFullName)
                    + unusedDeletedByRepository.GetValueOrDefault(result.RepositoryFullName);

                return additionalDeletedCount == 0
                    ? result
                    : result with { DeletedCount = result.DeletedCount + additionalDeletedCount };
            })
            .ToArray();
    }

    /// <summary>Counts labels deleted through remap or delete-without-remap for one repository.</summary>
    /// <param name="remapResults">The remap outcomes to inspect.</param>
    /// <param name="repositoryFullName">The owner/repository full name.</param>
    /// <returns>The number of source labels deleted by remap actions.</returns>
    public static int CountRemapLabelDeletions(IReadOnlyList<LabelRemapResultDto> remapResults, string repositoryFullName)
        => remapResults.Count(result =>
            RemapResultMatchesRepository(result, repositoryFullName)
            && (result.SourceDeleted || IsDeleteWithoutRemapSuccess(result)));

    /// <summary>Merges additional per-label delete failures into repository apply results.</summary>
    /// <param name="applyResults">The apply results to update.</param>
    /// <param name="deleteErrorsByRepository">Delete failures keyed by owner/repository full name.</param>
    /// <returns>Apply results with merged delete errors.</returns>
    public static IReadOnlyList<RecommendedTaxonomyRepositoryResultDto> AppendDeleteErrors(
        IReadOnlyList<RecommendedTaxonomyRepositoryResultDto> applyResults,
        IReadOnlyDictionary<string, IReadOnlyList<RecommendedTaxonomyLabelDeleteErrorDto>> deleteErrorsByRepository)
    {
        ArgumentNullException.ThrowIfNull(applyResults);
        ArgumentNullException.ThrowIfNull(deleteErrorsByRepository);

        if (deleteErrorsByRepository.Count == 0)
        {
            return applyResults;
        }

        return applyResults
            .Select(result =>
            {
                if (!deleteErrorsByRepository.TryGetValue(result.RepositoryFullName, out var additionalErrors)
                    || additionalErrors.Count == 0)
                {
                    return result;
                }

                return result with
                {
                    DeleteErrors = result.DeleteErrors
                        .Concat(additionalErrors)
                        .ToArray(),
                };
            })
            .ToArray();
    }

    /// <summary>Determines whether a remap result represents a successful delete-without-remap action.</summary>
    /// <param name="result">The remap result to inspect.</param>
    /// <returns><see langword="true" /> when the source label was deleted without remapping.</returns>
    public static bool IsDeleteWithoutRemapSuccess(LabelRemapResultDto result)
        => !result.HasErrors
            && result.FailedItemCount == 0
            && string.IsNullOrWhiteSpace(result.DestinationLabelName)
            && !result.SourceDeleted;

    private static bool RemapResultMatchesRepository(LabelRemapResultDto remapResult, string repositoryFullName)
    {
        var slashIndex = repositoryFullName.LastIndexOf('/');
        if (slashIndex <= 0 || slashIndex >= repositoryFullName.Length - 1)
        {
            return false;
        }

        var repositoryName = repositoryFullName[(slashIndex + 1)..];
        return remapResult.RepositoryName.Equals(repositoryName, StringComparison.OrdinalIgnoreCase);
    }
}
