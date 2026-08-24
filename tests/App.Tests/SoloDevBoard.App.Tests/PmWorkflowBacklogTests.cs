using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.PmWorkflow;
using SoloDevBoard.App.Components.Features.PmWorkflow.Pages;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for Backlog Review on <see cref="PmWorkflowBacklog"/>.</summary>
public sealed class PmWorkflowBacklogTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPmProjectBoardDiscoveryService>();
    private readonly IBacklogReviewService _backlogReviewService = Substitute.For<IBacklogReviewService>();
    private readonly FakePmSettingsStorage _settingsStorage = new()
    {
        StoredJson = """{"planningBoardNodeId":"PVT_board","capacity":8,"stallDays":3,"neglectDays":14,"excludedRepositories":[]}""",
    };

    [Fact]
    public async Task PmWorkflowTabSwitch_WhenReturningToBacklog_DoesNotReloadBacklog()
    {
        ConfigureDefaults();
        _backlogReviewService.GetBacklogAsync("PVT_board", Arg.Any<CancellationToken>()).Returns(
            new BacklogReviewResultDto(
                [
                    new BacklogReviewItemDto(
                        PmWorkItemTypeDto.Issue,
                        "owner/repo-a",
                        10,
                        "Urgent story",
                        "https://github.com/owner/repo-a/issues/10",
                        ["priority/high"],
                        "priority/high",
                        null),
                ],
                [],
                [],
                [],
                [],
                [],
                false,
                []));

        await using var ctx = CreateContext();
        var backlog = ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        backlog.WaitForAssertion(() =>
            Assert.Contains("data-testid=\"pm-workflow-backlog-urgent\"", backlog.Markup));

        ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();
        ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();

        await _backlogReviewService.Received(1).GetBacklogAsync(
            "PVT_board",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PmWorkflowTabSwitch_WhenReturningToPlanning_DoesNotReloadPlanning()
    {
        ConfigureDefaults();
        var planningService = Substitute.For<IIterationPlanningService>();
        planningService.GetPlanningViewAsync("PVT_board", 8, 3, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
            new IterationPlanningViewDto([], [], [], true, 1, 0, 8, false, []));

        await using var ctx = CreateContext(planningService);
        var planning = ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        planning.WaitForAssertion(() =>
            Assert.Contains("data-testid=\"pm-workflow-planning-candidates\"", planning.Markup));

        ctx.RenderPmWorkflowPage<PmWorkflowBacklog>();
        ctx.RenderPmWorkflowPage<PmWorkflowPlanning>();

        await planningService.Received(1).GetPlanningViewAsync(
            "PVT_board",
            8,
            3,
            false,
            Arg.Any<CancellationToken>());
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

    private BunitContext CreateContext(IIterationPlanningService? planningService = null)
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
        ctx.Services.AddScoped(_ => planningService ?? Substitute.For<IIterationPlanningService>());
        ctx.Services.AddScoped(_ => PmWorkflowTestChromeDependencies.CreateBoardCompatibilityService());
        ctx.Services.AddScoped<PmWorkflowChromeCoordinator>();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static RepositoryDto CreateRepository(string owner, string name) =>
        new(1, name, $"{owner}/{name}", string.Empty, $"https://github.com/{owner}/{name}", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false);

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
