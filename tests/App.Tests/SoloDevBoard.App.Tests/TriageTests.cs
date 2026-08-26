using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.Triage.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Triage;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Triage"/> page.</summary>
public sealed class TriageTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly ITriageService _triageService = Substitute.For<ITriageService>();
    private readonly ILabelManagerService _labelManagerService = Substitute.For<ILabelManagerService>();
    private IRenderedComponent<MudSnackbarProvider> _snackbarProvider = default!;

    public TriageTests()
    {
        _labelManagerService.GetLabelsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([
                new LabelDto("type/story", "1d76db", "A user-facing Story delivering a discrete piece of value", "repo"),
            ]);

        _triageService.GetMilestoneOptionsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TriageMilestoneOptionDto>());

        _triageService.GetProjectBoardOptionsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>())
            .Returns(new TriageProjectBoardDiscoveryDto([], 0, 0));
    }

    [Fact]
    public async Task Triage_RepositoriesAreLoading_ShowsLoadingStateUntilRepositoryDataArrives()
    {
        // Arrange
        var repositoryTaskSource = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositoryTaskSource.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();

        // Assert
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-loading-repositories']"));

        repositoryTaskSource.SetResult([CreateRepository("owner", "repo")]);

        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));
    }

    [Fact]
    public async Task Triage_NoRepositoriesReturned_ShowsNoRepositoriesWarningState()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<RepositoryDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("[data-testid='triage-no-repositories-alert']"));
            Assert.Contains("No active repositories are available for triage.", cut.Markup, StringComparison.Ordinal);
        });

    }

    [Fact]
    public async Task Triage_RepositoryLoadFailure_ShowsInlineErrorWithRetry()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<RepositoryDto>>(new HttpRequestException("Bad gateway", null, HttpStatusCode.BadGateway)));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("[data-testid='triage-repositories-load-error']"));
            Assert.Contains("GitHub API request failed while loading repositories.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Try loading repositories again", cut.Markup, StringComparison.Ordinal);
            Assert.Empty(cut.FindAll("[data-testid='triage-no-repositories-alert']"));
        });
    }

    [Fact]
    public async Task Triage_RepositoryScopeChanged_OnFirstSelection_DoesNotShowSnackbar()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
            CreateRepository("owner", "repo-b"),
        ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();

        // Act
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));

        // Assert
        Assert.Empty(_snackbarProvider.FindAll(".mud-snackbar"));
    }

    [Fact]
    public async Task Triage_RepositoryScopeChanged_WhenSwitchingBetweenRepositories_ShowsSnackbar()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
            CreateRepository("owner", "repo-b"),
        ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));

        // Act
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-b" }));

        // Assert
        _snackbarProvider.WaitForAssertion(() =>
            SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Repository scope changed. Start a new triage session to load items."));
    }

    [Fact]
    public async Task Triage_StartSessionFailure_ShowsErrorSnackbar()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TriageSessionDto>(new HttpRequestException("Bad gateway", null, HttpStatusCode.BadGateway)));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        await cut.InvokeAsync(() => cut.Find("[data-testid='triage-start-session-button']").Click());

        // Assert
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "GitHub API request failed while starting triage session."));
    }

    [Fact]
    public async Task Triage_StartSessionWithEmptyQueue_ShowsSummaryStateAndEmptyQueueMessage()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var emptySession = CreateSession(
            "owner",
            "repo",
            [],
            currentIndex: 0,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(emptySession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("[data-testid='triage-session-complete-region']"));
            Assert.Contains("Processed 0 of 0 items. 0 skipped item(s) are available to revisit.", cut.Markup, StringComparison.Ordinal);
        });

        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "No untriaged items were found in owner/repo."));
    }

    [Fact]
    public async Task Triage_StartedSessionWithPullRequestCurrentItem_ShowsPullRequestVariantAndKeyboardLegend()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var pullRequestItem = new TriageItemDto(
            TriageItemTypeDto.PullRequest,
            2201,
            2201,
            "owner/repo",
            "PR 2201",
            "https://github.com/owner/repo/pull/2201",
            "Body for PR 2201",
            "open",
            "markheydon",
            [],
            null,
            string.Empty,
            DateTimeOffset.UtcNow.AddDays(-4),
            DateTimeOffset.UtcNow.AddDays(-1));

        var startedSession = CreateSession(
            "owner",
            "repo",
            [pullRequestItem],
            currentIndex: 0,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Pull request", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Open item #2201 on GitHub", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Keyboard shortcuts (when the action buttons are focused)", cut.Markup, StringComparison.Ordinal);
        });

    }

    [Fact]
    public async Task Triage_StartSessionClicked_LoadsFirstItemAndShowsRemainingCount()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [
                CreateItem(101, "Issue 101", "owner/repo"),
                CreateItem(102, "Issue 102", "owner/repo"),
            ],
            currentIndex: 0,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Issue 101", cut.Markup);
            Assert.Contains("Item 1 of 2", cut.Markup);
            Assert.Contains("Remaining: 2 items", cut.Markup);
        });


        await _triageService.Received(1).StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_NextItemClicked_MovesToNextQueueItemAndUpdatesContext()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [
                CreateItem(101, "Issue 101", "owner/repo"),
                CreateItem(102, "Issue 102", "owner/repo"),
            ],
            currentIndex: 0,
            skippedItems: []);

        var advancedSession = startedSession with
        {
            CurrentIndex = 1,
            Progress = new TriageSessionProgressDto(2, 1, 1, 0),
            Summary = new TriageSessionSummaryDto(2, 1, 1, 0, 0, 0, 0, 0),
        };

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(advancedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 101", cut.Markup));

        cut.Find("[data-testid='triage-next-item-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Issue 102", cut.Markup);
            Assert.Contains("Item 2 of 2", cut.Markup);
            Assert.Contains("Remaining: 1 item", cut.Markup);
        });

    }

    [Fact]
    public async Task Triage_NextClicked_MovesToNextItemWithoutApplyingChanges()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [
                CreateItem(101, "Issue 101", "owner/repo"),
                CreateItem(102, "Issue 102", "owner/repo"),
            ],
            currentIndex: 0,
            skippedItems: []);

        var advancedSession = CreateSession(
            "owner",
            "repo",
            [
                CreateItem(101, "Issue 101", "owner/repo"),
                CreateItem(102, "Issue 102", "owner/repo"),
            ],
            currentIndex: 1,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(advancedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 101", cut.Markup));

        cut.Find("[data-testid='triage-next-item-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Issue 102", cut.Markup);
            Assert.Contains("Skipped: 0 items", cut.Markup);
        });


        await _triageService.Received(1).AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_NextClickedOnFinalItem_ShowsSessionCompleteWithoutProgressHeader()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(103, "Issue 103", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        var completedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(103, "Issue 103", "owner/repo")],
            currentIndex: 1,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(completedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 103", cut.Markup));

        cut.Find("[data-testid='triage-next-item-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("[data-testid='triage-session-complete-region']"));
            Assert.Empty(cut.FindAll("[data-testid='triage-session-header-region']"));
            Assert.Empty(cut.FindAll("[data-testid='triage-progress-indicator']"));
        });

    }

    [Fact]
    public async Task Triage_CurrentItemBodyContainsMarkdown_RendersFormattedBodyContent()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var markdownItem = new TriageItemDto(
            TriageItemTypeDto.Issue,
            201,
            201,
            "owner/repo",
            "Markdown issue",
            "https://github.com/owner/repo/issues/201",
            "# Heading\n\n- One\n- Two\n\n[Link](https://example.com)",
            "open",
            "markheydon",
            [],
            null,
            string.Empty,
            DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddDays(-1));

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(CreateSession("owner", "repo", [markdownItem], 0, []));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));
        cut.Find("[data-testid='triage-start-session-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var body = cut.Find("[data-testid='triage-item-body']");
            Assert.Contains("<h1", body.InnerHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<li", body.InnerHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("href=\"https://example.com\"", body.InnerHtml, StringComparison.OrdinalIgnoreCase);
        });

    }

    [Fact]
    public async Task Triage_CurrentItemBodyContainsRelativeAndUnsafeLinks_PreservesRelativeAndNeutralisesUnsafeHref()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var markdownItem = new TriageItemDto(
            TriageItemTypeDto.Issue,
            202,
            202,
            "owner/repo",
            "Link safety issue",
            "https://github.com/owner/repo/issues/202",
            "[Relative](./docs/page.md) [Query](?view=compact) [Unsafe](javascript:alert('x'))",
            "open",
            "markheydon",
            [],
            null,
            string.Empty,
            DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddDays(-1));

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(CreateSession("owner", "repo", [markdownItem], 0, []));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));
        cut.Find("[data-testid='triage-start-session-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var body = cut.Find("[data-testid='triage-item-body']");
            Assert.Contains("href=\"./docs/page.md\"", body.InnerHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("href=\"?view=compact\"", body.InnerHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("href=\"#\"", body.InnerHtml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("href=\"javascript:alert", body.InnerHtml, StringComparison.OrdinalIgnoreCase);
        });

    }

    [Fact]
    public async Task Triage_ApplyLabelClicked_InvokesTriageServiceAndShowsUpdatedLabels()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        _labelManagerService.GetLabelsAsync("owner", "repo", Arg.Any<CancellationToken>()).Returns([
                new LabelDto("type/bug", "d73a4a", "A bug or unexpected behaviour", "repo"),
            ]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [
                CreateItem(401, "Issue 401", "owner/repo"),
                CreateItem(402, "Issue 402", "owner/repo"),
            ],
            currentIndex: 0,
            skippedItems: []);

        var labelledSession = startedSession with
        {
            Queue =
            [
                startedSession.Queue[0] with
                {
                    Labels = ["type/bug"],
                },
                startedSession.Queue[1],
            ],
            ActionHistory =
            [
                new TriageActionDto(TriageActionTypeDto.LabelApplied, TriageItemTypeDto.Issue, 401, "owner/repo", "Applied label 'type/bug'.", DateTimeOffset.UtcNow),
            ],
            Summary = new TriageSessionSummaryDto(2, 0, 2, 0, 1, 0, 0, 0),
        };

        var advancedSession = labelledSession with
        {
            CurrentIndex = 1,
            Progress = new TriageSessionProgressDto(2, 1, 1, 0),
            Summary = new TriageSessionSummaryDto(2, 1, 1, 0, 1, 0, 0, 0),
        };

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.ApplyLabelToCurrentItemAsync(Arg.Any<TriageSessionDto>(), "type/bug", Arg.Any<CancellationToken>()).Returns(labelledSession);

        _triageService.AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(advancedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 401", cut.Markup));

        await SelectQuickLabelAsync(cut, "type/bug");

        await cut.InvokeAsync(() => cut.Find("[data-testid='triage-apply-label-button']").Click());

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Issue 402", cut.Markup));
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Applied label 'type/bug' to item #401 and moved to Item 2 of 2"));

        await _triageService.Received(1).ApplyLabelToCurrentItemAsync(Arg.Any<TriageSessionDto>(), "type/bug", Arg.Any<CancellationToken>());
        await _triageService.Received(1).AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_ActionSurfaceShortcutL_PressedAppliesSelectedLabel()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        _labelManagerService.GetLabelsAsync("owner", "repo", Arg.Any<CancellationToken>()).Returns([
                new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", "repo"),
            ]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(402, "Issue 402", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.ApplyLabelToCurrentItemAsync(Arg.Any<TriageSessionDto>(), "priority/high", Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(startedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='triage-action-surface-region']")));

        await SelectQuickLabelAsync(cut, "priority/high");

        cut.Find("[data-testid='triage-action-buttons-row']").KeyDown("l");

        // Assert
        await _triageService.Received(1).ApplyLabelToCurrentItemAsync(Arg.Any<TriageSessionDto>(), "priority/high", Arg.Any<CancellationToken>());
        await _triageService.Received(1).AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_SessionStartsWithAvailableLabels_QuickLabelDefaultsToEmptyAndApplyDisabled()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        _labelManagerService.GetLabelsAsync("owner", "repo", Arg.Any<CancellationToken>()).Returns([
                new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", "repo"),
                new LabelDto("type/bug", "d73a4a", "A bug or unexpected behaviour", "repo"),
            ]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(450, "Issue 450", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.True(string.IsNullOrEmpty(cut.Find("[data-testid='triage-quick-label-autocomplete']").GetAttribute("value")));
            Assert.True(cut.Find("[data-testid='triage-apply-label-button']").HasAttribute("disabled"));
        });


        await _triageService.DidNotReceive().ApplyLabelToCurrentItemAsync(Arg.Any<TriageSessionDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_AssignMilestoneClicked_AssignsMilestoneAndShowsSuccessMessage()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(501, "Issue 501", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        var milestoneAssignedSession = startedSession with
        {
            Queue =
            [
                startedSession.Queue[0] with
                {
                    MilestoneNumber = 7,
                    MilestoneTitle = "v0.7.0",
                },
            ],
            ActionHistory =
            [
                new TriageActionDto(TriageActionTypeDto.MilestoneAssigned, TriageItemTypeDto.Issue, 501, "owner/repo", "Assigned milestone 'v0.7.0'.", DateTimeOffset.UtcNow),
            ],
            Summary = new TriageSessionSummaryDto(1, 0, 1, 0, 0, 1, 0, 0),
        };

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.GetMilestoneOptionsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns([new TriageMilestoneOptionDto(7, "v0.7.0")]);

        _triageService.GetProjectBoardOptionsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(new TriageProjectBoardDiscoveryDto([], 0, 0));

        _triageService.AssignMilestoneToCurrentItemAsync(Arg.Any<TriageSessionDto>(), 7, "v0.7.0", Arg.Any<CancellationToken>()).Returns(milestoneAssignedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 501", cut.Markup));

        var milestoneSelect = cut
            .FindComponents<MudSelect<int?>>()
            .Single(component => string.Equals(component.Instance.Label, "Milestone", StringComparison.Ordinal));

        await cut.InvokeAsync(() => milestoneSelect.Instance.ValueChanged.InvokeAsync(7));
        cut.Find("[data-testid='triage-assign-milestone-button']").Click();

        // Assert
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Assigned milestone 'v0.7.0' to item #501"));


        await _triageService.Received(1).AssignMilestoneToCurrentItemAsync(Arg.Any<TriageSessionDto>(), 7, "v0.7.0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_StartSessionWithPullRequestMilestone_PreselectsMilestoneInDropdown()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [
                new TriageItemDto(
                    TriageItemTypeDto.PullRequest,
                    2202,
                    2202,
                    "owner/repo",
                    "PR 2202",
                    "https://github.com/owner/repo/pull/2202",
                    "Body for PR 2202",
                    "open",
                    "markheydon",
                    [],
                    7,
                    "v0.7.0",
                    DateTimeOffset.UtcNow.AddDays(-3),
                    DateTimeOffset.UtcNow.AddDays(-1)),
            ],
            currentIndex: 0,
            skippedItems: []);

        var milestoneAssignedSession = startedSession with
        {
            ActionHistory =
            [
                new TriageActionDto(TriageActionTypeDto.MilestoneAssigned, TriageItemTypeDto.PullRequest, 2202, "owner/repo", "Assigned milestone 'v0.7.0'.", DateTimeOffset.UtcNow),
            ],
            Summary = new TriageSessionSummaryDto(1, 0, 1, 0, 0, 1, 0, 0),
        };

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.GetMilestoneOptionsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns([
                new TriageMilestoneOptionDto(7, "v0.7.0"),
                new TriageMilestoneOptionDto(8, "v0.8.0"),
            ]);

        _triageService.GetProjectBoardOptionsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(new TriageProjectBoardDiscoveryDto([], 0, 0));

        _triageService.AssignMilestoneToCurrentItemAsync(Arg.Any<TriageSessionDto>(), 7, "v0.7.0", Arg.Any<CancellationToken>()).Returns(milestoneAssignedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();

        cut.WaitForAssertion(() => Assert.Contains("PR 2202", cut.Markup, StringComparison.Ordinal));

        cut.Find("[data-testid='triage-assign-milestone-button']").Click();

        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Assigned milestone 'v0.7.0' to item #2202"));


        // Assert
        await _triageService.Received(1).AssignMilestoneToCurrentItemAsync(Arg.Any<TriageSessionDto>(), 7, "v0.7.0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_AddToProjectBoardClicked_AddsItemAndShowsSuccessMessage()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(601, "Issue 601", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        var updatedSession = startedSession with
        {
            ActionHistory =
            [
                new TriageActionDto(TriageActionTypeDto.ProjectBoardAssigned, TriageItemTypeDto.Issue, 601, "owner/repo", "Added to project board 'Roadmap' with status 'In Progress'.", DateTimeOffset.UtcNow),
            ],
            Summary = new TriageSessionSummaryDto(1, 0, 1, 0, 0, 0, 1, 0),
        };

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.GetMilestoneOptionsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns([]);

        _triageService.GetProjectBoardOptionsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(new TriageProjectBoardDiscoveryDto(
            [
                new TriageProjectBoardOptionDto(
                    "project-id",
                    "Roadmap",
                    "owner",
                    "status-field-id",
                    [
                        new TriageProjectBoardStatusOptionDto("in-progress", "In Progress"),
                        new TriageProjectBoardStatusOptionDto("done", "Done"),
                    ]),
            ],
            1,
            0));

        _triageService.AddCurrentItemToProjectBoardAsync(Arg.Any<TriageSessionDto>(), "project-id", "Roadmap", "status-field-id", "in-progress", "In Progress", Arg.Any<CancellationToken>()).Returns(updatedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 601", cut.Markup));

        cut.Find("[data-testid='triage-add-to-project-board-button']").Click();

        // Assert
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Added item #601 to 'Roadmap' with status 'In Progress'"));


        await _triageService.Received(1).AddCurrentItemToProjectBoardAsync(Arg.Any<TriageSessionDto>(), "project-id", "Roadmap", "status-field-id", "in-progress", "In Progress", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_CloseAsDuplicateClicked_ClosesCurrentItemAndAdvancesSession()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [
                CreateItem(701, "Issue 701", "owner/repo"),
                CreateItem(702, "Issue 702", "owner/repo"),
            ],
            currentIndex: 0,
            skippedItems: []);

        var duplicateClosedSession = startedSession with
        {
            ActionHistory =
            [
                new TriageActionDto(TriageActionTypeDto.ClosedAsDuplicate, TriageItemTypeDto.Issue, 701, "owner/repo", "Closed as duplicate of '#555'.", DateTimeOffset.UtcNow),
            ],
            Summary = new TriageSessionSummaryDto(2, 0, 2, 0, 0, 0, 0, 1),
        };

        var advancedSession = duplicateClosedSession with
        {
            CurrentIndex = 1,
            Progress = new TriageSessionProgressDto(2, 1, 1, 0),
            Summary = new TriageSessionSummaryDto(2, 1, 1, 0, 0, 0, 0, 1),
        };

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.CloseCurrentItemAsDuplicateAsync(Arg.Any<TriageSessionDto>(), "#555", Arg.Any<CancellationToken>()).Returns(duplicateClosedSession);

        _triageService.AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(advancedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 701", cut.Markup));

        var duplicateReferenceInput = cut
            .FindComponents<MudTextField<string>>()
            .Single(component => string.Equals(component.Instance.Label, "Duplicate reference", StringComparison.Ordinal));

        await cut.InvokeAsync(() => duplicateReferenceInput.Instance.ValueChanged.InvokeAsync("#555"));

        cut.Find("[data-testid='triage-close-duplicate-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Issue 702", cut.Markup);
        });
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Closed item #701 as a duplicate of '#555' and moved to Item 2 of 2"));


        await _triageService.Received(1).CloseCurrentItemAsDuplicateAsync(Arg.Any<TriageSessionDto>(), "#555", Arg.Any<CancellationToken>());
        await _triageService.Received(1).AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_ActionSurfaceShortcutD_PressedClosesCurrentItemAsDuplicate()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(703, "Issue 703", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.CloseCurrentItemAsDuplicateAsync(Arg.Any<TriageSessionDto>(), "#556", Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(startedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='triage-action-surface-region']")));

        var duplicateReferenceInput = cut
            .FindComponents<MudTextField<string>>()
            .Single(component => string.Equals(component.Instance.Label, "Duplicate reference", StringComparison.Ordinal));

        await cut.InvokeAsync(() => duplicateReferenceInput.Instance.ValueChanged.InvokeAsync("#556"));

        cut.Find("[data-testid='triage-action-buttons-row']").KeyDown("d");

        // Assert
        await _triageService.Received(1).CloseCurrentItemAsDuplicateAsync(Arg.Any<TriageSessionDto>(), "#556", Arg.Any<CancellationToken>());
        await _triageService.Received(1).AdvanceSessionAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_SkipItemClicked_RecordsSkipAndMovesToNextItem()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [
                CreateItem(801, "Issue 801", "owner/repo"),
                CreateItem(802, "Issue 802", "owner/repo"),
            ],
            currentIndex: 0,
            skippedItems: []);

        var skippedSession = new TriageSessionDto(
            startedSession.SessionId,
            "owner",
            "repo",
            false,
            [CreateItem(802, "Issue 802", "owner/repo")],
            0,
            [CreateItem(801, "Issue 801", "owner/repo")],
            [
                new TriageActionDto(TriageActionTypeDto.Skipped, TriageItemTypeDto.Issue, 801, "owner/repo", "Skipped for later review. Reason: Requires broader context", DateTimeOffset.UtcNow),
            ],
            new TriageSessionProgressDto(1, 0, 1, 1),
            new TriageSessionSummaryDto(1, 0, 1, 1, 0, 0, 0, 0)
            {
                SkippedItemDetails = ["Issue #801 (owner/repo): Issue 801"],
            },
            DateTimeOffset.UtcNow);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        _triageService.SkipCurrentItemAsync(Arg.Any<TriageSessionDto>(), "Requires broader context", Arg.Any<CancellationToken>()).Returns(skippedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 801", cut.Markup));

        var skipReasonInput = cut
            .FindComponents<MudTextField<string>>()
            .Single(component => string.Equals(component.Instance.Label, "Skip reason (optional)", StringComparison.Ordinal));

        await cut.InvokeAsync(() => skipReasonInput.Instance.ValueChanged.InvokeAsync("Requires broader context"));
        cut.Find("[data-testid='triage-skip-item-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Issue 802", cut.Markup);
            Assert.Contains("Skipped: 1 item", cut.Markup);
        });
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Skipped item #801 for later review and moved to Item 1 of 1"));


        await _triageService.Received(1).SkipCurrentItemAsync(Arg.Any<TriageSessionDto>(), "Requires broader context", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_SessionCompleted_ShowsGroupedSummaryDetailsAndSkippedRevisitButton()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var completedSession = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            [CreateItem(901, "Issue 901", "owner/repo")],
            1,
            [CreateItem(902, "Issue 902", "owner/repo")],
            [],
            new TriageSessionProgressDto(1, 1, 0, 1),
            new TriageSessionSummaryDto(1, 1, 0, 1, 2, 1, 1, 1)
            {
                LabelActionDetails = ["Issue #901 (owner/repo): Applied label 'type/story'.", "Issue #902 (owner/repo): Applied label 'priority/high'."],
                MilestoneActionDetails = ["Issue #901 (owner/repo): Assigned milestone 'v0.3.0'."],
                ProjectActionDetails = ["Issue #901 (owner/repo): Added to project board 'Roadmap' with status 'In Progress'."],
                DuplicateActionDetails = ["Issue #902 (owner/repo): Closed as duplicate of '#321'."],
                SkippedActionDetails = ["Issue #902 (owner/repo): Skipped for later review. Reason: Waiting for product clarification"],
                SkippedItemDetails = ["Issue #902 (owner/repo): Issue 902"],
            },
            DateTimeOffset.UtcNow);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(completedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("[data-testid='triage-session-complete-region']"));
            Assert.Contains("Processed 1 of 1 items. 1 skipped item(s) are available to revisit.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Issue #901", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("(owner/repo): Applied label 'type/story'.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("(owner/repo): Assigned milestone 'v0.3.0'.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("(owner/repo): Added to project board 'Roadmap' with status 'In Progress'.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Issue #902", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("(owner/repo): Closed as duplicate of '#321'.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("(owner/repo): Skipped for later review. Reason: Waiting for product clarification", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("(owner/repo): Issue 902", cut.Markup, StringComparison.Ordinal);
            Assert.NotEmpty(cut.FindAll("[data-testid='triage-revisit-skipped-button']"));
        });

    }

    [Fact]
    public async Task Triage_SessionCompleted_MixedIssueAndPullRequestSummaryEntries_RenderTypeSpecificGitHubLinks()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var completedSession = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            [CreateItem(1001, "Issue 1001", "owner/repo")],
            1,
            [
                new TriageItemDto(
                    TriageItemTypeDto.PullRequest,
                    1002,
                    1002,
                    "owner/repo",
                    "Pull request 1002",
                    "https://github.com/owner/repo/pull/1002",
                    "Body for pull request 1002",
                    "open",
                    "markheydon",
                    [],
                    null,
                    string.Empty,
                    DateTimeOffset.UtcNow.AddDays(-3),
                    DateTimeOffset.UtcNow.AddDays(-1)),
            ],
            [],
            new TriageSessionProgressDto(1, 1, 0, 1),
            new TriageSessionSummaryDto(1, 1, 0, 1, 1, 1, 1, 1)
            {
                LabelActionDetails = ["Issue #1001 (owner/repo): Applied label 'type/story'."],
                MilestoneActionDetails = ["Pull request #1002 (owner/repo): Assigned milestone 'v0.3.0'."],
                ProjectActionDetails = ["Pull request #1002 (owner/repo): Added to project board 'Roadmap' with status 'In Progress'."],
                DuplicateActionDetails = ["Issue #1001 (owner/repo): Closed as duplicate of '#999'."],
                SkippedActionDetails = ["Pull request #1002 (owner/repo): Skipped for later review. Reason: Waiting for product clarification"],
                SkippedItemDetails = ["Pull request #1002 (owner/repo): Pull request 1002"],
            },
            DateTimeOffset.UtcNow);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(completedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("href=\"https://github.com/owner/repo/issues/1001\"", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("href=\"https://github.com/owner/repo/pull/1002\"", cut.Markup, StringComparison.Ordinal);
        });

    }

    [Fact]
    public async Task Triage_RevisitSkippedItemsClicked_ResumesSessionFromSkippedQueue()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        var completedSession = new TriageSessionDto(
            Guid.NewGuid(),
            "owner",
            "repo",
            false,
            [CreateItem(950, "Issue 950", "owner/repo")],
            1,
            [CreateItem(951, "Issue 951", "owner/repo")],
            [],
            new TriageSessionProgressDto(1, 1, 0, 1),
            new TriageSessionSummaryDto(1, 1, 0, 1, 0, 0, 0, 0)
            {
                SkippedItemDetails = ["Issue #951 (owner/repo): Issue 951"],
            },
            DateTimeOffset.UtcNow);

        var resumedSession = new TriageSessionDto(
            completedSession.SessionId,
            "owner",
            "repo",
            false,
            [CreateItem(951, "Issue 951", "owner/repo")],
            0,
            [],
            [],
            new TriageSessionProgressDto(1, 0, 1, 0),
            new TriageSessionSummaryDto(1, 0, 1, 0, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(completedSession);

        _triageService.RevisitSkippedItemsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>()).Returns(resumedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='triage-revisit-skipped-button']")));

        cut.Find("[data-testid='triage-revisit-skipped-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Issue 951", cut.Markup);
        });
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Skipped items were appended to the queue for review."));


        await _triageService.Received(1).RevisitSkippedItemsAsync(Arg.Any<TriageSessionDto>(), Arg.Any<CancellationToken>());
    }

    private static async Task SelectQuickLabelAsync(IRenderedComponent<Triage> cut, string labelName)
    {
        ArgumentNullException.ThrowIfNull(cut);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);

        var quickLabelAutocomplete = cut
            .FindComponents<MudAutocomplete<string>>()
            .Single(component => string.Equals(component.Instance.Label, "Quick label", StringComparison.Ordinal));

        await cut.InvokeAsync(() => quickLabelAutocomplete.Instance.ValueChanged.InvokeAsync(labelName));
    }

    [Fact]
    public async Task Triage_QuickLabelInputShortcutL_Pressed_DoesNotApplySelectedLabel()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo")]);

        _labelManagerService.GetLabelsAsync("owner", "repo", Arg.Any<CancellationToken>()).Returns([
                new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", "repo"),
            ]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(403, "Issue 403", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-quick-label-autocomplete']"));

        cut.Find("[data-testid='triage-quick-label-autocomplete']").KeyDown("l");

        // Assert
        await _triageService.DidNotReceive().ApplyLabelToCurrentItemAsync(Arg.Any<TriageSessionDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Triage_RepositorySelectionClearedAfterSessionStarted_HidesActiveSessionDetails()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([CreateRepository("owner", "repo-a"), CreateRepository("owner", "repo-b")]);

        var startedSession = CreateSession(
            "owner",
            "repo-a",
            [CreateItem(301, "Issue 301", "owner/repo-a")],
            currentIndex: 0,
            skippedItems: []);

        _triageService.StartSessionAsync("owner", "repo-a", true, Arg.Any<CancellationToken>()).Returns(startedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo-a" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 301", cut.Markup));

        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(Array.Empty<string>()));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("[data-testid='triage-not-started-region']"));
            Assert.DoesNotContain("Issue 301", cut.Markup);
            Assert.Empty(cut.FindAll("[data-testid='triage-item-detail-region']"));
        });

    }

    private static RepositoryDto CreateRepository(string owner, string name)
        => new(
            Id: Math.Abs(HashCode.Combine(owner, name)),
            Name: name,
            FullName: $"{owner}/{name}",
            Description: $"Repository {name}",
            Url: $"https://github.com/{owner}/{name}",
            IsPrivate: false,
            IsArchived: false,
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-30),
            UpdatedAt: DateTimeOffset.UtcNow.AddDays(-1),
            Topics: [],
            IsOpenSource: false);

    private static TriageItemDto CreateItem(int number, string title, string repositoryFullName)
        => new(
            TriageItemTypeDto.Issue,
            number,
            number,
            repositoryFullName,
            title,
            $"https://github.com/{repositoryFullName}/issues/{number}",
            $"Body for {title}",
            "open",
            "markheydon",
            [],
            null,
            string.Empty,
            DateTimeOffset.UtcNow.AddDays(-5),
            DateTimeOffset.UtcNow.AddDays(-1));

    private static TriageSessionDto CreateSession(
        string owner,
        string repo,
        IReadOnlyList<TriageItemDto> queue,
        int currentIndex,
        IReadOnlyList<TriageItemDto> skippedItems)
    {
        var processed = Math.Min(Math.Max(currentIndex, 0), queue.Count);
        var remaining = Math.Max(queue.Count - processed, 0);

        return new TriageSessionDto(
            Guid.NewGuid(),
            owner,
            repo,
            false,
            queue,
            currentIndex,
            skippedItems,
            [],
            new TriageSessionProgressDto(queue.Count, processed, remaining, skippedItems.Count),
            new TriageSessionSummaryDto(queue.Count, processed, remaining, skippedItems.Count, 0, 0, 0, 0),
            DateTimeOffset.UtcNow);
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _triageService);
        ctx.Services.AddScoped(_ => _labelManagerService);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        _snackbarProvider = ctx.Render<MudSnackbarProvider>();

        return ctx;
    }
}
