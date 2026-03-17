using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.Labels.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Labels"/> page.</summary>
public sealed class LabelsTests
{
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();
    private readonly Mock<ILabelManagerService> _labelManagerServiceMock = new();

    [Fact]
    public async Task Labels_WhileRepositoryServiceIsLoading_ShowsLoadingState()
    {
        // Arrange
        var repositoriesTask = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .Returns(repositoriesTask.Task);

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
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
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

        _labelManagerServiceMock.Verify(
            service => service.GetLabelsForRepositoriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Labels_RepositoriesLoaded_ArchivedRepositoriesAreHiddenByDefault()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
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

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LabelDto>());

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

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA, repoB]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner-a", It.Is<IReadOnlyList<string>>(repositories => repositories.SequenceEqual(new[] { "repo-a" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("type/story", "1d76db", "Story label", "repo-a"),
                new LabelDto("priority/high", "d93f0b", "High priority", "repo-a"),
            ]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner-b", It.Is<IReadOnlyList<string>>(repositories => repositories.SequenceEqual(new[] { "repo-b" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
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

        _labelManagerServiceMock.Verify(
            service => service.GetLabelsForRepositoriesAsync("owner-a", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _labelManagerServiceMock.Verify(
            service => service.GetLabelsForRepositoriesAsync("owner-b", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Labels_FilterAppliedAfterLoad_FiltersRowsByName()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
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
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

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

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.PreviewRecommendedTaxonomyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-a")],
                    [new LabelDto("priority/high", "d93f0b", "High", "owner/repo-a")],
                    [new LabelDto("status/todo", "ffffff", "Ready", "owner/repo-a")]),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='preview-taxonomy-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Taxonomy preview", cut.Markup);
            Assert.Contains("owner/repo-a", cut.Markup);
            Assert.Contains("Labels to create", cut.Markup);
            Assert.Contains("Labels to update", cut.Markup);
            Assert.Contains("Confirm apply taxonomy", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_ApplyRecommendedTaxonomy_WhenConfirmed_ShowsSummary()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.PreviewRecommendedTaxonomyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RecommendedTaxonomyRepositoryPreviewDto(
                    "owner/repo-a",
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-a")],
                    [],
                    []),
            ]);

        _labelManagerServiceMock
            .Setup(service => service.ApplyRecommendedTaxonomyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RecommendedTaxonomyRepositoryResultDto("owner/repo-a", 1, 0, 0, null),
            ]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='preview-taxonomy-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='confirm-apply-taxonomy-button']"));

        cut.Find("[data-testid='confirm-apply-taxonomy-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Apply summary", cut.Markup);
            Assert.Contains("Created: 1, Updated: 0, Skipped: 0", cut.Markup);
            Assert.Contains("Applied taxonomy successfully", cut.Markup);
        });

        _labelManagerServiceMock.Verify(
            service => service.ApplyRecommendedTaxonomyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Labels_InitialLoad_DefaultsToSoloDevBoardStrategy()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.PreviewRecommendedTaxonomyAsync("solodevboard", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RecommendedTaxonomyRepositoryPreviewDto("owner/repo-a", [], [], []),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);
        cut.Find("[data-testid='preview-taxonomy-button']").Click();

        // Assert
        _labelManagerServiceMock.Verify(
            service => service.PreviewRecommendedTaxonomyAsync("solodevboard", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Labels_PreviewRecommendedTaxonomy_WhenClickedTwiceDuringPendingCall_CallsServiceOnce()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var previewTask = new TaskCompletionSource<IReadOnlyList<RecommendedTaxonomyRepositoryPreviewDto>>();

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA]);

        _labelManagerServiceMock
            .Setup(service => service.PreviewRecommendedTaxonomyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(previewTask.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA);

        var previewButton = cut.Find("[data-testid='preview-taxonomy-button']");
        previewButton.Click();
        previewButton.Click();

        await cut.InvokeAsync(() => previewTask.SetResult([
            new RecommendedTaxonomyRepositoryPreviewDto("owner/repo-a", [], [], []),
        ]));

        cut.WaitForAssertion(() => Assert.Contains("Taxonomy preview", cut.Markup));

        // Assert
        _labelManagerServiceMock.Verify(
            service => service.PreviewRecommendedTaxonomyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Labels_WhenTwoRepositoriesAreSelected_ShowsSynchronisationControls()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA, repoB]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA, repoB);

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

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA, repoB]);

        _labelManagerServiceMock
            .Setup(service => service.PreviewLabelSynchronisationAsync("owner/repo-a", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelSyncRepositoryPreviewDto("owner/repo-b", [], [], [], []),
            ]);

        _labelManagerServiceMock
            .Setup(service => service.ApplyLabelSynchronisationAsync("owner/repo-a", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelSyncRepositoryResultDto("owner/repo-b", 1, 2, 3, 4, "GitHub API failure"),
            ]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsForRepositoriesAsync("owner", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LabelDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA, repoB);
        cut.Find("[data-testid='preview-sync-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='confirm-sync-button']"));
        cut.Find("[data-testid='confirm-sync-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Synchronisation summary", cut.Markup);
            Assert.Contains("GitHub API failure", cut.Markup);
            Assert.Contains("repository failures", cut.Markup);
        });
    }

    [Fact]
    public async Task Labels_PreviewSynchronisation_WithSourceAndTargets_ShowsPreviewCard()
    {
        // Arrange
        var repoA = CreateRepository("owner", "repo-a");
        var repoB = CreateRepository("owner", "repo-b");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([repoA, repoB]);

        _labelManagerServiceMock
            .Setup(service => service.PreviewLabelSynchronisationAsync("owner/repo-a", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High", "owner/repo-b")],
                    [],
                    [],
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-b")]),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Labels>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, repoA, repoB);

        cut.Find("[data-testid='preview-sync-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Synchronisation preview", cut.Markup);
            Assert.Contains("owner/repo-b", cut.Markup);
            Assert.Contains("Labels to create", cut.Markup);
            Assert.Contains("Labels to skip", cut.Markup);
            Assert.Contains("Confirm synchronisation", cut.Markup);
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        _labelManagerServiceMock
            .Setup(service => service.GetRecommendedLabelStrategiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RecommendedLabelStrategyDto("solodevboard", "SoloDevBoard", "SoloDevBoard canonical taxonomy"),
                new RecommendedLabelStrategyDto("github-default", "GitHub default", "GitHub default labels"),
            ]);

        ctx.Services.AddScoped(_ => _repositoryServiceMock.Object);
        ctx.Services.AddScoped(_ => _labelManagerServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static async Task SelectRepositoriesAsync(IRenderedComponent<Labels> cut, params RepositoryDto[] repositories)
    {
        var selector = cut.FindComponent<RepositorySelector>();
        var selectedFullNames = repositories.Select(repository => repository.FullName).ToArray();

        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(selectedFullNames));
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
            UpdatedAt: DateTimeOffset.UnixEpoch);
}
