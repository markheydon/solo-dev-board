using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.ActionsTemplates.Pages;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.ActionsTemplates;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="ActionsTemplates"/> page.</summary>
public sealed class ActionsTemplatesTests
{
    private readonly IActionsTemplateService _workflowTemplateService = Substitute.For<IActionsTemplateService>();
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();

    [Fact]
    public async Task ActionsTemplates_WhileTemplatesAreLoading_ShowsLoadingState()
    {
        // Arrange
        var templatesTask = new TaskCompletionSource<IReadOnlyList<ActionsTemplateDto>>();
        _workflowTemplateService.GetTemplatesAsync(Arg.Any<CancellationToken>()).Returns(templatesTask.Task);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(CreateRepositories());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<ActionsTemplates>();

        // Assert
        Assert.Contains("Loading workflow templates", cut.Markup);
    }

    [Fact]
    public async Task ActionsTemplates_InitialLoad_RendersBuiltInTemplateCardsAndRepositorySelector()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<ActionsTemplates>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='repository-selector-region']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-browser-region']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-grid']"));
            Assert.Contains(".NET CI", cut.Markup);
            Assert.Contains("Azure CD (Aspire)", cut.Markup);
            Assert.Contains("Dependabot Auto-Merge", cut.Markup);
        });
    }

    [Fact]
    public async Task ActionsTemplates_SelectTemplateButton_UsesSemanticControlAndShowsSelectedState()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateService.GetTemplateDetailAsync(1, Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));

        // Act
        var selectButton = cut.Find("[data-testid='actions-templates-select-1']");
        await cut.InvokeAsync(() => selectButton.Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", selectButton.GetAttribute("aria-pressed"));
            Assert.Contains("Selected", selectButton.TextContent);
            Assert.Single(cut.FindAll("[data-testid='actions-templates-detail-region']"));
            Assert.Contains("YAML preview", cut.Markup);
        });
    }

    [Fact]
    public async Task ActionsTemplates_SelectTemplate_ShowsParameterFields()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateService.GetTemplateDetailAsync(1, Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-select-1']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-parameter-form']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-parameter-mainBranch']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-parameter-dotnetVersion']"));
        });
    }

    [Fact]
    public async Task ActionsTemplates_RequiredParameterCleared_DisablesApplyButton()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateService.GetTemplateDetailAsync(1, Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-select-1']").Click());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='actions-templates-parameter-mainBranch']")));

        // Act
        var mainBranchField = cut.FindComponents<MudTextField<string>>()
            .First(field => field.Instance.Label == "Main branch");
        await cut.InvokeAsync(() => mainBranchField.Instance.ValueChanged.InvokeAsync(string.Empty));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var applyButton = cut.Find("[data-testid='actions-templates-apply-button']");
            Assert.True(applyButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task ActionsTemplates_ApplyTemplate_ShowsSuccessFeedback()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateService.GetTemplateDetailAsync(1, Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        _workflowTemplateService.GetRepositoryStatusesAsync(1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns([
                new ActionsTemplateRepositoryStatusDto("owner/repo-a", ActionsTemplateApplicationStatus.NotApplied, "Workflow file is not present in this repository."),
            ]);

        _workflowTemplateService.ApplyTemplateAsync(1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns([
                new ActionsTemplateRepositoryResultDto("owner/repo-a", "Created", null),
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-select-1']").Click());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='workflow-repository-autocomplete']")));
        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();
        await cut.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-a"));

        cut.WaitForAssertion(() => Assert.Contains("owner/repo-a", cut.Markup));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-apply-button']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-feedback-region']"));
            Assert.Contains("Applied template successfully", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='actions-templates-apply-results']"));
        });
    }

    [Fact]
    public async Task ActionsTemplates_ApplyTemplateFailure_ShowsErrorFeedback()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateService.GetTemplateDetailAsync(1, Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        _workflowTemplateService.GetRepositoryStatusesAsync(1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns([
                new ActionsTemplateRepositoryStatusDto("owner/repo-a", ActionsTemplateApplicationStatus.NotApplied, "Workflow file is not present in this repository."),
            ]);

        _workflowTemplateService.ApplyTemplateAsync(1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns([
                new ActionsTemplateRepositoryResultDto("owner/repo-a", "Failed", "GitHub API request failed."),
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-select-1']").Click());

        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();
        await cut.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-a"));

        cut.WaitForAssertion(() => Assert.Contains("owner/repo-a", cut.Markup));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-apply-button']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("repository errors", cut.Markup);
            Assert.Contains("GitHub API request failed.", cut.Markup);
        });
    }

    [Fact]
    public async Task ActionsTemplates_SearchByTag_FiltersTemplateCards()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-card-']").Count));

        // Act
        var searchField = cut.Find("[data-testid='actions-templates-search']");
        searchField.Input("dependabot");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid^='actions-templates-card-']"));
            Assert.Contains("Dependabot Auto-Merge", cut.Markup);
            Assert.DoesNotContain("Azure CD (Aspire)", cut.Markup);
        });
    }

    [Fact]
    public async Task ActionsTemplates_CategoryFilter_ShowsOnlyMatchingTemplates()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-card-']").Count));

        // Act
        var ciChip = cut.Find("[data-testid='actions-templates-category-ci']");
        await cut.InvokeAsync(() => ciChip.Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid^='actions-templates-card-']"));
            Assert.Contains(".NET CI", cut.Markup);
            Assert.DoesNotContain("Dependabot Auto-Merge", cut.Markup);
        });
    }

    [Fact]
    public async Task ActionsTemplates_NoMatchingResults_ShowsEmptyState()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-card-']").Count));

        // Act
        var searchField = cut.Find("[data-testid='actions-templates-search']");
        searchField.Input("nonexistent-template");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-empty-state']"));
            Assert.Contains("No templates found", cut.Markup);
        });
    }

    private void SetupDefaultServices()
    {
        _workflowTemplateService.GetTemplatesAsync(Arg.Any<CancellationToken>()).Returns(CreateTemplates());

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(CreateRepositories());
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _workflowTemplateService);
        ctx.Services.AddScoped(_ => _repositoryService);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static IReadOnlyList<RepositoryDto> CreateRepositories()
        =>
        [
            new(1, "repo-a", "owner/repo-a", "Repository A", "https://github.com/owner/repo-a", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false),
            new(2, "repo-b", "owner/repo-b", "Repository B", "https://github.com/owner/repo-b", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false),
        ];

    private static ActionsTemplateDetailDto CreateTemplateDetail()
        => new(
            1,
            ".NET CI",
            "Build and test a .NET solution on every push and pull request to the main branch.",
            "CI",
            ["dotnet", "github-actions", "build", "test"],
            ".github/workflows/ci.yml",
            "Push and pull request to main",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "name: CI",
            [
                new ActionsTemplateParameterDto("mainBranch", "Main branch", "The branch that triggers CI builds.", "main", true),
                new ActionsTemplateParameterDto("dotnetVersion", ".NET version", "The .NET SDK version used by the workflow.", "10.0.x", false),
            ]);

    private static IReadOnlyList<ActionsTemplateDto> CreateTemplates()
        =>
        [
            new(
                1,
                ".NET CI",
                "Build and test a .NET solution on every push and pull request to the main branch.",
                "CI",
                ["dotnet", "github-actions", "build", "test"],
                ".github/workflows/ci.yml",
                "Push and pull request to main",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            new(
                2,
                "Azure CD (Aspire)",
                "Deploy the application to Azure Container Apps using Aspire after operator approval.",
                "CD",
                ["aspire", "azure", "deploy", "container-apps"],
                ".github/workflows/cd.yml",
                "Manual workflow dispatch",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            new(
                3,
                "Dependabot Auto-Merge",
                "Automatically enable auto-merge for low-risk Dependabot pull requests after metadata checks.",
                "Maintenance",
                ["dependabot", "security", "automation"],
                ".github/workflows/dependabot-auto-merge.yml",
                "Dependabot pull requests targeting main",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
        ];
}
