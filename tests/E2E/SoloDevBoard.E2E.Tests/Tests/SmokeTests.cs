using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Smoke tests for health and home page rendering.
/// </summary>
[Trait("Category", "E2E")]
public sealed class SmokeTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the health endpoint returns a healthy response.
    /// </summary>
    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await Page.APIRequest.GetAsync("/health");
        Assert.True(response.Ok);
        Assert.Contains("Healthy", await response.TextAsync());
    }

    /// <summary>
    /// Verifies the home page renders dashboard navigation.
    /// </summary>
    [Fact]
    public async Task HomePage_RendersDashboardNavigation()
    {
        await Page.GotoAsync("/");
        await Expect(Page).ToHaveTitleAsync(new Regex("Home — SoloDevBoard"));
        await Expect(Page.GetByRole(AriaRole.Navigation)).ToBeVisibleAsync();
    }
}
