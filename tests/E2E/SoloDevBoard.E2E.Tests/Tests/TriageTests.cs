using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Triage shell tests.
/// </summary>
[Trait("Category", "E2E")]
public sealed class TriageTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the session scope exposes reload from GitHub after repositories finish loading.
    /// </summary>
    [Fact]
    public async Task SessionScope_ExposesReloadFromGitHubAfterRepositoriesFinishLoading()
    {
        await Page.GotoAsync("/triage");

        await Expect(Page.GetByTestId("triage-repositories-load-error")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("triage-reload-from-github-button")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Reload from GitHub" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the page shows repository load failure inline without a live GitHub connection.
    /// </summary>
    [Fact]
    public async Task Page_ShowsRepositoryLoadFailureInlineWithoutLiveGitHubConnection()
    {
        await Page.GotoAsync("/triage");

        await Expect(Page).ToHaveTitleAsync(new Regex("Triage UI"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Triage UI" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("triage-not-started-region")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("triage-repositories-load-error")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByText(new Regex("GitHub API request failed while loading repositories", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("try loading repositories again", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the action surface exposes disposition controls only after a session starts.
    /// </summary>
    [Fact]
    public async Task ActionSurface_ExposesDispositionControlsOnlyAfterSessionStarts()
    {
        await Page.GotoAsync("/triage");

        await Expect(Page.GetByTestId("triage-not-started-region")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("triage-disposition-toggle")).ToHaveCountAsync(0);
    }
}
