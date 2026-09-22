using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Authentication entry tests for PAT mode.
/// </summary>
[Trait("Category", "E2E")]
public sealed class AuthEntryTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the PAT connectivity error page returns token-rejected status and guidance.
    /// </summary>
    [Fact]
    public async Task PatConnectivityErrorPage_ReturnsTokenRejectedStatusAndGuidance()
    {
        Skip.If(E2eAuthMode.IsHosted, "PAT mode only");

        var response = await Page.GotoAsync("/auth/connectivity-error?reason=token-rejected");

        Assert.Equal(401, response?.Status);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "GitHub connection problem" })).ToBeVisibleAsync();
        await Expect(Page.GetByText(new Regex("personal access token", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("pat-connectivity-return-home")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the welcome page redirects to home in PAT mode.
    /// </summary>
    [Fact]
    public async Task WelcomePage_RedirectsToHomeInPatMode()
    {
        Skip.If(E2eAuthMode.IsHosted, "PAT mode only");

        await Page.GotoAsync("/welcome");

        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
        await Expect(Page).ToHaveTitleAsync(new Regex("Home — SoloDevBoard"));
    }
}
