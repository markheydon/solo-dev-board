using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// About page tests for PAT mode.
/// </summary>
[Trait("Category", "E2E")]
public sealed class AboutTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the about page shows deployment metadata from the shell menu.
    /// </summary>
    [Fact]
    public async Task AboutPage_ShowsDeploymentMetadataFromShellMenu()
    {
        Skip.If(E2eAuthMode.IsHosted, "PAT mode only");

        await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "More options" }).ClickAsync();
        var userGuide = Page.GetByRole(AriaRole.Menuitem, new() { Name = "User Guide" });
        await Expect(userGuide).ToBeVisibleAsync();
        await Expect(userGuide).ToHaveAttributeAsync("href", "https://solodevboard.com/docs/");
        await Expect(userGuide).ToHaveAttributeAsync("target", "_blank");
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "About" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/about$"));
        await Expect(Page).ToHaveTitleAsync(new Regex("About"));
        await Expect(Page.GetByTestId("about-application-name")).ToContainTextAsync("SoloDevBoard");
        await Expect(Page.GetByTestId("about-version")).Not.ToBeEmptyAsync();

        var versionText = await Page.GetByTestId("about-version").TextContentAsync();
        if (versionText?.Contains('-') == true)
        {
            await Expect(Page.GetByTestId("about-built-at")).Not.ToBeEmptyAsync();
        }

        await Expect(Page.GetByTestId("about-build")).Not.ToBeEmptyAsync();
        await Expect(Page.GetByTestId("about-dotnet-version")).ToHaveTextAsync(new Regex(@"\d+\.\d+"));
        await Expect(Page.GetByTestId("about-auth-mode")).ToContainTextAsync("PAT-only local trusted mode");
        await Expect(Page.GetByTestId("about-github-login")).ToContainTextAsync("@");
        await Expect(Page.GetByTestId("about-repository-link")).ToHaveAttributeAsync("href", new Regex("github\\.com"));
    }
}
