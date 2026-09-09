using SoloDevBoard.Application.Services.Labels;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="RecommendedTaxonomyApplySummaryHelper"/>.</summary>
public sealed class RecommendedTaxonomyApplySummaryHelperTests
{
    [Fact]
    public void EnrichDeletedCounts_WhenRemapDeletedSources_AddsToRepositoryDeletedCount()
    {
        var applyResults = new[]
        {
            new RecommendedTaxonomyRepositoryResultDto("owner/repo-a", 22, 1, 0, 0, [], null),
            new RecommendedTaxonomyRepositoryResultDto("owner/repo-b", 0, 0, 0, 23, [], null),
        };

        var remapResults = new[]
        {
            new LabelRemapResultDto("repo-a", "bug", "type/bug", 1, 0, true, false, []),
            new LabelRemapResultDto("repo-a", "story", "type/story", 6, 0, true, false, []),
        };

        var enriched = RecommendedTaxonomyApplySummaryHelper.EnrichDeletedCounts(
            applyResults,
            remapResults,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        Assert.Equal(2, enriched[0].DeletedCount);
        Assert.Equal(0, enriched[1].DeletedCount);
    }

    [Fact]
    public void EnrichDeletedCounts_WhenUnusedExtrasDeleted_AddsToMatchingRepository()
    {
        var applyResults = new[]
        {
            new RecommendedTaxonomyRepositoryResultDto("owner/repo-a", 0, 0, 0, 0, [], null),
        };

        var enriched = RecommendedTaxonomyApplySummaryHelper.EnrichDeletedCounts(
            applyResults,
            [],
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["owner/repo-a"] = 3 });

        Assert.Equal(3, enriched[0].DeletedCount);
    }

    [Fact]
    public void IsDeleteWithoutRemapSuccess_WhenNoErrorsAndEmptyDestination_ReturnsTrue()
    {
        var result = new LabelRemapResultDto("repo-a", "legacy", string.Empty, 0, 0, false, false, []);

        Assert.True(RecommendedTaxonomyApplySummaryHelper.IsDeleteWithoutRemapSuccess(result));
    }
}
