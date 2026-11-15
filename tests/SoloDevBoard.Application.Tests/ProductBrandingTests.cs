using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="ProductBranding"/>.</summary>
public sealed class ProductBrandingTests
{
    [Fact]
    public void DisplayName_IsSoloDevBoardSpacedWords()
    {
        Assert.Equal("Solo Dev Board", ProductBranding.DisplayName);
    }

    [Fact]
    public void MarkPath_PointsAtWwwrootFaviconSvg()
    {
        Assert.Equal("favicon.svg", ProductBranding.MarkPath);
    }
}
