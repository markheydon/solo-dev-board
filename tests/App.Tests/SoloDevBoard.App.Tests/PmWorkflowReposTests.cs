using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoloDevBoard.App.Components.Features.PmWorkflow;
using SoloDevBoard.App.Components.Features.PmWorkflow.Pages;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for <see cref="PmWorkflowRepos"/>.</summary>
public sealed class PmWorkflowReposTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPmProjectBoardDiscoveryService>();
    private readonly IPmWorkItemCatalogueService _workItemCatalogueService = Substitute.For<IPmWorkItemCatalogueService>();
    private readonly FakePmSettingsStorage _settingsStorage = new();

    public PmWorkflowReposTests()
    {
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .Returns(new PmWorkItemCatalogueResultDto([], [], []));
    }

    [Fact]
    public async Task PmWorkflowRepos_OnFirstLoad_ShowsDefaultThresholdFields()
    {
        ConfigureDefaults();

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        cut.WaitForAssertion(() =>
        {
            var capacityField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Capacity limit", StringComparison.Ordinal));
#pragma warning disable MUD0012
            Assert.Equal(PmSettingsDefaults.Capacity, capacityField.Instance.Value);
            var stallDaysField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Stall days", StringComparison.Ordinal));
            Assert.Equal(PmSettingsDefaults.StallDays, stallDaysField.Instance.Value);
            var neglectDaysField = cut.FindComponents<MudNumericField<int>>()
                .First(component => component.Markup.Contains("Neglect days", StringComparison.Ordinal));
            Assert.Equal(PmSettingsDefaults.NeglectDays, neglectDaysField.Instance.Value);
#pragma warning restore MUD0012
        });
    }

    [Fact]
    public async Task PmWorkflowRepos_WhenNoBoardIsSelected_ShowsInstructionalAlert()
    {
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
            CreateRepository("owner", "repo-a"),
        ]);
        _projectBoardDiscoveryService.GetPlanningBoardOptionsForRepositoriesAsync(
            Arg.Any<IReadOnlyList<RepositoryDto>>(),
            Arg.Any<CancellationToken>()).Returns(
            new PmProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Repo Management", cut.Markup);
            Assert.Contains("Select a planning board", cut.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowRepos_Refresh_SyncsThresholdFieldsFromStorage()
    {
        ConfigureDefaults();
        _settingsStorage.StoredJson = """{"capacity":12,"stallDays":5,"neglectDays":21,"excludedRepositories":[]}""";

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

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
        cut.Find("[data-testid='pm-workflow-refresh-button']").Click();

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
    public async Task PmWorkflowRepos_IncludeRepository_PersistsAfterReload()
    {
        ConfigureDefaults();
        _settingsStorage.StoredJson = """{"excludedRepositories":["owner/repo-b"]}""";

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-b", cut.Markup);
            Assert.Contains("Include again", cut.Markup);
        });

        cut.Find("[data-testid='pm-workflow-include-button']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories are excluded. All active repositories participate.", cut.Markup);
            Assert.Contains("owner/repo-b", cut.Find("[data-testid='pm-workflow-included-table']").InnerHtml);
        });

        await using var reloadContext = CreateContext();
        var reload = reloadContext.RenderPmWorkflowPage<PmWorkflowRepos>();

        reload.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories are excluded. All active repositories participate.", reload.Markup);
            Assert.Contains("owner/repo-b", reload.Find("[data-testid='pm-workflow-included-table']").InnerHtml);
        });
    }

    [Fact]
    public async Task PmWorkflowRepos_ExcludeRepository_PersistsAfterReload()
    {
        ConfigureDefaults();

        await using var ctx = CreateContext();
        var firstRender = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        firstRender.WaitForAssertion(() => Assert.Contains("Repository participation", firstRender.Markup));

        var excludeAutocomplete = firstRender
            .FindComponents<MudAutocomplete<string>>()
            .First(component => component.Markup.Contains("Quick exclude", StringComparison.Ordinal));

        await firstRender.InvokeAsync(() => excludeAutocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-b"));
        firstRender.Find("[data-testid='pm-workflow-exclude-button']").Click();

        firstRender.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-b", firstRender.Markup);
            Assert.Contains("Include again", firstRender.Markup);
        });

        await using var reloadContext = CreateContext();
        var secondRender = reloadContext.RenderPmWorkflowPage<PmWorkflowRepos>();

        secondRender.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-b", secondRender.Markup);
            Assert.DoesNotContain("No repositories are excluded. All active repositories participate.", secondRender.Markup);
        });
    }

    [Fact]
    public async Task PmWorkflowRepos_RepositorySummary_ShowsCountsAndOmitsExcludedRepositories()
    {
        ConfigureDefaults();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>()).Returns(
            new PmWorkItemCatalogueResultDto(
                [],
                [],
                [
                    new PmRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow.AddDays(-2), true),
                    new PmRepositorySummaryDto("owner/repo-b", 1, 0, DateTimeOffset.UtcNow.AddDays(-1), true),
                ]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Per-repository summary", cut.Markup);
            Assert.Contains("owner/repo-a", cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml);
            Assert.Contains("12", cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml);
            Assert.Contains("2", cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml);
            Assert.Contains("Yes", cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml);
            Assert.Contains("owner/repo-b", cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml);
        });

        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>()).Returns(
            new PmWorkItemCatalogueResultDto(
                [],
                [],
                [new PmRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow.AddDays(-2), true)]));

        var excludeAutocomplete = cut
            .FindComponents<MudAutocomplete<string>>()
            .First(component => component.Markup.Contains("Quick exclude", StringComparison.Ordinal));

        await cut.InvokeAsync(() => excludeAutocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-b"));
        cut.Find("[data-testid='pm-workflow-exclude-button']").Click();

        cut.WaitForAssertion(() =>
        {
            var summaryTable = cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml;
            Assert.Contains("owner/repo-a", summaryTable);
            Assert.DoesNotContain("owner/repo-b", summaryTable);
        });
    }

    [Fact]
    public async Task PmWorkflowRepos_RepositorySummaryLoadFails_ShowsRetryAlert()
    {
        ConfigureDefaults();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Catalogue unavailable"));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load per-repository issue and pull request counts", cut.Markup);
            Assert.NotNull(cut.Find("[data-testid='pm-workflow-repository-summary-retry']"));
        });
    }

    [Fact]
    public async Task PmWorkflowRepos_PartialRepositoryFailure_ShowsWarningAndOmitsFailedRepository()
    {
        ConfigureDefaults();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>()).Returns(
            new PmWorkItemCatalogueResultDto(
                [],
                [new PmRepositoryCatalogueFailureDto("owner/repo-b", "Issues: Forbidden", 403)],
                [new PmRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow.AddDays(-2), true)]));

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Counts are unavailable", cut.Markup);
            Assert.Contains("Failed repositories are omitted from the table.", cut.Markup);
            Assert.NotNull(cut.Find("[data-testid='pm-workflow-repository-summary-partial-failure']"));
            Assert.NotNull(cut.Find("[data-testid='pm-workflow-repository-summary-partial-retry']"));

            var summaryTable = cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml;
            Assert.Contains("owner/repo-a", summaryTable);
            Assert.DoesNotContain("owner/repo-b", summaryTable);
        });
    }

    [Fact]
    public async Task PmWorkflowRepos_ExcludeWhileCatalogueLoadInFlight_CancelsPreviousLoadAndIgnoresStaleSummaries()
    {
        ConfigureDefaults();
        var loads = new List<(CancellationToken Token, TaskCompletionSource<PmWorkItemCatalogueResultDto> Completion)>();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var completion = new TaskCompletionSource<PmWorkItemCatalogueResultDto>();
                loads.Add((callInfo.Arg<CancellationToken>(), completion));
                return completion.Task;
            });

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        cut.WaitForAssertion(() => Assert.Single(loads));

        var excludeAutocomplete = cut
            .FindComponents<MudAutocomplete<string>>()
            .First(component => component.Markup.Contains("Quick exclude", StringComparison.Ordinal));

        await cut.InvokeAsync(() => excludeAutocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-b"));
        cut.Find("[data-testid='pm-workflow-exclude-button']").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, loads.Count));
        Assert.True(loads[0].Token.IsCancellationRequested);

        loads[0].Completion.SetResult(
            new PmWorkItemCatalogueResultDto(
                [],
                [],
                [
                    new PmRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow, true),
                    new PmRepositorySummaryDto("owner/repo-b", 1, 0, DateTimeOffset.UtcNow, true),
                ]));
        loads[1].Completion.SetResult(
            new PmWorkItemCatalogueResultDto(
                [],
                [],
                [new PmRepositorySummaryDto("owner/repo-a", 12, 2, DateTimeOffset.UtcNow, true)]));

        cut.WaitForAssertion(() =>
        {
            var summaryTable = cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml;
            Assert.Contains("owner/repo-a", summaryTable);
            Assert.DoesNotContain("owner/repo-b", summaryTable);
        });
    }

    [Fact]
    public async Task PmWorkflowRepos_SameRevisionWhileLoading_DoesNotStartSecondCatalogueLoad()
    {
        ConfigureDefaults();
        var completion = new TaskCompletionSource<PmWorkItemCatalogueResultDto>();
        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        await using var ctx = CreateContext();
        var cut = ctx.RenderPmWorkflowPage<PmWorkflowRepos>();

        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='pm-workflow-repository-summary-loading']")));

        cut.Render();

        await _workItemCatalogueService.Received(1).GetCatalogueAsync(Arg.Any<CancellationToken>());

        completion.SetResult(
            new PmWorkItemCatalogueResultDto(
                [],
                [],
                [new PmRepositorySummaryDto("owner/repo-a", 3, 1, DateTimeOffset.UtcNow, true)]));

        cut.WaitForAssertion(() =>
            Assert.Contains("owner/repo-a", cut.Find("[data-testid='pm-workflow-repository-summary-table']").InnerHtml));
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
            new PmProjectBoardDiscoveryDto(
                [new PmPlanningBoardOptionDto("PVT_board", "Roadmap", "owner", "status-field")],
                1,
                0));

        _workItemCatalogueService.GetCatalogueAsync(Arg.Any<CancellationToken>()).Returns(
            new PmWorkItemCatalogueResultDto([], [], []));
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
        ctx.Services.AddScoped(_ => _workItemCatalogueService);
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
