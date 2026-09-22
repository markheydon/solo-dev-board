using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Repositories shell tests.
/// </summary>
[Trait("Category", "E2E")]
public sealed class RepositoriesTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the command strip and repository load failure are visible without a live GitHub connection.
    /// </summary>
    [Fact]
    public async Task CommandStripAndRepositoryLoadFailure_AreVisibleWithoutLiveGitHubConnection()
    {
        await Page.GotoAsync("/repositories");

        await Expect(Page).ToHaveTitleAsync(new Regex("Repositories"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Repositories", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("repositories-reload-from-github-button")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Search repositories")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("repositories-catalogue-filter")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("repositories-catalogue-filter-all")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("repositories-catalogue-filter-open-source")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("repositories-catalogue-filter-not-open-source")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("repositories-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("repositories-error-state")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unable to load repositories")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Try again" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies a phone-width viewport does not introduce horizontal overflow on the repositories shell.
    /// </summary>
    [Fact]
    public async Task PhoneWidthViewport_DoesNotIntroduceHorizontalOverflowOnRepositoriesShell()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync("/repositories");

        await Expect(Page.GetByTestId("repositories-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("repositories-error-state")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Actions" })).ToBeVisibleAsync();

        var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

        Assert.False(hasHorizontalOverflow);
    }
}
