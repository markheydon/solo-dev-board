using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.Workflows.Pages;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Workflows;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Workflows"/> page.</summary>
public sealed class WorkflowsTests
{
    private readonly Mock<IWorkflowTemplateService> _workflowTemplateServiceMock = new();
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();

    [Fact]
    public async Task Workflows_WhileTemplatesAreLoading_ShowsLoadingState()
    {
        // Arrange
        var templatesTask = new TaskCompletionSource<IReadOnlyList<WorkflowTemplateDto>>();
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .Returns(templatesTask.Task);

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRepositories());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Workflows>();

        // Assert
        Assert.Contains("Loading workflow templates", cut.Markup);
    }

    [Fact]
    public async Task Workflows_InitialLoad_RendersBuiltInTemplateCardsAndRepositorySelector()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Workflows>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='repository-selector-region']"));
            Assert.Single(cut.FindAll("[data-testid='workflow-template-browser-region']"));
            Assert.Single(cut.FindAll("[data-testid='workflow-template-grid']"));
            Assert.Contains(".NET CI", cut.Markup);
            Assert.Contains("Azure CD (Aspire)", cut.Markup);
            Assert.Contains("Dependabot Auto-Merge", cut.Markup);
        });
    }

    [Fact]
    public async Task Workflows_SelectTemplateButton_UsesSemanticControlAndShowsSelectedState()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplateDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<Workflows>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='workflow-template-select-']").Count));

        // Act
        var selectButton = cut.Find("[data-testid='workflow-template-select-1']");
        await cut.InvokeAsync(() => selectButton.Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", selectButton.GetAttribute("aria-pressed"));
            Assert.Contains("Selected", selectButton.TextContent);
            Assert.Single(cut.FindAll("[data-testid='workflow-template-detail-region']"));
            Assert.Contains("YAML preview", cut.Markup);
        });
    }

    [Fact]
    public async Task Workflows_SelectTemplate_ShowsParameterFields()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplateDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<Workflows>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='workflow-template-select-']").Count));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='workflow-template-select-1']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='workflow-template-parameter-form']"));
            Assert.Single(cut.FindAll("[data-testid='workflow-template-parameter-mainBranch']"));
            Assert.Single(cut.FindAll("[data-testid='workflow-template-parameter-dotnetVersion']"));
        });
    }

    [Fact]
    public async Task Workflows_RequiredParameterCleared_DisablesApplyButton()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplateDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplateDetail());

        await using var ctx = CreateContext();
        var cut = ctx.Render<Workflows>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='workflow-template-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='workflow-template-select-1']").Click());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='workflow-template-parameter-mainBranch']")));

        // Act
        var mainBranchField = cut.FindComponents<MudTextField<string>>()
            .First(field => field.Instance.Label == "Main branch");
        await cut.InvokeAsync(() => mainBranchField.Instance.ValueChanged.InvokeAsync(string.Empty));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var applyButton = cut.Find("[data-testid='workflow-template-apply-button']");
            Assert.True(applyButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task Workflows_ApplyTemplate_ShowsSuccessFeedback()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplateDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplateDetail());

        _workflowTemplateServiceMock
            .Setup(service => service.GetRepositoryStatusesAsync(1, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new WorkflowTemplateRepositoryStatusDto("owner/repo-a", WorkflowTemplateApplicationStatus.NotApplied, "Workflow file is not present in this repository."),
            ]);

        _workflowTemplateServiceMock
            .Setup(service => service.ApplyTemplateAsync(1, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new WorkflowTemplateRepositoryResultDto("owner/repo-a", "Created", null),
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Workflows>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='workflow-template-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='workflow-template-select-1']").Click());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='workflow-repository-autocomplete']")));
        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();
        await cut.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-a"));

        cut.WaitForAssertion(() => Assert.Contains("owner/repo-a", cut.Markup));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='workflow-template-apply-button']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='workflow-template-feedback-region']"));
            Assert.Contains("Applied template successfully", cut.Markup);
            Assert.Single(cut.FindAll("[data-testid='workflow-template-apply-results']"));
        });
    }

    [Fact]
    public async Task Workflows_ApplyTemplateFailure_ShowsErrorFeedback()
    {
        // Arrange
        SetupDefaultServices();
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplateDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplateDetail());

        _workflowTemplateServiceMock
            .Setup(service => service.GetRepositoryStatusesAsync(1, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new WorkflowTemplateRepositoryStatusDto("owner/repo-a", WorkflowTemplateApplicationStatus.NotApplied, "Workflow file is not present in this repository."),
            ]);

        _workflowTemplateServiceMock
            .Setup(service => service.ApplyTemplateAsync(1, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new WorkflowTemplateRepositoryResultDto("owner/repo-a", "Failed", "GitHub API request failed."),
            ]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Workflows>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='workflow-template-select-']").Count));
        await cut.InvokeAsync(() => cut.Find("[data-testid='workflow-template-select-1']").Click());

        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();
        await cut.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("owner/repo-a"));

        cut.WaitForAssertion(() => Assert.Contains("owner/repo-a", cut.Markup));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='workflow-template-apply-button']").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("repository errors", cut.Markup);
            Assert.Contains("GitHub API request failed.", cut.Markup);
        });
    }

    [Fact]
    public async Task Workflows_SearchByTag_FiltersTemplateCards()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<Workflows>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='workflow-template-card-']").Count));

        // Act
        var searchField = cut.Find("[data-testid='workflow-template-search']");
        searchField.Input("dependabot");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid^='workflow-template-card-']"));
            Assert.Contains("Dependabot Auto-Merge", cut.Markup);
            Assert.DoesNotContain("Azure CD (Aspire)", cut.Markup);
        });
    }

    [Fact]
    public async Task Workflows_CategoryFilter_ShowsOnlyMatchingTemplates()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<Workflows>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='workflow-template-card-']").Count));

        // Act
        var ciChip = cut.Find("[data-testid='workflow-template-category-ci']");
        await cut.InvokeAsync(() => ciChip.Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid^='workflow-template-card-']"));
            Assert.Contains(".NET CI", cut.Markup);
            Assert.DoesNotContain("Dependabot Auto-Merge", cut.Markup);
        });
    }

    [Fact]
    public async Task Workflows_NoMatchingResults_ShowsEmptyState()
    {
        // Arrange
        SetupDefaultServices();

        await using var ctx = CreateContext();
        var cut = ctx.Render<Workflows>();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid^='workflow-template-card-']").Count));

        // Act
        var searchField = cut.Find("[data-testid='workflow-template-search']");
        searchField.Input("nonexistent-template");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='workflow-templates-empty-state']"));
            Assert.Contains("No templates found", cut.Markup);
        });
    }

    private void SetupDefaultServices()
    {
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplates());

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRepositories());
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestHostedAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _workflowTemplateServiceMock.Object);
        ctx.Services.AddScoped(_ => _repositoryServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static IReadOnlyList<RepositoryDto> CreateRepositories()
        =>
        [
            new(1, "repo-a", "owner/repo-a", "Repository A", "https://github.com/owner/repo-a", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new(2, "repo-b", "owner/repo-b", "Repository B", "https://github.com/owner/repo-b", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
        ];

    private static WorkflowTemplateDetailDto CreateTemplateDetail()
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
                new WorkflowTemplateParameterDto("mainBranch", "Main branch", "The branch that triggers CI builds.", "main", true),
                new WorkflowTemplateParameterDto("dotnetVersion", ".NET version", "The .NET SDK version used by the workflow.", "10.0.x", false),
            ]);

    private static IReadOnlyList<WorkflowTemplateDto> CreateTemplates()
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
