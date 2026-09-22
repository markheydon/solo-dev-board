using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Label Manager shell tests.
/// </summary>
[Trait("Category", "E2E")]
public sealed class LabelsTests : SoloDevBoardPageTest
{
    /// <summary>
    /// Verifies the page renders taxonomy controls before any repository data is available.
    /// </summary>
    [Fact]
    public async Task Page_RendersTaxonomyControlsBeforeRepositoryDataIsAvailable()
    {
        await Page.GotoAsync("/labels");

        await Expect(Page).ToHaveTitleAsync(new Regex("Label Manager"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Label Manager" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("label-manager-tab-strip")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("new-label-button")).ToBeDisabledAsync();
        await Expect(Page.GetByTestId("load-labels-button")).ToBeDisabledAsync();
        await Expect(Page.GetByTestId("bulk-delete-labels-button")).ToBeDisabledAsync();
    }

    /// <summary>
    /// Verifies the repository selector exposes reload from GitHub after repositories finish loading.
    /// </summary>
    [Fact]
    public async Task RepositorySelector_ExposesReloadFromGitHubAfterRepositoriesFinishLoading()
    {
        await Page.GotoAsync("/labels");

        await Expect(Page.GetByText("Loading repositories...")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("labels-reload-from-github-button")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Reload from GitHub" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies repository load failure surfaces in the label manager feedback region.
    /// </summary>
    [Fact]
    public async Task RepositoryLoadFailure_SurfacesInLabelManagerFeedbackRegion()
    {
        await Page.GotoAsync("/labels");

        await Expect(Page.GetByText("Loading repositories...")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByTestId("label-manager-feedback-region")).ToBeVisibleAsync();
        await Expect(Page.GetByText(new Regex("GitHub API request failed", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Try loading repositories again" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies label manager tabs remain available across taxonomy modes.
    /// </summary>
    [Fact]
    public async Task LabelManagerTabs_RemainAvailableAcrossTaxonomyModes()
    {
        await Page.GotoAsync("/labels");

        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Labels" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Recommended taxonomy" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Synchronise" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Tab, new() { Name = "Recommended taxonomy" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Apply recommended taxonomy" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("preview-taxonomy-button")).ToHaveTextAsync("Preview");
        await Expect(Page.GetByTestId("remove-labels-outside-taxonomy-checkbox")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("keep-area-labels-checkbox")).ToHaveCountAsync(0);

        var removeOutsideCheckbox = Page.GetByTestId("remove-labels-outside-taxonomy-checkbox");
        if (await removeOutsideCheckbox.IsEnabledAsync())
        {
            await removeOutsideCheckbox.ClickAsync();
            await Expect(Page.GetByTestId("keep-area-labels-checkbox")).ToBeVisibleAsync();
        }

        await Page.GetByRole(AriaRole.Tab, new() { Name = "Synchronise" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Synchronise labels" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("sync-keep-area-labels-checkbox")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the labels tab exposes the bulk delete control disabled until rows are selected.
    /// </summary>
    [Fact]
    public async Task LabelsTab_ExposesBulkDeleteControlDisabledUntilRowsAreSelected()
    {
        await Page.GotoAsync("/labels");

        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Labels" })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("bulk-delete-labels-button")).ToBeDisabledAsync();
        await Expect(Page.GetByTestId("bulk-delete-labels-button")).ToHaveTextAsync("Delete");
    }

    /// <summary>
    /// Verifies the recommended taxonomy remap workflow exposes wireframe test ids when preview is available.
    /// </summary>
    [Fact]
    public async Task RecommendedTaxonomyRemapWorkflow_ExposesWireframeTestIdsWhenPreviewIsAvailable()
    {
        await Page.GotoAsync("/labels");

        await Expect(Page.GetByText("Loading repositories...")).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Page.GetByRole(AriaRole.Tab, new() { Name = "Recommended taxonomy" }).ClickAsync();
        await Expect(Page.GetByTestId("preview-taxonomy-button")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("remove-labels-outside-taxonomy-checkbox")).ToBeVisibleAsync();

        var loadError = Page.GetByTestId("label-manager-error-alert");
        var repositoryAutocomplete = Page.GetByTestId("repository-autocomplete");
        await Expect(loadError.Or(repositoryAutocomplete)).ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await loadError.IsVisibleAsync())
        {
            return;
        }

        var removeOutsideCheckbox = Page.GetByTestId("remove-labels-outside-taxonomy-checkbox");
        if (await removeOutsideCheckbox.IsEnabledAsync())
        {
            await removeOutsideCheckbox.ClickAsync();
            await Expect(Page.GetByTestId("keep-area-labels-checkbox")).ToBeVisibleAsync();
        }

        await Expect(Page.GetByTestId("confirm-apply-taxonomy-button")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("labels-recommended-remap-apply-button")).ToHaveCountAsync(0);

        await Page.GetByTestId("preview-taxonomy-button").ClickAsync();

        var remapTable = Page.GetByTestId("labels-recommended-remap-table");
        var confirmApplyButton = Page.GetByTestId("confirm-apply-taxonomy-button");
        await Expect(remapTable.Or(confirmApplyButton)).ToBeVisibleAsync(new() { Timeout = 15_000 });

        if (await remapTable.IsVisibleAsync())
        {
            await Expect(Page.GetByTestId("labels-recommended-remap-apply-button")).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Apply remap" })).ToBeVisibleAsync();
        }
    }
}
