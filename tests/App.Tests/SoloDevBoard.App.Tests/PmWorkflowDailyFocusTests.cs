using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoloDevBoard.App.Components.Features.PmWorkflow.Pages;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for Daily Focus occupancy on <see cref="PmWorkflowDailyFocus"/>.</summary>
public sealed class PmWorkflowDailyFocusTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPmProjectBoardDiscoveryService>();
    private readonly IDailyFocusBoardStateService _boardStateService = Substitute.For<IDailyFocusBoardStateService>();
    private readonly FakePmSettingsStorage _settingsStorage = new();

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
        var cut = ctx.Render<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Daily Focus", cut.Markup);
            Assert.Contains("Select a planning board to load Daily Focus occupancy.", cut.Markup);
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
        var cut = ctx.Render<PmWorkflowDailyFocus>();

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
        var cut = ctx.Render<PmWorkflowDailyFocus>();

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
        var cut = ctx.Render<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load board occupancy.", cut.Markup);
            Assert.Contains("Retry", cut.Markup);
        });
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
        var cut = ctx.Render<PmWorkflowDailyFocus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("could not be loaded", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
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
