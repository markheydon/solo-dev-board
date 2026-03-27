using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.Triage.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Triage;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Triage"/> page.</summary>
public sealed class TriageTests
{
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();
    private readonly Mock<ITriageService> _triageServiceMock = new();
    private readonly Mock<ILabelManagerService> _labelManagerServiceMock = new();

    [Fact]
    public async Task Triage_StartSessionClicked_LoadsFirstItemAndShowsRemainingCount()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [
                CreateItem(101, "Issue 101", "owner/repo"),
                CreateItem(102, "Issue 102", "owner/repo"),
            ],
            currentIndex: 0,
            skippedItems: []);

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

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

        _triageServiceMock.Verify(
            service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Triage_NextItemClicked_MovesToNextQueueItemAndUpdatesContext()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

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

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

        _triageServiceMock
            .Setup(service => service.AdvanceSessionAsync(It.IsAny<TriageSessionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(advancedSession);

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
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

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

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

        _triageServiceMock
            .Setup(service => service.AdvanceSessionAsync(It.IsAny<TriageSessionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(advancedSession);

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

        _triageServiceMock.Verify(
            service => service.AdvanceSessionAsync(It.IsAny<TriageSessionDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Triage_NextClickedOnFinalItem_ShowsSessionCompleteWithoutProgressHeader()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

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

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

        _triageServiceMock
            .Setup(service => service.AdvanceSessionAsync(It.IsAny<TriageSessionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedSession);

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
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

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

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession("owner", "repo", [markdownItem], 0, []));

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
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

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

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession("owner", "repo", [markdownItem], 0, []));

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
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
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

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

        _triageServiceMock
            .Setup(service => service.ApplyLabelToCurrentItemAsync(It.IsAny<TriageSessionDto>(), "type/bug", It.IsAny<CancellationToken>()))
            .ReturnsAsync(labelledSession);

        _triageServiceMock
            .Setup(service => service.AdvanceSessionAsync(It.IsAny<TriageSessionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(advancedSession);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Triage>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='triage-repository-autocomplete']"));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(new[] { "owner/repo" }));

        cut.Find("[data-testid='triage-start-session-button']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Issue 401", cut.Markup));

        await SelectQuickLabelAsync(cut, "type/bug");

        cut.Find("[data-testid='triage-apply-label-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Issue 402", cut.Markup);
            Assert.Contains("Applied label 'type/bug' to item #401 and moved to Item 2 of 2", cut.Markup, StringComparison.Ordinal);
        });

        _triageServiceMock.Verify(
            service => service.ApplyLabelToCurrentItemAsync(It.IsAny<TriageSessionDto>(), "type/bug", It.IsAny<CancellationToken>()),
            Times.Once);
        _triageServiceMock.Verify(
            service => service.AdvanceSessionAsync(It.IsAny<TriageSessionDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Triage_ActionSurfaceShortcutL_PressedAppliesSelectedLabel()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", "repo"),
            ]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(402, "Issue 402", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

        _triageServiceMock
            .Setup(service => service.ApplyLabelToCurrentItemAsync(It.IsAny<TriageSessionDto>(), "priority/high", It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

        _triageServiceMock
            .Setup(service => service.AdvanceSessionAsync(It.IsAny<TriageSessionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

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
        _triageServiceMock.Verify(
            service => service.ApplyLabelToCurrentItemAsync(It.IsAny<TriageSessionDto>(), "priority/high", It.IsAny<CancellationToken>()),
            Times.Once);
        _triageServiceMock.Verify(
            service => service.AdvanceSessionAsync(It.IsAny<TriageSessionDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Triage_SessionStartsWithAvailableLabels_QuickLabelDefaultsToEmptyAndApplyDisabled()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", "repo"),
                new LabelDto("type/bug", "d73a4a", "A bug or unexpected behaviour", "repo"),
            ]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(450, "Issue 450", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

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

        _triageServiceMock.Verify(
            service => service.ApplyLabelToCurrentItemAsync(It.IsAny<TriageSessionDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo")]);

        _labelManagerServiceMock
            .Setup(service => service.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", "repo"),
            ]);

        var startedSession = CreateSession(
            "owner",
            "repo",
            [CreateItem(403, "Issue 403", "owner/repo")],
            currentIndex: 0,
            skippedItems: []);

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

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
        _triageServiceMock.Verify(
            service => service.ApplyLabelToCurrentItemAsync(It.IsAny<TriageSessionDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Triage_RepositorySelectionClearedAfterSessionStarted_HidesActiveSessionDetails()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRepository("owner", "repo-a"), CreateRepository("owner", "repo-b")]);

        var startedSession = CreateSession(
            "owner",
            "repo-a",
            [CreateItem(301, "Issue 301", "owner/repo-a")],
            currentIndex: 0,
            skippedItems: []);

        _triageServiceMock
            .Setup(service => service.StartSessionAsync("owner", "repo-a", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startedSession);

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
            UpdatedAt: DateTimeOffset.UtcNow.AddDays(-1));

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
        _labelManagerServiceMock
            .SetReturnsDefault(Task.FromResult<IReadOnlyList<LabelDto>>(
            [
                new LabelDto("type/story", "1d76db", "A user-facing Story delivering a discrete piece of value", "repo"),
            ]));

        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => _repositoryServiceMock.Object);
        ctx.Services.AddScoped(_ => _triageServiceMock.Object);
        ctx.Services.AddScoped(_ => _labelManagerServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }
}
