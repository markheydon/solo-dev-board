using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.Workflows.Pages;
using SoloDevBoard.Application.Services.Workflows;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Workflows"/> page.</summary>
public sealed class WorkflowsTests
{
    private readonly Mock<IWorkflowTemplateService> _workflowTemplateServiceMock = new();

    [Fact]
    public async Task Workflows_WhileTemplatesAreLoading_ShowsLoadingState()
    {
        // Arrange
        var templatesTask = new TaskCompletionSource<IReadOnlyList<WorkflowTemplateDto>>();
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .Returns(templatesTask.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Workflows>();

        // Assert
        Assert.Contains("Loading workflow templates", cut.Markup);
    }

    [Fact]
    public async Task Workflows_InitialLoad_RendersBuiltInTemplateCards()
    {
        // Arrange
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplates());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Workflows>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='workflow-template-browser-region']"));
            Assert.Single(cut.FindAll("[data-testid='workflow-template-grid']"));
            Assert.Contains(".NET CI", cut.Markup);
            Assert.Contains("Azure CD (Aspire)", cut.Markup);
            Assert.Contains("Dependabot Auto-Merge", cut.Markup);
        });
    }

    [Fact]
    public async Task Workflows_SearchByTag_FiltersTemplateCards()
    {
        // Arrange
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplates());

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
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplates());

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
        _workflowTemplateServiceMock
            .Setup(service => service.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplates());

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

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestHostedAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _workflowTemplateServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

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
