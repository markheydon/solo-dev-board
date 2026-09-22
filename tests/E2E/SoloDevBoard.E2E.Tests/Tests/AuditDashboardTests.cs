using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Audit Dashboard shell tests.
/// </summary>
[Trait("Category", "E2E")]
public sealed class AuditDashboardTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the repository selector exposes reload from GitHub after repositories finish loading.
    /// </summary>
    [Fact]
    public async Task RepositorySelector_ExposesReloadFromGitHubAfterRepositoriesFinishLoading()
    {
        await Page.GotoAsync("/audit-dashboard");

        await Expect(Page.GetByTestId("audit-command-surface")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("audit-reload-from-github-button")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Reload from GitHub" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the page surfaces repository load failure without a live GitHub connection.
    /// </summary>
    [Fact]
    public async Task Page_SurfacesRepositoryLoadFailureWithoutLiveGitHubConnection()
    {
        await Page.GotoAsync("/audit-dashboard");

        await Expect(Page).ToHaveTitleAsync(new Regex("Audit Dashboard"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Audit Dashboard" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("audit-feedback-region")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByText("Unable to load repositories")).ToBeVisibleAsync();
    }
}
