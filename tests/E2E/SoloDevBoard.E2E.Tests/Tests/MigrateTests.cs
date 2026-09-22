using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// One-Click Migration shell tests.
/// </summary>
[Trait("Category", "E2E")]
public sealed class MigrateTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies migration setup exposes reload from GitHub after repositories finish loading.
    /// </summary>
    [Fact]
    public async Task MigrationSetup_ExposesReloadFromGitHubAfterRepositoriesFinishLoading()
    {
        await Page.GotoAsync("/migrate");

        await Expect(Page.GetByTestId("migration-repositories-load-error")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("migration-reload-from-github-button")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Reload from GitHub" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the migration setup shell renders with preview locked before migration starts.
    /// </summary>
    [Fact]
    public async Task MigrationSetupShell_RendersWithPreviewLockedBeforeMigrationStarts()
    {
        await Page.GotoAsync("/migrate");

        await Expect(Page).ToHaveTitleAsync(new Regex("One-Click Migration"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "One-Click Migration" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("migration-workflow-controls-card")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("migration-workflow-controls-heading")).ToHaveTextAsync("Migration setup");
        await Expect(Page.GetByTestId("migration-preview-empty-state")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var loadError = Page.GetByTestId("migration-repositories-load-error");
        var columnsSwitch = Page.GetByTestId("migration-scope-columns-switch");
        await Expect(loadError.Or(columnsSwitch)).ToBeVisibleAsync(new() { Timeout = 30_000 });

        if (await loadError.IsVisibleAsync())
        {
            await Expect(Page.GetByText(new Regex("GitHub API request failed while loading repositories", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("try loading repositories again", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
            return;
        }

        await Expect(columnsSwitch).ToBeVisibleAsync();
        await Expect(Page.GetByText("Project board columns")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("migration-preview-button")).ToBeDisabledAsync();
    }

    /// <summary>
    /// Verifies repository load failure surfaces inline with retry.
    /// </summary>
    [Fact]
    public async Task RepositoryLoadFailure_SurfacesInlineWithRetry()
    {
        await Page.GotoAsync("/migrate");

        await Expect(Page.GetByTestId("migration-repositories-load-error")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByText(new Regex("GitHub API request failed while loading repositories", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("try loading repositories again", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the labels scope shows the ignore area labels control by default when repositories load.
    /// </summary>
    [Fact]
    public async Task LabelsScope_ShowsIgnoreAreaLabelsControlByDefaultWhenRepositoriesLoad()
    {
        await Page.GotoAsync("/migrate");

        await Expect(Page.GetByTestId("migration-workflow-controls-card")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var loadError = Page.GetByTestId("migration-repositories-load-error");
        var ignoreAreaCheckbox = Page.GetByTestId("migration-ignore-area-labels-checkbox");
        await Expect(loadError.Or(ignoreAreaCheckbox)).ToBeVisibleAsync(new() { Timeout = 30_000 });

        if (await loadError.IsVisibleAsync())
        {
            return;
        }

        await Expect(ignoreAreaCheckbox).ToBeVisibleAsync();
        await Expect(ignoreAreaCheckbox).ToBeCheckedAsync();
        await Expect(Page.GetByText("Ignore area/* labels")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies overwrite with labels scope shows the keep area labels control when repositories load.
    /// </summary>
    [Fact]
    public async Task OverwriteWithLabelsScope_ShowsKeepAreaLabelsControlWhenRepositoriesLoad()
    {
        await Page.GotoAsync("/migrate");

        await Expect(Page.GetByTestId("migration-workflow-controls-card")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var loadError = Page.GetByTestId("migration-repositories-load-error");
        var conflictSelect = Page.GetByTestId("migration-conflict-strategy-select");
        await Expect(loadError.Or(conflictSelect)).ToBeVisibleAsync(new() { Timeout = 30_000 });

        if (await loadError.IsVisibleAsync())
        {
            return;
        }

        await conflictSelect.ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Overwrite" }).ClickAsync();

        var keepAreaCheckbox = Page.GetByTestId("migration-keep-area-labels-checkbox");
        await Expect(keepAreaCheckbox).ToBeVisibleAsync();
        await Expect(keepAreaCheckbox).ToBeCheckedAsync();
        await Expect(Page.GetByText("Keep area/* labels")).ToBeVisibleAsync();
    }
}
