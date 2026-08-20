using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoloDevBoard.App.Components.Features.PmWorkflow;
using SoloDevBoard.App.Components.Features.PmWorkflow.Pages;
using SoloDevBoard.App.Components.Shell.Layout;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for Backlog Review on <see cref="PmWorkflowBacklog"/>.</summary>
public sealed class PmWorkflowBacklogTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService =
        Substitute.For<IPmProjectBoardDiscoveryService>();
    private readonly IBacklogReviewService _backlogReviewService = Substitute.For<IBacklogReviewService>();
    private readonly FakePmSettingsStorage _settingsStorage = new();

    public PmWorkflowBacklogTests()
    {
        _backlogReviewService.GetBacklogAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmptyBacklogResult());
    }

    [Fact]
    public void PmWorkflowLayout_UsesMainLayoutAsParent()
    {
        var layoutAttribute = typeof(PmWorkflowLayout)
            .GetCustomAttributes(typeof(LayoutAttribute), inherit: true)
            .Cast<LayoutAttribute>()
            .FirstOrDefault();

        Assert.NotNull(layoutAttribute);
        Assert.Equal(typeof(MainLayout), layoutAttribute.LayoutType);
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenNoBoardIsSelected_ShowsInstructionalAlert()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PmProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Backlog Review", cut.Markup);
            Assert.Contains("Select a planning board in the dropdown above to load Backlog Review groups.", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenGroupsHaveItems_ShowsPanelsFiltersAndKindChips()
    {
        ConfigureDefaults();
        _backlogReviewService.GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(new BacklogReviewResultDto(
                [CreateItem(PmWorkItemTypeDto.Issue, "owner/repo-a", 10, "Fix auth", ["priority/high"])],
                [CreateItem(PmWorkItemTypeDto.Issue, "owner/repo-a", 11, "Ready story", ["priority/medium"])],
                [CreateItem(PmWorkItemTypeDto.Issue, "owner/repo-c", 13, "Needs labels", ["type/story"])],
                [CreateItem(PmWorkItemTypeDto.PullRequest, "owner/repo-b", 12, "Parked PR", ["status/blocked"])],
                [],
                [],
                false,
                []));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-testid=\"pm-workflow-backlog-filters\"", cut.Markup);
            Assert.Contains("Urgent", cut.Markup);
            Assert.Contains("Ready to start", cut.Markup);
            Assert.Contains("Awaiting triage", cut.Markup);
            Assert.Contains("Blocked / deferred", cut.Markup);
            Assert.Contains("Epics near completion", cut.Markup);
            Assert.Contains("Neglected repositories", cut.Markup);
            Assert.Contains("owner/repo-a#10", cut.Markup);
            Assert.Contains("Fix auth", cut.Markup);
            Assert.Contains("owner/repo-a#11", cut.Markup);
            Assert.Contains("owner/repo-b#12", cut.Markup);
            Assert.Contains("Needs labels", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-backlog-kind-chip\"", cut.Markup);
            Assert.Contains("Pull request", cut.Markup);
            Assert.Contains("Issue", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenCatalogueIsEmpty_ShowsEmptyAlert()
    {
        ConfigureDefaults();

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No open issues or pull requests in included repositories.", cut.Markup);
            Assert.Contains("No items in this group.", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenLoadFails_ShowsErrorWithRetry()
    {
        ConfigureDefaults();
        _backlogReviewService.GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("GitHub unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load the backlog.", cut.Markup);
            Assert.Contains("Retry", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenRetryClickedAfterFailure_ReloadsGroups()
    {
        ConfigureDefaults();
        _backlogReviewService.GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("GitHub unavailable"),
                _ => new BacklogReviewResultDto(
                    [CreateItem(PmWorkItemTypeDto.Issue, "owner/repo-a", 40, "Recovered", ["priority/high"])],
                    [],
                    [],
                    [],
                    [],
                    [],
                    false,
                    []));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() => Assert.Contains("Unable to load the backlog.", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='pm-workflow-backlog-retry']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-a#40", cut.Markup);
            Assert.Contains("Recovered", cut.Markup);
        });

        await _backlogReviewService.Received(2).GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenCatalogueHasPartialFailures_ShowsWarningAndRows()
    {
        ConfigureDefaults();
        _backlogReviewService.GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(new BacklogReviewResultDto(
                [CreateItem(PmWorkItemTypeDto.Issue, "owner/repo-a", 40, "Ship backlog", ["priority/high"])],
                [],
                [],
                [],
                [],
                [],
                false,
                [new PmRepositoryCatalogueFailureDto("owner/failed-repo", "Not found", 404)]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-a#40", cut.Markup);
            Assert.Contains("owner/failed-repo", cut.Markup);
            Assert.Contains("grouped without 1 repository that failed to load", cut.Markup);
            Assert.DoesNotContain("Unable to load the backlog.", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenSearchExcludesAllRows_ShowsFilterEmptyAlert()
    {
        ConfigureDefaults();
        _backlogReviewService.GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(new BacklogReviewResultDto(
                [CreateItem(PmWorkItemTypeDto.Issue, "owner/repo-a", 10, "Fix auth", ["priority/high"])],
                [],
                [],
                [],
                [],
                [],
                false,
                []));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() => Assert.Contains("Fix auth", cut.Markup));

        var panel = cut.FindComponent<PmWorkflowBacklogPanel>();
        await cut.InvokeAsync(() => panel.Instance.OnSearchChanged("nomatch"));

        cut.WaitForAssertion(() =>
            Assert.Contains("No items match the current filters.", cut.Markup));
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenNeglectedRepositoriesExist_ShowsRepositoryRows()
    {
        ConfigureDefaults();
        _backlogReviewService.GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(new BacklogReviewResultDto(
                [],
                [],
                [],
                [],
                [],
                [new BacklogNeglectedRepositoryDto("owner/quiet", DateTimeOffset.Parse("2026-07-01T00:00:00Z"), 0, 0)],
                false,
                []));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/quiet", cut.Markup);
            Assert.Contains("1 Jul 2026", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowBacklog_WhenSubIssueCountsUnavailable_ShowsExplanatoryAlert()
    {
        ConfigureDefaults();
        _backlogReviewService.GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>())
            .Returns(new BacklogReviewResultDto(
                [],
                [],
                [],
                [],
                [],
                [],
                true,
                []));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        cut.WaitForAssertion(() =>
            Assert.Contains("GitHub did not return sub-issue counts", cut.Markup));
    }

    private void ConfigureDefaults()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PmProjectBoardDiscoveryDto(
                [new PmPlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                1,
                0));
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddSingleton<IPmSettingsStorage>(_settingsStorage);
        ctx.Services.AddScoped<IPmSettingsService, PmSettingsService>();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _projectBoardDiscoveryService);
        ctx.Services.AddScoped(_ => _backlogReviewService);
        ctx.Services.AddScoped<PmWorkflowChromeCoordinator>();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static BacklogReviewResultDto EmptyBacklogResult()
        => new([], [], [], [], [], [], false, []);

    private static BacklogReviewItemDto CreateItem(
        PmWorkItemTypeDto itemType,
        string repositoryFullName,
        int number,
        string title,
        IReadOnlyList<string> labels)
        => new(
            itemType,
            repositoryFullName,
            number,
            title,
            $"https://github.com/{repositoryFullName}/issues/{number}",
            labels,
            PmLabelHelpers.ParsePriorityLabel(labels),
            BoardStatusName: null);

    private static RepositoryDto CreateRepository(string owner, string name) =>
        new(1, name, $"{owner}/{name}", string.Empty, $"https://github.com/{owner}/{name}", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private sealed class FakePmSettingsStorage : IPmSettingsStorage
    {
        public string? StoredJson { get; set; }

        public Task<string?> GetStoredJsonAsync() => Task.FromResult(StoredJson);

        public Task SetStoredJsonAsync(string json)
        {
            StoredJson = json;
            return Task.CompletedTask;
        }
    }
}
