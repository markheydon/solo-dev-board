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

/// <summary>Component tests for <see cref="PmWorkflowRepos"/>.</summary>
public sealed class PmWorkflowReposTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IPmProjectBoardDiscoveryService _projectBoardDiscoveryService = Substitute.For<IPmProjectBoardDiscoveryService>();
    private readonly FakePmSettingsStorage _settingsStorage = new();

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
