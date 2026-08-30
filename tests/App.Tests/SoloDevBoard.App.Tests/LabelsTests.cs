using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.Labels.Dialogs;
using SoloDevBoard.App.Components.Features.Labels.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Labels"/> page.</summary>
public sealed class LabelsTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly ILabelManagerService _labelManagerService = Substitute.For<ILabelManagerService>();
    private IRenderedComponent<MudSnackbarProvider> _snackbarProvider = default!;

    [Fact]
    public async Task Labels_WhileRepositoryServiceIsLoading_ShowsLoadingState()
    {
        // Arrange
        var repositoriesTask = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositoriesTask.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        Assert.Contains("Loading repositories", cut.Markup);
    }

    [Fact]
    public async Task Labels_InitialLoad_DoesNotFetchLabelsUntilRequested()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Load selected repositories", cut.Markup);
            Assert.Contains("New label", cut.Markup);
            Assert.Contains("Showing 1 active repository", cut.Markup);
            Assert.Empty(cut.FindAll("[data-testid='label-filter']"));
            Assert.Empty(cut.FindAll("[data-testid='labels-grid']"));
        });

        await _labelManagerService.DidNotReceive().GetLabelsForRepositoriesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_PageLayout_RendersRepositorySelectorBeforeTabStrip()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='repository-selector-region']"));
            Assert.Single(cut.FindAll("[data-testid='label-manager-tab-strip']"));

            var markup = cut.Markup;
            var selectorPosition = markup.IndexOf("repository-selector-region", StringComparison.Ordinal);
            var tabStripPosition = markup.IndexOf("label-manager-tab-strip", StringComparison.Ordinal);
            Assert.True(selectorPosition >= 0);
            Assert.True(tabStripPosition > selectorPosition);
        });
    }

    [Fact]
    public async Task Labels_WhenSwitchingToRecommendedTaxonomyTab_ShowsTaxonomyActionStrip()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await ActivateTabAsync(cut, "Recommended taxonomy");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='taxonomy-action-strip']"));
            Assert.Empty(cut.FindAll("[data-testid='sync-action-strip']"));
        });
    }

    [Fact]
    public async Task Labels_RepositoriesLoaded_ArchivedRepositoriesAreHiddenByDefault()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a", isArchived: false),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 1 active repository", cut.Markup);
            Assert.Contains("Archived repositories are hidden by default", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_LoadRequestedAndNoLabelsReturned_ShowsEmptyState()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No labels found", cut.Markup);
            Assert.Contains("No labels were returned for the selected repositories", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_SelectedRepositoriesAcrossOwners_LoadsEachOwnerAndShowsGapAnalysis()
    {
        // Arrange
        var repoA = CreateRepository("owner-a", "repo-a");
        var repoB = CreateRepository("owner-b", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA, repoB]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner-a", Arg.Is<IReadOnlyList<string>>(repositories => repositories!.SequenceEqual(new[] { "repo-a" })), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "repo-a"),
                new LabelDto("priority/high", "d93f0b", "High priority", "repo-a"),
            ]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner-b", Arg.Is<IReadOnlyList<string>>(repositories => repositories!.SequenceEqual(new[] { "repo-b" })), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("priority/high", "d93f0b", "High priority", "repo-b"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA, repoB);
        cut.Find("[data-testid='load-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("type/story", cut.Markup);
            Assert.Contains("Story label", cut.Markup);
            Assert.Contains("owner-a/repo-a", cut.Markup);
            Assert.Contains("owner-b/repo-b", cut.Markup);
        });

        await _labelManagerService.Received(1).GetLabelsForRepositoriesAsync("owner-a", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());

        await _labelManagerService.Received(1).GetLabelsForRepositoriesAsync("owner-b", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_FilterAppliedAfterLoad_FiltersRowsByName()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "repo-a"),
                new LabelDto("status/done", "cfd3d7", "Completed", "repo-a"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();

        cut.WaitForAssertion(() => Assert.Contains("type/story", cut.Markup));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='label-filter']")));
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll("[data-testid='edit-label-button']").Count);
            Assert.Equal(2, cut.FindAll("[data-testid='delete-label-button']").Count);
        });

        // MudTextField forwards data-testid directly onto the <input> element.
        var filterTextInput = cut.Find("[data-testid='label-filter']");

        // Act
        filterTextInput.Input("status");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("status/done", cut.Markup);
            Assert.DoesNotContain("type/story", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_RepositoryLoadFails_ShowsRepositorySpecificErrorAndAction()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<RepositoryDto>>(new HttpRequestException("Service unavailable")));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load repositories", cut.Markup);
            Assert.Contains("Try loading repositories again", cut.Markup);
            Assert.DoesNotContain("Try loading labels again", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_PreviewRecommendedTaxonomy_WhenSelected_ShowsPreviewCard()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-a")],
                    [new LabelDto("priority/high", "d93f0b", "High", "owner/repo-a")],
                    [],
                    [new LabelDto("status/todo", "ffffff", "Ready", "owner/repo-a")],
                    []),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");
        cut.Find("[data-testid='preview-taxonomy-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Taxonomy preview", cut.Markup);
            Assert.Contains("owner/repo-a", cut.Markup);
            Assert.Contains("Labels to create", cut.Markup);
            Assert.Contains("Labels to update", cut.Markup);
            Assert.Contains("aria-label=\"Confirm apply taxonomy\"", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_ApplyRecommendedTaxonomy_WhenConfirmed_ShowsSummary()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-a")],
                    [],
                    [],
                    [],
                    []),
            ]);

        _labelManagerService.ApplyRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryResultDto("owner/repo-a", 1, 0, 0, 0, [], null),
            ]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");
        cut.Find("[data-testid='preview-taxonomy-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='confirm-apply-taxonomy-button']"));

        cut.Find("[data-testid='confirm-apply-taxonomy-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Apply summary", cut.Markup);
            Assert.Contains("Created: 1, Updated: 0, Deleted: 0, Skipped: 0", cut.Markup);
        });
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Applied taxonomy successfully. Created 1, updated 0, deleted 0, skipped 0."));

        await _labelManagerService.Received(1).ApplyRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), false, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_ApplyRecommendedTaxonomy_WhenConfirmed_ShowsInProgressStateUntilApplyCompletes()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var applyTask = new TaskCompletionSource<IReadOnlyList<RecommendedTaxonomyRepositoryResultDto>>();

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-a")],
                    [],
                    [],
                    [],
                    []),
            ]);

        _labelManagerService.ApplyRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(applyTask.Task);
        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");
        cut.Find("[data-testid='preview-taxonomy-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='confirm-apply-taxonomy-button']"));

        cut.Find("[data-testid='confirm-apply-taxonomy-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='taxonomy-progress-indicator']"));
            Assert.Contains("Applying taxonomy changes. Duplicate submissions are disabled.", cut.Markup);

            var confirmButton = cut.Find("[data-testid='confirm-apply-taxonomy-button']");
            Assert.True(confirmButton.HasAttribute("disabled"));
            Assert.Contains("Applying...", confirmButton.TextContent);

            var cancelButton = cut.Find("[data-testid='cancel-apply-taxonomy-button']");
            Assert.True(cancelButton.HasAttribute("disabled"));
        });
        await cut.InvokeAsync(() => applyTask.SetResult([
            new RecommendedTaxonomyRepositoryResultDto("owner/repo-a", 1, 0, 0, 0, [], null),
        ]));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid='taxonomy-progress-indicator']")));
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Applied taxonomy successfully."));
    }

    [Fact]
    public async Task Labels_ApplyRecommendedTaxonomy_WhenClickedTwiceDuringPendingCall_CallsServiceOnce()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var applyTask = new TaskCompletionSource<IReadOnlyList<RecommendedTaxonomyRepositoryResultDto>>();

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-a")],
                    [],
                    [],
                    [],
                    []),
            ]);

        _labelManagerService.ApplyRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(applyTask.Task);
        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");
        cut.Find("[data-testid='preview-taxonomy-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='confirm-apply-taxonomy-button']"));

        var confirmButton = cut.Find("[data-testid='confirm-apply-taxonomy-button']");
        confirmButton.Click();
        confirmButton.Click();

        await cut.InvokeAsync(() => applyTask.SetResult([
            new RecommendedTaxonomyRepositoryResultDto("owner/repo-a", 1, 0, 0, 0, [], null),
        ]));

        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Applied taxonomy successfully."));

        // Assert
        await _labelManagerService.Received(1).ApplyRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), false, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_ApplySynchronisation_WhenConfirmed_ShowsInProgressStateUntilApplyCompletes()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");
        var applyTask = new TaskCompletionSource<IReadOnlyList<LabelSyncRepositoryResultDto>>();

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA, repoB]);

        _labelManagerService.PreviewLabelSynchronisationAsync("owner/repo-a", Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High", "owner/repo-b")],
                    [],
                    [],
                    [],
                    [],
                    []),
            ]);

        _labelManagerService.ApplyLabelSynchronisationAsync("owner/repo-a", Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(applyTask.Task);
        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA, repoB);
        await ActivateTabAsync(cut, "Synchronise");
        cut.Find("[data-testid='preview-sync-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='confirm-sync-button']"));

        cut.Find("[data-testid='confirm-sync-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='sync-progress-indicator']"));
            Assert.Contains("Applying synchronisation changes. Duplicate submissions are disabled.", cut.Markup);

            var confirmButton = cut.Find("[data-testid='confirm-sync-button']");
            Assert.True(confirmButton.HasAttribute("disabled"));
            Assert.Contains("Applying...", confirmButton.TextContent);
        });
        await cut.InvokeAsync(() => applyTask.SetResult([
            new LabelSyncRepositoryResultDto("owner/repo-b", 1, 0, 0, 0, null),
        ]));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid='sync-progress-indicator']")));
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Synchronisation completed."));
    }

    [Fact]
    public async Task Labels_InitialLoad_DefaultsToSoloDevBoardStrategy()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync("solodevboard", Arg.Any<IReadOnlyList<string>>(), false, true, Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto("owner/repo-a", [], [], [], [], []),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");
        cut.Find("[data-testid='preview-taxonomy-button']").Click();

        // Assert
        await _labelManagerService.Received(1).PreviewRecommendedTaxonomyAsync("solodevboard", Arg.Any<IReadOnlyList<string>>(), false, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_PreviewRecommendedTaxonomy_WhenClickedTwiceDuringPendingCall_CallsServiceOnce()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var previewTask = new TaskCompletionSource<IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto>>();

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(previewTask.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");

        var previewButton = cut.Find("[data-testid='preview-taxonomy-button']");
        previewButton.Click();
        previewButton.Click();

        await cut.InvokeAsync(() => previewTask.SetResult([
            new RecommendedTaxonomyRepositoryPreviewDto("owner/repo-a", [], [], [], [], []),
        ]));

        cut.WaitForAssertion(() => Assert.Contains("Taxonomy preview", cut.Markup));

        // Assert
        await _labelManagerService.Received(1).PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_RecommendedTaxonomy_RemoveOutsideTaxonomyCheckbox_IsUncheckedByDefault()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");

        // Assert
        var checkbox = cut.Find("[data-testid='remove-labels-outside-taxonomy-checkbox']");
        Assert.False(checkbox.HasAttribute("checked"));
        Assert.Contains("Remove labels outside taxonomy", cut.Markup);
    }

    [Fact]
    public async Task Labels_RecommendedTaxonomy_KeepAreaLabelsCheckbox_IsHiddenUntilRemoveOutsideIsEnabled()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='keep-area-labels-checkbox']"));

        var checkbox = cut.FindComponent<MudCheckBox<bool>>();
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(true));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='keep-area-labels-checkbox']")));
        var keepCheckbox = cut.Find("[data-testid='keep-area-labels-checkbox']");
        Assert.Contains("Keep area/* labels", cut.Markup);
        Assert.True(keepCheckbox.HasAttribute("checked"));
    }

    [Fact]
    public async Task Labels_SynchroniseTab_ShowsKeepAreaLabelsCheckboxByDefault()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA, repoB]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA, repoB);
        await ActivateTabAsync(cut, "Synchronise");

        // Assert
        var keepCheckbox = cut.Find("[data-testid='sync-keep-area-labels-checkbox']");
        Assert.Contains("Keep area/* labels", cut.Markup);
        Assert.True(keepCheckbox.HasAttribute("checked"));
    }

    [Fact]
    public async Task Labels_PreviewRecommendedTaxonomy_WhenRemoveOutsideTaxonomyEnabled_PassesStrictModeToService()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), true, true, Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [],
                    [],
                    [new LabelDto("dependencies", "0366d6", "Dependencies", "owner/repo-a")],
                    [],
                    []),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");

        var checkbox = cut.FindComponent<MudCheckBox<bool>>();
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(true));
        cut.Find("[data-testid='preview-taxonomy-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Delete: 1", cut.Markup);
            Assert.Contains("Labels to delete", cut.Markup);
            Assert.Contains("dependencies", cut.Markup);
        });

        await _labelManagerService.Received(1).PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), true, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_PreviewRecommendedTaxonomy_WhenKeepAreaLabelsEnabled_ShowsCaptionNotKeptTable()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), true, true, Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [],
                    [],
                    [],
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-a")],
                    [
                        new LabelDto("area/docs", "0052cc", "Documentation", "owner/repo-a"),
                        new LabelDto("area/labels", "c5def5", "Label Manager feature", "owner/repo-a"),
                    ]),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");

        var checkbox = cut.FindComponent<MudCheckBox<bool>>();
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(true));
        cut.Find("[data-testid='preview-taxonomy-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Delete: 0", cut.Markup);
            Assert.Contains("2 area/* labels are excluded from delete and will be left unchanged.", cut.Markup);
            Assert.DoesNotContain("Kept (area prefix)", cut.Markup);
            Assert.DoesNotContain("area/docs", cut.Markup);

            var confirmButton = cut.Find("[data-testid='confirm-apply-taxonomy-button']");
            Assert.True(confirmButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task Labels_PreviewRecommendedTaxonomy_WhenNoActionableChanges_DisablesConfirmButton()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [],
                    [],
                    [],
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-a")],
                    []),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");
        cut.Find("[data-testid='preview-taxonomy-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Taxonomy preview", cut.Markup);
            var confirmButton = cut.Find("[data-testid='confirm-apply-taxonomy-button']");
            Assert.True(confirmButton.HasAttribute("disabled"));
        });

        await _labelManagerService.DidNotReceive().ApplyRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_PreviewRecommendedTaxonomy_WhenRemoveOutsideTaxonomyDisabled_PassesFalseToService()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), false, true, Arg.Any<CancellationToken>()).Returns([
                new RecommendedTaxonomyRepositoryPreviewDto("owner/repo-a", [], [], [], [], []),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        await ActivateTabAsync(cut, "Recommended taxonomy");
        cut.Find("[data-testid='preview-taxonomy-button']").Click();

        // Assert
        await _labelManagerService.Received(1).PreviewRecommendedTaxonomyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), false, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_WhenTwoRepositoriesAreSelected_ShowsSynchronisationControls()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA, repoB]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA, repoB);
        await ActivateTabAsync(cut, "Synchronise");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Synchronise labels", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='preview-sync-button']"));
            Assert.Single(cut.FindAll("[data-testid='sync-target-list']"));
        });
    }

    [Fact]
    public async Task Labels_ApplySynchronisation_WithPartialFailure_ShowsSummary()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA, repoB]);

        _labelManagerService.PreviewLabelSynchronisationAsync("owner/repo-a", Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High", "owner/repo-b")],
                    [],
                    [],
                    [],
                    [],
                    []),
            ]);

        _labelManagerService.ApplyLabelSynchronisationAsync("owner/repo-a", Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new LabelSyncRepositoryResultDto("owner/repo-b", 1, 2, 3, 4, "GitHub API failure"),
            ]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA, repoB);
        await ActivateTabAsync(cut, "Synchronise");
        cut.Find("[data-testid='preview-sync-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='confirm-sync-button']"));
        cut.Find("[data-testid='confirm-sync-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Synchronisation summary", cut.Markup);
            Assert.Contains("GitHub API failure", cut.Markup);
        });
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "repository failures"));
    }

    [Fact]
    public async Task Labels_PreviewSynchronisation_WithSourceAndTargets_ShowsPreviewCard()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA, repoB]);

        _labelManagerService.PreviewLabelSynchronisationAsync("owner/repo-a", Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([
                new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High", "owner/repo-b")],
                    [],
                    [],
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-b")],
                    [],
                    []),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA, repoB);
        await ActivateTabAsync(cut, "Synchronise");

        cut.Find("[data-testid='preview-sync-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Synchronisation preview", cut.Markup);
            Assert.Contains("owner/repo-b", cut.Markup);
            Assert.Contains("Labels to create", cut.Markup);
            Assert.Contains("Labels to skip", cut.Markup);
            Assert.Contains("aria-label=\"Confirm synchronisation\"", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_BulkDeleteButton_WhenNoRowsSelected_IsDisabled()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var bulkDeleteButton = cut.Find("[data-testid='bulk-delete-labels-button']");
            Assert.True(bulkDeleteButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task Labels_BulkDelete_WhenConfirmed_CallsServiceAndShowsSuccess()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var dialogService = Substitute.For<IDialogService>();
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(true)));
        dialogService
            .ShowAsync<LabelBulkDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
                new LabelDto("status/done", "cfd3d7", "Completed", "owner/repo-a"),
            ]);

        _labelManagerService.BulkDeleteLabelsAsync(Arg.Any<IReadOnlyList<LabelBulkDeleteTargetDto>>(), Arg.Any<CancellationToken>()).Returns(
            new LabelBulkDeleteResultDto(1, 0, []));

        await using var ctx = CreateContext(dialogService);

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='labels-grid']")));

        var selectedRow = new Labels.LabelRow("type/story", "1d76db", "Story label", ["owner/repo-a"], []);
        var grid = cut.FindComponent<MudDataGrid<Labels.LabelRow>>();
        await cut.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Labels.LabelRow> { selectedRow }));

        cut.WaitForAssertion(() =>
        {
            var bulkDeleteButton = cut.Find("[data-testid='bulk-delete-labels-button']");
            Assert.False(bulkDeleteButton.HasAttribute("disabled"));
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='bulk-delete-labels-button']").Click());

        // Assert
        await dialogService.Received(1).ShowAsync<LabelBulkDeleteConfirmDialog>(
            Arg.Any<string>(),
            Arg.Any<DialogParameters>(),
            Arg.Any<DialogOptions>());

        await _labelManagerService.Received(1).BulkDeleteLabelsAsync(
            Arg.Is<IReadOnlyList<LabelBulkDeleteTargetDto>>(targets =>
                targets.Count == 1
                && targets[0].LabelName == "type/story"
                && targets[0].RepositoryFullNames.SequenceEqual(new[] { "owner/repo-a" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_BulkDelete_WhenAwaitingConfirmation_DoesNotShowProgressIndicator()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var dialogResultTask = new TaskCompletionSource<DialogResult?>();
        var dialogService = Substitute.For<IDialogService>();
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(dialogResultTask.Task);
        dialogService
            .ShowAsync<LabelBulkDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
            ]);

        await using var ctx = CreateContext(dialogService);

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='labels-grid']")));

        var selectedRow = new Labels.LabelRow("type/story", "1d76db", "Story label", ["owner/repo-a"], []);
        var grid = cut.FindComponent<MudDataGrid<Labels.LabelRow>>();
        await cut.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Labels.LabelRow> { selectedRow }));

        cut.Find("[data-testid='bulk-delete-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("[data-testid='bulk-delete-progress-indicator']"));
            Assert.True(cut.Find("[data-testid='bulk-delete-labels-button']").HasAttribute("disabled"));
        });

        await cut.InvokeAsync(() => dialogResultTask.SetResult(DialogResult.Cancel()));
    }

    [Fact]
    public async Task Labels_BulkDelete_ConfirmDialog_ListsSelectedLabelNamesAndRepositories()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");
        DialogParameters? capturedParameters = null;
        var dialogService = Substitute.For<IDialogService>();
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        dialogService
            .ShowAsync<LabelBulkDeleteConfirmDialog>(
                Arg.Any<string>(),
                Arg.Do<DialogParameters>(parameters => capturedParameters = parameters),
                Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA, repoB]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-b"),
            ]);

        await using var ctx = CreateContext(dialogService);

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA, repoB);
        cut.Find("[data-testid='load-labels-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='labels-grid']")));

        var selectedRow = new Labels.LabelRow("type/story", "1d76db", "Story label", ["owner/repo-a", "owner/repo-b"], []);
        var grid = cut.FindComponent<MudDataGrid<Labels.LabelRow>>();
        await cut.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Labels.LabelRow> { selectedRow }));

        cut.Find("[data-testid='bulk-delete-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() => Assert.NotNull(capturedParameters));
        var request = capturedParameters!.Get<LabelBulkDeleteConfirmDialogRequest>(nameof(LabelBulkDeleteConfirmDialog.Content));
        Assert.NotNull(request);
        var target = Assert.Single(request!.Targets);
        Assert.Equal("type/story", target.LabelName);
        Assert.Equal(["owner/repo-a", "owner/repo-b"], target.RepositoryFullNames);
    }

    [Fact]
    public async Task Labels_BulkDelete_WhenCancelled_DoesNotCallService()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var dialogService = Substitute.For<IDialogService>();
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        dialogService
            .ShowAsync<LabelBulkDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
            ]);

        await using var ctx = CreateContext(dialogService);

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='labels-grid']")));

        var selectedRow = new Labels.LabelRow("type/story", "1d76db", "Story label", ["owner/repo-a"], []);
        var grid = cut.FindComponent<MudDataGrid<Labels.LabelRow>>();
        await cut.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Labels.LabelRow> { selectedRow }));

        cut.Find("[data-testid='bulk-delete-labels-button']").Click();

        // Assert
        await dialogService.Received(1).ShowAsync<LabelBulkDeleteConfirmDialog>(
            Arg.Any<string>(),
            Arg.Any<DialogParameters>(),
            Arg.Any<DialogOptions>());

        await _labelManagerService.DidNotReceive().BulkDeleteLabelsAsync(Arg.Any<IReadOnlyList<LabelBulkDeleteTargetDto>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_BulkDelete_WhenCancelled_KeepsSelectionAndDoesNotRefreshGrid()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var dialogService = Substitute.For<IDialogService>();
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        dialogService
            .ShowAsync<LabelBulkDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
            ]);

        await using var ctx = CreateContext(dialogService);

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='labels-grid']")));

        var selectedRow = new Labels.LabelRow("type/story", "1d76db", "Story label", ["owner/repo-a"], []);
        var grid = cut.FindComponent<MudDataGrid<Labels.LabelRow>>();
        await cut.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Labels.LabelRow> { selectedRow }));

        cut.Find("[data-testid='bulk-delete-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var bulkDeleteButton = cut.Find("[data-testid='bulk-delete-labels-button']");
            Assert.False(bulkDeleteButton.HasAttribute("disabled"));

#pragma warning disable MUD0012
            var selectedItem = Assert.Single(grid.Instance.SelectedItems!);
#pragma warning restore MUD0012
            Assert.Equal("type/story", selectedItem.Name);
            Assert.Contains("type/story", cut.Find("[data-testid='labels-grid']").TextContent, StringComparison.Ordinal);
        });

        await _labelManagerService.Received(1).GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_BulkDelete_WhenConfirmed_ShowsInProgressStateUntilDeleteCompletes()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var deleteTask = new TaskCompletionSource<LabelBulkDeleteResultDto>();
        var dialogService = Substitute.For<IDialogService>();
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(true)));
        dialogService
            .ShowAsync<LabelBulkDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(
            [
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
            ],
            Array.Empty<LabelDto>());

        _labelManagerService.BulkDeleteLabelsAsync(Arg.Any<IReadOnlyList<LabelBulkDeleteTargetDto>>(), Arg.Any<CancellationToken>()).Returns(deleteTask.Task);

        await using var ctx = CreateContext(dialogService);

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='labels-grid']")));

        var selectedRow = new Labels.LabelRow("type/story", "1d76db", "Story label", ["owner/repo-a"], []);
        var grid = cut.FindComponent<MudDataGrid<Labels.LabelRow>>();
        await cut.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Labels.LabelRow> { selectedRow }));

        cut.Find("[data-testid='bulk-delete-labels-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='bulk-delete-progress-indicator']"));
            Assert.Contains("Deleting selected labels. Duplicate submissions are disabled.", cut.Markup);

            var bulkDeleteButton = cut.Find("[data-testid='bulk-delete-labels-button']");
            Assert.True(bulkDeleteButton.HasAttribute("disabled"));
        });

        await cut.InvokeAsync(() => deleteTask.SetResult(new LabelBulkDeleteResultDto(1, 0, [])));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid='bulk-delete-progress-indicator']")));

        _snackbarProvider.WaitForAssertion(() =>
        {
            var snackbar = _snackbarProvider.Find(".mud-snackbar");
            Assert.Contains("Deleted 1 label-repository pair.", snackbar.TextContent, StringComparison.Ordinal);
        });

        await _labelManagerService.Received(2).GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_BulkDelete_WhenDeleteButtonClickedTwiceDuringPendingCall_CallsServiceOnce()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var deleteTask = new TaskCompletionSource<LabelBulkDeleteResultDto>();
        var dialogService = Substitute.For<IDialogService>();
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(true)));
        dialogService
            .ShowAsync<LabelBulkDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(
            [
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
            ],
            Array.Empty<LabelDto>());

        _labelManagerService.BulkDeleteLabelsAsync(Arg.Any<IReadOnlyList<LabelBulkDeleteTargetDto>>(), Arg.Any<CancellationToken>()).Returns(deleteTask.Task);

        await using var ctx = CreateContext(dialogService);

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='labels-grid']")));

        var selectedRow = new Labels.LabelRow("type/story", "1d76db", "Story label", ["owner/repo-a"], []);
        var grid = cut.FindComponent<MudDataGrid<Labels.LabelRow>>();
        await cut.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Labels.LabelRow> { selectedRow }));

        var bulkDeleteButton = cut.Find("[data-testid='bulk-delete-labels-button']");
        bulkDeleteButton.Click();
        bulkDeleteButton.Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='bulk-delete-progress-indicator']")));

        await cut.InvokeAsync(() => deleteTask.SetResult(new LabelBulkDeleteResultDto(1, 0, [])));

        // Assert
        await dialogService.Received(1).ShowAsync<LabelBulkDeleteConfirmDialog>(
            Arg.Any<string>(),
            Arg.Any<DialogParameters>(),
            Arg.Any<DialogOptions>());

        await _labelManagerService.Received(1).BulkDeleteLabelsAsync(
            Arg.Any<IReadOnlyList<LabelBulkDeleteTargetDto>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Labels_BulkDelete_WhenSkippedPairsExist_ShowsSkippedCountInSummary()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var dialogService = Substitute.For<IDialogService>();
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(true)));
        dialogService
            .ShowAsync<LabelBulkDeleteConfirmDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(dialogReference);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repoA]);

        _labelManagerService.GetLabelsForRepositoriesAsync("owner", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/story", "1d76db", "Story label", "owner/repo-a"),
            ]);

        _labelManagerService.BulkDeleteLabelsAsync(Arg.Any<IReadOnlyList<LabelBulkDeleteTargetDto>>(), Arg.Any<CancellationToken>()).Returns(
            new LabelBulkDeleteResultDto(2, 1, []));

        await using var ctx = CreateContext(dialogService);

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='load-labels-button']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='labels-grid']")));

        var selectedRow = new Labels.LabelRow("type/story", "1d76db", "Story label", ["owner/repo-a"], []);
        var grid = cut.FindComponent<MudDataGrid<Labels.LabelRow>>();
        await cut.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<Labels.LabelRow> { selectedRow }));

        cut.Find("[data-testid='bulk-delete-labels-button']").Click();

        // Assert
        _snackbarProvider.WaitForAssertion(() =>
        {
            var snackbar = _snackbarProvider.Find(".mud-snackbar");
            Assert.Contains("Deleted 2 label-repository pairs (1 skipped).", snackbar.TextContent, StringComparison.Ordinal);
        });
    }

    private BunitContext CreateContext(IDialogService? dialogService = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();

        _labelManagerService.GetRecommendedLabelStrategiesAsync(Arg.Any<CancellationToken>()).Returns([
                new RecommendedLabelStrategyDto("solodevboard", "SoloDevBoard", "SoloDevBoard canonical taxonomy"),
                new RecommendedLabelStrategyDto("github-default", "GitHub default", "GitHub default labels"),
            ]);

        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _labelManagerService);

        if (dialogService is not null)
        {
            ctx.Services.AddSingleton(dialogService);
        }

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        _snackbarProvider = ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static async Task SelectRepositoriesAsync(IRenderedComponent<Labels> cut, params RepositoryDto[] repositories)
    {
        var selector = cut.FindComponent<RepositorySelector>();
        var selectedFullNames = repositories.Select(repository => repository.FullName).ToArray();

        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(selectedFullNames));
    }

    private static async Task ActivateTabAsync(IRenderedComponent<Labels> cut, string tabText)
    {
        var tabButton = cut
            .FindAll("[role='tab']")
            .First(button => button.TextContent.Trim().Equals(tabText, StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => tabButton.Click());
    }

    private static RepositoryDto CreateRepository(string owner, string name, bool isPrivate = false, bool isArchived = false)
        => new(
            Id: 0,
            Name: name,
            FullName: $"{owner}/{name}",
            Description: string.Empty,
            Url: string.Empty,
            IsPrivate: isPrivate,
            IsArchived: isArchived,
            CreatedAt: DateTimeOffset.UnixEpoch,
            UpdatedAt: DateTimeOffset.UnixEpoch,
            Topics: [],
            IsOpenSource: false);
}
