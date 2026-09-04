using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.ActionsTemplates;
using SoloDevBoard.App.Components.Features.ActionsTemplates.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.ActionsTemplates;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="ActionsTemplates"/> page.</summary>
public sealed class ActionsTemplatesTests
{
    private readonly IActionsTemplateService _workflowTemplateService = Substitute.For<IActionsTemplateService>();
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IActionsTemplateSourceStorage _actionsTemplateSourceStorage = Substitute.For<IActionsTemplateSourceStorage>();
    private IRenderedComponent<MudSnackbarProvider> _snackbarProvider = default!;

    [Fact]
    public async Task ActionsTemplates_WhileTemplatesAreLoading_ShowsLoadingState()
    {
        // Arrange
        var templatesTask = new TaskCompletionSource<ActionsTemplateCatalogueDto>();
        _workflowTemplateService.GetTemplatesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(templatesTask.Task);

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(CreateRepositories());

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
            Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-region']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-autocomplete']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-field']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-browser-region']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-grid']"));
            Assert.Contains(".NET CI", cut.Markup);
            Assert.Contains("Azure CD (Aspire)", cut.Markup);
            Assert.Contains("Dependabot Auto-Merge", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='actions-templates-source-badge-builtin:1']"));
            Assert.Contains("Built-in", cut.Markup);
        });
    }

    [Fact]
    public async Task ActionsTemplates_SelectTemplateButton_UsesSemanticControlAndShowsSelectedState()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateService.GetTemplateDetailAsync("builtin:1", Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));

        // Act
        var selectButton = cut.Find("[data-testid='actions-templates-select-builtin:1']");
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
        _workflowTemplateService.GetTemplateDetailAsync("builtin:1", Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-select-builtin:1']").Click());

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
        _workflowTemplateService.GetTemplateDetailAsync("builtin:1", Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-select-builtin:1']").Click());

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
        _workflowTemplateService.GetTemplateDetailAsync("builtin:1", Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        _workflowTemplateService.GetRepositoryStatusesAsync("builtin:1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns([
                new ActionsTemplateRepositoryStatusDto("owner/repo-a", ActionsTemplateApplicationStatus.NotApplied, "Workflow file is not present in this repository."),
            ]);

        _workflowTemplateService.ApplyTemplateAsync("builtin:1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns([
                new ActionsTemplateRepositoryResultDto("owner/repo-a", "Created", null),
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-select-builtin:1']").Click());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='workflow-repository-autocomplete']")));
        var autocomplete = cut.FindComponents<MudAutocomplete<string>>()
            .First(component => component.Instance.Label == "Repositories");
        await cut.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-a"));

        cut.WaitForAssertion(() => Assert.Contains("owner/repo-a", cut.Markup));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-apply-button']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-feedback-region']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-apply-results']"));
        });
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Applied template successfully"));
    }

    [Fact]
    public async Task ActionsTemplates_ApplyTemplateFailure_ShowsErrorFeedback()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateService.GetTemplateDetailAsync("builtin:1", Arg.Any<CancellationToken>()).Returns(CreateTemplateDetail());

        _workflowTemplateService.GetRepositoryStatusesAsync("builtin:1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns([
                new ActionsTemplateRepositoryStatusDto("owner/repo-a", ActionsTemplateApplicationStatus.NotApplied, "Workflow file is not present in this repository."),
            ]);

        _workflowTemplateService.ApplyTemplateAsync("builtin:1", Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>()).Returns([
                new ActionsTemplateRepositoryResultDto("owner/repo-a", "Failed", "GitHub API request failed."),
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-select-builtin:1']").Click());

        var autocomplete = cut.FindComponents<MudAutocomplete<string>>()
            .First(component => component.Instance.Label == "Repositories");
        await cut.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-a"));

        cut.WaitForAssertion(() => Assert.Contains("owner/repo-a", cut.Markup));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-apply-button']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "repository errors");
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
    public async Task ActionsTemplates_CustomSourceError_ShowsErrorAlert()
    {
        // Arrange
        _workflowTemplateService
            .GetTemplatesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ActionsTemplateCatalogueDto
            {
                Templates = CreateTemplates(),
                CustomSourceError = "Repository 'owner/missing' was not found or is not accessible.",
            });

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(CreateRepositories());

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-error']"));
            Assert.Contains("was not found or is not accessible", cut.Markup);
            Assert.Equal(3, cut.FindAll("[data-testid^='actions-templates-card-']").Count);
            Assert.Single(cut.FindAll("[data-testid='actions-templates-source-badge-builtin:1']"));
        });
    }

    [Fact]
    public async Task ActionsTemplates_CustomSourceWarning_ShowsWarningAlert()
    {
        // Arrange
        _workflowTemplateService
            .GetTemplatesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ActionsTemplateCatalogueDto
            {
                Templates = CreateTemplates(),
                CustomSourceWarning = "Could not load 1 workflow file(s) from 'owner/template-repo': .github/workflows/missing.yml.",
                SkippedWorkflowPaths = [".github/workflows/missing.yml"],
            });

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(CreateRepositories());

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-warning']"));
            Assert.Contains("Could not load 1 workflow file(s)", cut.Markup);
        });
    }

    [Fact]
    public async Task ActionsTemplates_RestoreLastUsedSource_LoadsCatalogueWithStoredSource()
    {
        // Arrange
        _actionsTemplateSourceStorage.GetLastUsedSourceAsync().Returns("owner/template-repo");
        _workflowTemplateService
            .GetTemplatesAsync("owner/template-repo", Arg.Any<CancellationToken>())
            .Returns(new ActionsTemplateCatalogueDto { Templates = CreateTemplates() });
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(CreateRepositories());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<ActionsTemplates>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/template-repo", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-last-used-caption']"));
            Assert.Contains(LastUsedSourceCaptionText, cut.Markup);
        });

        await _workflowTemplateService.Received(1).GetTemplatesAsync("owner/template-repo", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActionsTemplates_RestoreLastUsedSourceInCatalogue_PreSelectsSourceSelector()
    {
        // Arrange
        _actionsTemplateSourceStorage.GetLastUsedSourceAsync().Returns("owner/repo-a");
        _workflowTemplateService
            .GetTemplatesAsync("owner/repo-a", Arg.Any<CancellationToken>())
            .Returns(new ActionsTemplateCatalogueDto { Templates = CreateTemplates() });
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(CreateRepositories());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<ActionsTemplates>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-a", cut.Find("[data-testid='selected-repositories']").TextContent);
            Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-last-used-caption']"));
        });

        await _workflowTemplateService.Received(1).GetTemplatesAsync("owner/repo-a", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActionsTemplates_CustomSourceSelectorSelection_SyncsManualFieldWithoutLoading()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-autocomplete']")));

        // Act
        var sourceAutocomplete = cut.FindComponents<MudAutocomplete<string>>()
            .First(autocomplete => autocomplete.Instance.Label == "Source repository");
        await cut.InvokeAsync(() => sourceAutocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-a"));

        // Assert — load enables and chip appears without an explicit load call.
        cut.WaitForAssertion(() =>
        {
            var loadButton = cut.Find("[data-testid='actions-templates-custom-source-load']");
            Assert.False(loadButton.HasAttribute("disabled"));
            Assert.Contains("owner/repo-a", cut.Find("[data-testid='selected-repositories']").TextContent);
        });

        await _workflowTemplateService.DidNotReceive().GetTemplatesAsync("owner/repo-a", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActionsTemplates_ManualSourceMatchingCatalogue_SelectsSourceSelector()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-field']")));

        // Act
        var sourceField = cut.Find("[data-testid='actions-templates-custom-source-field']");
        sourceField.Change("owner/repo-b");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner/repo-b", cut.Find("[data-testid='selected-repositories']").TextContent);
        });
    }

    [Fact]
    public async Task ActionsTemplates_ManualSourceOutsideCatalogue_ClearsSourceSelector()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-autocomplete']")));

        var sourceAutocomplete = cut.FindComponents<MudAutocomplete<string>>()
            .First(autocomplete => autocomplete.Instance.Label == "Source repository");
        await cut.InvokeAsync(() => sourceAutocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-a"));

        cut.WaitForAssertion(() => Assert.Contains("owner/repo-a", cut.Find("[data-testid='selected-repositories']").TextContent));

        // Act
        var sourceField = cut.Find("[data-testid='actions-templates-custom-source-field']");
        sourceField.Change("other/external-repo");

        // Assert
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid='selected-repositories']")));
    }

    [Fact]
    public async Task ActionsTemplates_LoadCustomSource_CallsServiceWithRepositoryAndShowsCustomBadge()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateService
            .GetTemplatesAsync("owner/template-repo", Arg.Any<CancellationToken>())
            .Returns(new ActionsTemplateCatalogueDto
            {
                Templates =
                [
                    ..CreateTemplates(),
                    new(
                        "custom:owner/template-repo:.github/workflows/deploy.yml",
                        "Deploy",
                        "Deploy workflow",
                        "Custom",
                        ["deploy"],
                        ".github/workflows/deploy.yml",
                        "Manual workflow dispatch",
                        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        "owner/template-repo"),
                ],
            });

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='actions-templates-custom-source-field']")));

        // Act
        var sourceField = cut.Find("[data-testid='actions-templates-custom-source-field']");
        sourceField.Change("owner/template-repo");
        await cut.InvokeAsync(() => cut.Find("[data-testid='actions-templates-custom-source-load']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-source-badge-custom:owner/template-repo:.github/workflows/deploy.yml']"));
            Assert.Contains("owner/template-repo", cut.Markup);
        });

        await _actionsTemplateSourceStorage.Received(1).SetLastUsedSourceAsync("owner/template-repo");
        await _workflowTemplateService.Received(1).GetTemplatesAsync("owner/template-repo", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActionsTemplates_LoadCustomSource_DisabledWhenFieldEmpty()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var loadButton = cut.Find("[data-testid='actions-templates-custom-source-load']");
            Assert.True(loadButton.HasAttribute("disabled"));
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

    [Fact]
    public async Task ActionsTemplates_AfterRepositoriesLoad_ShowsReloadFromGitHubButton()
    {
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-reload-from-github-button']"));
            Assert.Contains("Reload from GitHub", cut.Markup);
        });
    }

    [Fact]
    public async Task ActionsTemplates_ReloadFromGitHub_KeepsRepositorySelectionAndForceReloadsCatalogue()
    {
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='workflow-repository-autocomplete']")));

        var selector = cut.FindComponent<RepositorySelector>();
        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(["owner/repo-a", "owner/repo-b"]));

        cut.WaitForAssertion(() => Assert.Contains("2 selected", cut.Markup));

        await cut.Find("[data-testid='actions-templates-reload-from-github-button']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() => Assert.Contains("2 selected", cut.Markup));

        await _repositoryService.Received(1).GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), true);
    }

    [Fact]
    public async Task ActionsTemplates_RepositoryLoadFailure_ShowsTryAgainButton()
    {
        _actionsTemplateSourceStorage.GetLastUsedSourceAsync().Returns((string?)null);
        _workflowTemplateService.GetTemplatesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ActionsTemplateCatalogueDto { Templates = CreateTemplates() });
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Task.FromException<IReadOnlyList<RepositoryDto>>(new HttpRequestException("Connection refused")));

        await using var ctx = CreateContext();
        var cut = ctx.Render<ActionsTemplates>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='actions-templates-reload-repositories-button']"));
            Assert.Single(cut.FindAll("[data-testid='actions-templates-reload-from-github-button']"));
            Assert.Contains("Try again", cut.Markup);
        });
    }

    private void SetupDefaultServices()
    {
        _actionsTemplateSourceStorage.GetLastUsedSourceAsync().Returns((string?)null);
        _workflowTemplateService.GetTemplatesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new ActionsTemplateCatalogueDto { Templates = CreateTemplates() });

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(CreateRepositories());
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _workflowTemplateService);
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _actionsTemplateSourceStorage);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        _snackbarProvider = ctx.Render<MudSnackbarProvider>();

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
            "builtin:1",
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

    private const string LastUsedSourceCaptionText = "Last-used source restored from browser storage.";

    private static IReadOnlyList<ActionsTemplateDto> CreateTemplates()
        =>
        [
            new(
                "builtin:1",
                ".NET CI",
                "Build and test a .NET solution on every push and pull request to the main branch.",
                "CI",
                ["dotnet", "github-actions", "build", "test"],
                ".github/workflows/ci.yml",
                "Push and pull request to main",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "Built-in"),
            new(
                "builtin:2",
                "Azure CD (Aspire)",
                "Deploy the application to Azure Container Apps using Aspire after operator approval.",
                "CD",
                ["aspire", "azure", "deploy", "container-apps"],
                ".github/workflows/cd.yml",
                "Manual workflow dispatch",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "Built-in"),
            new(
                "builtin:3",
                "Dependabot Auto-Merge",
                "Automatically enable auto-merge for low-risk Dependabot pull requests after metadata checks.",
                "Maintenance",
                ["dependabot", "security", "automation"],
                ".github/workflows/dependabot-auto-merge.yml",
                "Dependabot pull requests targeting main",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "Built-in"),
        ];
}
