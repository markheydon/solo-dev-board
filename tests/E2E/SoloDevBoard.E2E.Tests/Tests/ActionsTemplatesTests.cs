using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Workflow template browser tests.
/// </summary>
[Trait("Category", "E2E")]
public sealed class ActionsTemplatesTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies built-in templates load and can be filtered and selected.
    /// </summary>
    [Fact]
    public async Task BuiltInTemplates_LoadAndCanBeFilteredAndSelected()
    {
        await Page.GotoAsync("/actions-templates");

        await Expect(Page.GetByTestId("actions-templates-custom-source-region")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-custom-source-field")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-custom-source-load")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-browser-region")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("actions-templates-grid")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-card-builtin:1")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Searchbox, new() { Name = "Search templates by name, category, or tags" }).FillAsync(".NET CI");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = ".NET CI" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Dependabot Auto-Merge" })).ToBeHiddenAsync();

        await Page.GetByTestId("actions-templates-select-builtin:1").ClickAsync();
        await Expect(Page.GetByTestId("actions-templates-detail-region")).Not.ToContainTextAsync("Select a template to review YAML");
        await Expect(Page.GetByTestId("actions-templates-select-builtin:1")).ToHaveTextAsync("Selected");
    }

    /// <summary>
    /// Verifies the catalogue is built-in only when the custom source is empty.
    /// </summary>
    [Fact]
    public async Task Catalogue_IsBuiltInOnlyWhenCustomSourceIsEmpty()
    {
        await Page.GotoAsync("/actions-templates");

        await Expect(Page.GetByTestId("actions-templates-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("actions-templates-custom-source-field")).ToHaveValueAsync("");
        await Expect(Page.GetByTestId("actions-templates-custom-source-load")).ToBeDisabledAsync();
        await Expect(Page.GetByTestId("actions-templates-card-builtin:1")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-card-builtin:2")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-card-builtin:3")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-source-badge-builtin:1")).ToHaveTextAsync("Built-in");
        await Expect(Page.Locator("[data-testid^=\"actions-templates-source-badge-custom:\"]")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("actions-templates-custom-source-error")).ToBeHiddenAsync();
    }

    /// <summary>
    /// Verifies an invalid custom source shows an error whilst built-in templates remain visible.
    /// </summary>
    [Fact]
    public async Task InvalidCustomSource_ShowsErrorWhilstBuiltInTemplatesRemainVisible()
    {
        await Page.GotoAsync("/actions-templates");

        await Expect(Page.GetByTestId("actions-templates-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });

        var customSourceField = Page.GetByRole(AriaRole.Textbox, new() { Name = "Source repository" });
        await customSourceField.FillAsync("invalid-source");
        await customSourceField.PressAsync("Tab");
        await Expect(Page.GetByTestId("actions-templates-custom-source-load")).ToBeEnabledAsync();
        await Page.GetByTestId("actions-templates-custom-source-load").ClickAsync();

        await Expect(Page.GetByTestId("actions-templates-custom-source-error")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("actions-templates-custom-source-error")).ToContainTextAsync("owner/repository format");
        await Expect(Page.GetByTestId("actions-templates-card-builtin:1")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-card-builtin:2")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("actions-templates-card-builtin:3")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the repository selector exposes reload from GitHub after repositories finish loading.
    /// </summary>
    [Fact]
    public async Task RepositorySelector_ExposesReloadFromGitHubAfterRepositoriesFinishLoading()
    {
        await Page.GotoAsync("/actions-templates");

        await Expect(Page.GetByTestId("actions-templates-repositories-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("actions-templates-reload-from-github-button")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Reload from GitHub" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the repository selector surfaces an error without a live GitHub connection.
    /// </summary>
    [Fact]
    public async Task RepositorySelector_SurfacesErrorWithoutLiveGitHubConnection()
    {
        await Page.GotoAsync("/actions-templates");

        await Expect(Page.GetByTestId("actions-templates-repositories-loading-state")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("actions-templates-repositories-error-state")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unable to load repositories")).ToBeVisibleAsync();
    }
}
