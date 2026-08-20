using SoloDevBoard.Application.Services.Labels;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="RecommendedLabelTaxonomyCatalog"/>.</summary>
public sealed class RecommendedLabelTaxonomyCatalogTests
{
    [Fact]
    public void TryGetLabels_WhenStrategyIdIsSoloDevBoard_ReturnsCanonicalTaxonomy()
    {
        var resolved = RecommendedLabelTaxonomyCatalog.TryGetLabels("solodevboard", out var labels);

        Assert.True(resolved);
        Assert.Same(RecommendedLabelTaxonomyCatalog.SoloDevBoard, labels);
    }

    [Fact]
    public void TryGetLabels_WhenStrategyIdUsesDifferentCasing_ReturnsCanonicalTaxonomy()
    {
        var resolved = RecommendedLabelTaxonomyCatalog.TryGetLabels("SoloDevBoard", out var labels);

        Assert.True(resolved);
        Assert.Same(RecommendedLabelTaxonomyCatalog.SoloDevBoard, labels);
    }

    [Fact]
    public void TryGetLabels_WhenStrategyIdIsUnrecognised_ReturnsFalse()
    {
        var resolved = RecommendedLabelTaxonomyCatalog.TryGetLabels("unknown", out var labels);

        Assert.False(resolved);
        Assert.Empty(labels);
    }
}
