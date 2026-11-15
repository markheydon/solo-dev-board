using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="LabelValueComparer"/>.</summary>
public sealed class LabelValueComparerTests
{
    [Fact]
    public void ColoursMatch_WhenHashPrefixDiffers_ReturnsTrue()
    {
        Assert.True(LabelValueComparer.ColoursMatch("d73a4a", "#D73A4A"));
    }

    [Fact]
    public void ColoursMatch_WhenValuesDiffer_ReturnsFalse()
    {
        Assert.False(LabelValueComparer.ColoursMatch("d73a4a", "ffffff"));
    }

    [Fact]
    public void HaveSameValues_WhenColourUsesHashPrefix_ReturnsTrue()
    {
        var left = new Label { Name = "type/bug", Colour = "d73a4a", Description = "A bug" };
        var right = new Label { Name = "type/bug", Colour = "#D73A4A", Description = "A bug" };

        Assert.True(LabelValueComparer.HaveSameValues(left, right));
    }

    [Fact]
    public void DescriptionsMatch_WhenBothNull_ReturnsTrue()
    {
        Assert.True(LabelValueComparer.DescriptionsMatch(null, null));
    }
}
