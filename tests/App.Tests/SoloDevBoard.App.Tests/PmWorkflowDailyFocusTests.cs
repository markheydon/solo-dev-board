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

/// <summary>Component tests for Daily Focus occupancy on <see cref="PmWorkflowDailyFocus"/>.</summary>
public sealed class PmWorkflowDailyFocusTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPmProjectBoardDiscoveryService>();
    private readonly IDailyFocusBoardStateService _boardStateService = Substitute.For<IDailyFocusBoardStateService>();
    private readonly IDailyFocusStalledReviewService _stalledReviewService = Substitute.For<IDailyFocusStalledReviewService>();
    private readonly FakePmSettingsStorage _settingsStorage = new();

    public PmWorkflowDailyFocusTests()
    {
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new DailyFocusStalledReviewSnapshotDto([], UsedInReviewColumn: false));
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
    public async Task PmWorkflowDailyFocus_WhenNoBoardIsSelected_ShowsInstructionalAlert()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PmProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Daily Focus", cut.Markup);
            Assert.Contains("Select a planning board in the dropdown above to load Daily Focus occupancy.", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenBoardHasItems_ShowsOccupancyAndActiveLoad()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [
                    new DailyFocusOccupancyChipDto("Todo", 2),
                    new DailyFocusOccupancyChipDto("Up Next", 4),
                    new DailyFocusOccupancyChipDto("In Progress", 2),
                ],
                ActiveLoad: 6,
                Capacity: 8,
                ItemCount: 8));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Todo 2", cut.Markup);
            Assert.Contains("Up Next 4", cut.Markup);
            Assert.Contains("In Progress 2", cut.Markup);
            Assert.Contains("Active load: 6 / 8 (Up Next + In Progress)", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenBoardHasNoItems_ShowsEmptyAlert()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 0)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("This planning board has no items.", cut.Markup);
            Assert.Contains("Active load: 0 / 8", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenCatalogueFails_ShowsErrorWithRetry()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("GitHub unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load board occupancy.", cut.Markup);
            Assert.Contains("Retry", cut.Markup);
        });

        cut.Render();

        await _boardStateService.Received(1).GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenRetryClickedAfterCatalogueFailure_ReloadsBoardState()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("GitHub unavailable"),
                _ => new DailyFocusBoardStateDto(
                    [new DailyFocusOccupancyChipDto("Todo", 1)],
                    ActiveLoad: 0,
                    Capacity: 8,
                    ItemCount: 1));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() => Assert.Contains("Unable to load board occupancy.", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='pm-workflow-daily-focus-retry']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Todo 1", cut.Markup);
            Assert.Contains("Active load: 0 / 8", cut.Markup);
        });

        await _boardStateService.Received(2).GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenStalledReviewPullRequestsExist_ShowsRepoNumberAgeAndLink()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("In Review", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1));
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
                "PVT_board",
                3,
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new DailyFocusStalledReviewSnapshotDto(
                [
                    new DailyFocusStalledReviewPullRequestDto(
                        "owner/repo",
                        12,
                        5,
                        "https://github.com/owner/repo/pull/12",
                        "Stalled review"),
                ],
                UsedInReviewColumn: true));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("PRs awaiting review 3+ days", cut.Markup);
            Assert.Contains("owner/repo#12", cut.Markup);
            Assert.Contains("(5d)", cut.Markup);
            Assert.Contains("https://github.com/owner/repo/pull/12", cut.Markup);
            Assert.Contains(">Open<", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenNoStalledReviewPullRequests_ShowsEmptyAlert()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No pull requests have been awaiting review for 3 or more days.", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-daily-focus-stalled-reviews-empty\"", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenStalledReviewLoadFails_ShowsErrorWithRetry()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1));
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
                "PVT_board",
                3,
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("GitHub unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load pull requests awaiting review.", cut.Markup);
            Assert.Contains("data-testid=\"pm-workflow-daily-focus-stalled-reviews-retry\"", cut.Markup);
        });

        await _stalledReviewService.Received(1).GetStalledReviewPullRequestsAsync(
            "PVT_board",
            3,
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenRetryClickedAfterStalledReviewFailure_ReloadsStalledReviews()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1));
        _stalledReviewService.GetStalledReviewPullRequestsAsync(
                "PVT_board",
                3,
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("GitHub unavailable"),
                _ => new DailyFocusStalledReviewSnapshotDto(
                    [
                        new DailyFocusStalledReviewPullRequestDto(
                            "owner/repo",
                            12,
                            5,
                            "https://github.com/owner/repo/pull/12",
                            "Stalled review"),
                    ],
                    UsedInReviewColumn: true));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() => Assert.Contains("Unable to load pull requests awaiting review.", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("[data-testid='pm-workflow-daily-focus-stalled-reviews-retry']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo#12", cut.Markup);
            Assert.Contains("(5d)", cut.Markup);
        });

        await _stalledReviewService.Received(2).GetStalledReviewPullRequestsAsync(
            "PVT_board",
            3,
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenStallDaysIsConfigured_IntroUsesThreshold()
    {
        _settingsStorage.StoredJson = """{"capacity":8,"stallDays":5,"neglectDays":14,"excludedRepositories":[]}""";
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("awaiting review for 5 or more days", cut.Markup);
            Assert.Contains("PRs awaiting review 5+ days", cut.Markup);
            Assert.DoesNotContain("three or more days", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowTabSwitch_WhenReturningToDailyFocus_DoesNotReloadStalledReviews()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1));

        await using var ctx = CreateContext();
        var dailyFocus = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        dailyFocus.WaitForAssertion(() =>
            Assert.Contains("No pull requests have been awaiting review for 3 or more days.", dailyFocus.Markup));

        ctx.RenderPmWorkflowPage<PmWorkflowRepos>();
        ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        await _stalledReviewService.Received(1).GetStalledReviewPullRequestsAsync(
            "PVT_board",
            3,
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenLinkedBoardsAreInaccessible_ShowsWarning()
    {
        ConfigureDefaults();
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PmProjectBoardDiscoveryDto(
                [new PmPlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                2,
                1));
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto([], 0, 8, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("could not be loaded", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task PmWorkflowDailyFocus_WhenChromeLoadIsInFlight_ShowsLoadingIndicator()
    {
        var repositoriesReady = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(repositoriesReady.Task);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PmProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Loading PM Workflow", cut.Markup);
            Assert.Contains("Loading planning boards and occupancy.", cut.Markup);
        });

        repositoriesReady.SetResult([CreateRepository("owner", "repo-a")]);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Select a planning board in the dropdown above to load Daily Focus occupancy.", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowLayout_DisposeWhileStillOnPmWorkflow_DoesNotCancelChromeLoad()
    {
        var repositoriesReady = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(repositoriesReady.Task);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PmProjectBoardDiscoveryDto(
                [new PmPlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                1,
                0));
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1));

        await using var ctx = CreateContext();
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/pm-workflow/daily-focus");
        var coordinator = ctx.Services.GetRequiredService<PmWorkflowChromeCoordinator>();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() => Assert.Contains("Loading PM Workflow", cut.Markup));

        cut.Instance.Dispose();
        repositoriesReady.SetResult([CreateRepository("owner", "repo-a")]);

        cut.WaitForAssertion(() => Assert.True(coordinator.HasLoadedOnce));
    }

    [Fact]
    public async Task PmWorkflowTabSwitch_WhenChromeAlreadyLoaded_DoesNotFetchRepositoriesAgain()
    {
        ConfigureDefaults();
        _boardStateService.GetBoardStateAsync("PVT_board", 8, Arg.Any<CancellationToken>())
            .Returns(new DailyFocusBoardStateDto(
                [new DailyFocusOccupancyChipDto("Todo", 1)],
                ActiveLoad: 0,
                Capacity: 8,
                ItemCount: 1));

        await using var ctx = CreateContext();
        var dailyFocus = ctx.RenderPmWorkflowPage<PmWorkflowDailyFocus>();

        dailyFocus.WaitForAssertion(() => Assert.Contains("Active load: 0 / 8", dailyFocus.Markup));

        ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        await _repositoryService.Received(1).GetActiveRepositoriesAsync(Arg.Any<CancellationToken>());
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
        ctx.Services.AddScoped(_ => _boardStateService);
        ctx.Services.AddScoped(_ => _stalledReviewService);
        ctx.Services.AddScoped<PmWorkflowChromeCoordinator>();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

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
