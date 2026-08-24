using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoloDevBoard.App.Components.Features.Planning;
using SoloDevBoard.App.Components.Features.Planning.Pages;
using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for <see cref="PlanningRepos"/>.</summary>
public sealed class PlanningReposTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPlanningProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPlanningProjectBoardDiscoveryService>();
    private readonly IPlanningWorkItemCatalogueService _workItemCatalogueService = Substitute.For<IPlanningWorkItemCatalogueService>();
    private readonly FakePlanningSettingsStorage _settingsStorage = new();

    public PlanningReposTests()
    {
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .Returns(new PlanningWorkItemCatalogueResultDto([], [], []));
    }

    [Fact]
    public async Task PlanningRepos_OnFirstLoad_ShowsDefaultThresholdFields()
    {
        ConfigureDefaults();

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() =>
        {
            var capacityField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Capacity limit", StringComparison.Ordinal));
#pragma warning disable MUD0012
            Assert.Equal(PlanningSettingsDefaults.Capacity, capacityField.Instance.Value);
            var stallDaysField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Stall days", StringComparison.Ordinal));
            Assert.Equal(PlanningSettingsDefaults.StallDays, stallDaysField.Instance.Value);
            var neglectDaysField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Neglect days", StringComparison.Ordinal));
            Assert.Equal(PlanningSettingsDefaults.NeglectDays, neglectDaysField.Instance.Value);
#pragma warning restore MUD0012
        });
    }

    [Fact]
    public async Task PlanningRepos_WhenNoBoardIsSelected_ShowsInstructionalAlert()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Repo Management", cut.Markup);
            Assert.Contains("Select a planning board", cut.Markup);
        });
    }

    [Fact]
    public async Task PlanningRepos_Refresh_SyncsThresholdFieldsFromStorage()
    {
        ConfigureDefaults();
        _settingsStorage.StoredJson = """{"capacity":12,"stallDays":5,"neglectDays":21,"excludedRepositories":[]}""";

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() =>
        {
            var capacityField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Capacity limit", StringComparison.Ordinal));
#pragma warning disable MUD0012
            Assert.Equal(12, capacityField.Instance.Value);
            var stallDaysField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Stall days", StringComparison.Ordinal));
            Assert.Equal(5, stallDaysField.Instance.Value);
            var neglectDaysField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Neglect days", StringComparison.Ordinal));
            Assert.Equal(21, neglectDaysField.Instance.Value);
#pragma warning restore MUD0012
        });

        _settingsStorage.StoredJson = """{"capacity":15,"stallDays":7,"neglectDays":30,"excludedRepositories":[]}""";
        cut.Find("[data-testid='planning-refresh-button']").Click();

        cut.WaitForAssertion(() =>
        {
            var capacityField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Capacity limit", StringComparison.Ordinal));
#pragma warning disable MUD0012
            Assert.Equal(15, capacityField.Instance.Value);
            var stallDaysField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Stall days", StringComparison.Ordinal));
            Assert.Equal(7, stallDaysField.Instance.Value);
            var neglectDaysField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Neglect days", StringComparison.Ordinal));
            Assert.Equal(30, neglectDaysField.Instance.Value);
#pragma warning restore MUD0012
        });
    }

    [Fact]
    public async Task PlanningRepos_IncludeRepository_PersistsAfterReload()
    {
        ConfigureDefaults();
        _settingsStorage.StoredJson = """{"excludedRepositories":["owner/repo-b"]}""";

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-b", cut.Markup);
            Assert.Contains("Include again", cut.Markup);
        });

        cut.Find("[data-testid='planning-include-button']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories are excluded. All active repositories participate.", cut.Markup);
            Assert.Contains("owner/repo-b", cut.Find("[data-testid='planning-included-table']").InnerHtml);
        });

        await using var reloadContext = CreateContext();
        var reload = reloadContext.RenderPlanningPage<PlanningRepos>();

        reload.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories are excluded. All active repositories participate.", reload.Markup);
            Assert.Contains("owner/repo-b", reload.Find("[data-testid='planning-included-table']").InnerHtml);
        });
    }

    [Fact]
    public async Task PlanningRepos_ExcludeRepository_PersistsAfterReload()
    {
        ConfigureDefaults();

        await using var ctx = CreateContext();
        var firstRender = ctx.RenderPlanningPage<PlanningRepos>();

        firstRender.WaitForAssertion(() => Assert.Contains("Repository participation", firstRender.Markup));

        var excludeAutocomplete = firstRender
            .FindComponents<MudAutocomplete<string>>()
            .First(component => component.Markup.Contains("Quick exclude", StringComparison.Ordinal));

        await firstRender.InvokeAsync(() => excludeAutocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-b"));
        firstRender.Find("[data-testid='planning-exclude-button']").Click();

        firstRender.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-b", firstRender.Markup);
            Assert.Contains("Include again", firstRender.Markup);
        });

        await using var reloadContext = CreateContext();
        var secondRender = reloadContext.RenderPlanningPage<PlanningRepos>();

        secondRender.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-b", secondRender.Markup);
            Assert.DoesNotContain("No repositories are excluded. All active repositories participate.", secondRender.Markup);
        });
    }

    [Fact]
    public async Task PlanningRepos_RepositorySummary_ShowsCountsAndOmitsExcludedRepositories()
    {
        ConfigureDefaults();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>()).Returns(
            new PlanningWorkItemCatalogueResultDto(
                [],
                [],
                [
                    new PlanningRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow.AddDays(-2), true),
                    new PlanningRepositorySummaryDto("owner/repo-b", 1, 0, DateTimeOffset.UtcNow.AddDays(-1), true),
                ]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Per-repository summary", cut.Markup);
            Assert.Contains("owner/repo-a", cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml);
            Assert.Contains("12", cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml);
            Assert.Contains("2", cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml);
            Assert.Contains("Yes", cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml);
            Assert.Contains("owner/repo-b", cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml);
        });

        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>()).Returns(
            new PlanningWorkItemCatalogueResultDto(
                [],
                [],
                [new PlanningRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow.AddDays(-2), true)]));

        var excludeAutocomplete = cut
            .FindComponents<MudAutocomplete<string>>()
            .First(component => component.Markup.Contains("Quick exclude", StringComparison.Ordinal));

        await cut.InvokeAsync(() => excludeAutocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-b"));
        cut.Find("[data-testid='planning-exclude-button']").Click();

        cut.WaitForAssertion(() =>
        {
            var summaryTable = cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml;
            Assert.Contains("owner/repo-a", summaryTable);
            Assert.DoesNotContain("owner/repo-b", summaryTable);
        });
    }

    [Fact]
    public async Task PlanningRepos_RepositorySummaryLoadFails_ShowsRetryAlert()
    {
        ConfigureDefaults();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Catalogue unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load per-repository issue and pull request counts", cut.Markup);
            Assert.NotNull(cut.Find("[data-testid='planning-repository-summary-retry']"));
        });
    }

    [Fact]
    public async Task PlanningRepos_PartialRepositoryFailure_ShowsWarningAndOmitsFailedRepository()
    {
        ConfigureDefaults();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>()).Returns(
            new PlanningWorkItemCatalogueResultDto(
                [],
                [new PlanningRepositoryCatalogueFailureDto("owner/repo-b", "Issues: Forbidden", 403)],
                [new PlanningRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow.AddDays(-2), true)]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Counts are unavailable", cut.Markup);
            Assert.Contains("Failed repositories are omitted from the table.", cut.Markup);
            Assert.NotNull(cut.Find("[data-testid='planning-repository-summary-partial-failure']"));
            Assert.NotNull(cut.Find("[data-testid='planning-repository-summary-partial-retry']"));

            var summaryTable = cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml;
            Assert.Contains("owner/repo-a", summaryTable);
            Assert.DoesNotContain("owner/repo-b", summaryTable);
        });
    }

    [Fact]
    public async Task PlanningRepos_ExcludeWhileCatalogueLoadInFlight_CancelsPreviousLoadAndIgnoresStaleSummaries()
    {
        ConfigureDefaults();
        var loads = new List<(CancellationToken Token, TaskCompletionSource<PlanningWorkItemCatalogueResultDto> Completion)>();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var completion = new TaskCompletionSource<PlanningWorkItemCatalogueResultDto>();
                loads.Add((callInfo.Arg<CancellationToken>(), completion));
                return completion.Task;
            });

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() => Assert.Single(loads));

        var excludeAutocomplete = cut
            .FindComponents<MudAutocomplete<string>>()
            .First(component => component.Markup.Contains("Quick exclude", StringComparison.Ordinal));

        await cut.InvokeAsync(() => excludeAutocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-b"));
        cut.Find("[data-testid='planning-exclude-button']").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, loads.Count));
        Assert.True(loads[0].Token.IsCancellationRequested);

        loads[0].Completion.SetResult(
            new PlanningWorkItemCatalogueResultDto(
                [],
                [],
                [
                    new PlanningRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow, true),
                    new PlanningRepositorySummaryDto("owner/repo-b", 1, 0, DateTimeOffset.UtcNow, true),
                ]));
        loads[1].Completion.SetResult(
            new PlanningWorkItemCatalogueResultDto(
                [],
                [],
                [new PlanningRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow, true)]));

        cut.WaitForAssertion(() =>
        {
            var summaryTable = cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml;
            Assert.Contains("owner/repo-a", summaryTable);
            Assert.DoesNotContain("owner/repo-b", summaryTable);
        });
    }

    [Fact]
    public async Task PlanningRepos_SameRevisionWhileLoading_DoesNotStartSecondCatalogueLoad()
    {
        ConfigureDefaults();
        var completion = new TaskCompletionSource<PlanningWorkItemCatalogueResultDto>();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        await using var ctx = CreateContext();
        var cut = ctx.RenderPlanningPage<PlanningRepos>();

        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='planning-repository-summary-loading']")));

        cut.Render();

        await _workItemCatalogueService.Received(1).GetCatalogueAsync(Arg.Any<CancellationToken>());

        completion.SetResult(
            new PlanningWorkItemCatalogueResultDto(
                [],
                [],
                [new PlanningRepositorySummaryDto("owner/repo-a", 3, 1, DateTimeOffset.UtcNow, true)]));

        cut.WaitForAssertion(() =>
            Assert.Contains("owner/repo-a", cut.Find("[data-testid='planning-repository-summary-table']").InnerHtml));
    }

    private void ConfigureDefaults()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
            CreateRepository("owner", "repo-b"),
        ]);

        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PlanningProjectBoardDiscoveryDto(
                [new PlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                1,
                0));

        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>()).Returns(
            new PlanningWorkItemCatalogueResultDto([], [], []));
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddSingleton<IPlanningSettingsStorage>(_settingsStorage);
        ctx.Services.AddScoped<IPlanningSettingsService, PlanningSettingsService>();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _projectBoardDiscoveryService);
        ctx.Services.AddScoped(_ => _workItemCatalogueService);
        ctx.Services.AddScoped(_ => PlanningTestChromeDependencies.CreateBoardCompatibilityService());
        ctx.Services.AddScoped<PlanningChromeCoordinator>();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static RepositoryDto CreateRepository(string owner, string name) =>
        new(1, name, $"{owner}/{name}", string.Empty, $"https://github.com/{owner}/{name}", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false);

    private sealed class FakePlanningSettingsStorage : IPlanningSettingsStorage
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
