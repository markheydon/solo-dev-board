using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Narrow-viewport shell checks for shipped feature pages (milestone v1.4).
/// </summary>
[Trait("Category", "E2E")]
public sealed class NarrowViewportChromeTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies representative routes remain usable at phone width without horizontal overflow.
    /// </summary>
    [Theory]
    [InlineData("/", "Home")]
    [InlineData("/repositories", "Repositories")]
    [InlineData("/labels", "Label Manager")]
    [InlineData("/audit-dashboard", "Audit Dashboard")]
    [InlineData("/triage", "Triage UI")]
    [InlineData("/planning", "Planning")]
    [InlineData("/migrate", "One-Click Migration")]
    [InlineData("/board-rules", "Board Rules Visualiser")]
    [InlineData("/actions-templates", "Actions Templates")]
    public async Task PhoneWidth_DoesNotIntroduceHorizontalOverflow(string route, string headingName)
    {
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(route);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = headingName, Exact = true })).ToBeVisibleAsync();

        var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

        Assert.False(hasHorizontalOverflow);
    }

    /// <summary>
    /// Verifies representative routes remain usable at compact tablet width without horizontal overflow.
    /// </summary>
    [Theory]
    [InlineData("/repositories", "Repositories")]
    [InlineData("/labels", "Label Manager")]
    [InlineData("/audit-dashboard", "Audit Dashboard")]
    [InlineData("/planning", "Planning")]
    public async Task CompactTabletWidth_DoesNotIntroduceHorizontalOverflow(string route, string headingName)
    {
        await Page.SetViewportSizeAsync(744, 1024);
        await Page.GotoAsync(route);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = headingName, Exact = true })).ToBeVisibleAsync();

        var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

        Assert.False(hasHorizontalOverflow);
    }
}
