using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Pages;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Audit"/> page.</summary>
public sealed class AuditTests
{
    private readonly Mock<IAuditDashboardService> _auditDashboardServiceMock = new();

    [Fact]
    public async Task Audit_WhileServiceIsLoading_ShowsLoadingSkeleton()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<RepositoryAuditSummaryDto>>();
        _auditDashboardServiceMock
            .Setup(service => service.GetRepositorySummaryAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();

        // Assert
        Assert.Single(cut.FindAll("[data-testid='audit-loading-state']"));
        Assert.Empty(cut.FindAll("[data-testid='audit-summary-table']"));
        Assert.Empty(cut.FindAll("[data-testid='audit-empty-state']"));
    }

    [Fact]
    public async Task Audit_WhenServiceReturnsNoRepositories_ShowsEmptyState()
    {
        // Arrange
        _auditDashboardServiceMock
            .Setup(service => service.GetRepositorySummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RepositoryAuditSummaryDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-empty-state']"));
            Assert.Contains("No repositories found", cut.Markup);
            Assert.Empty(cut.FindAll("[data-testid='audit-summary-table']"));
        });
    }

    [Fact]
    public async Task Audit_WhenServiceReturnsSummary_ShowsRowsTotalsAndRepositoryLinks()
    {
        // Arrange
        var summary = new List<RepositoryAuditSummaryDto>
        {
            new("owner/repo-b", 1, 2, 0, 0, 0),
            new("owner/repo-a", 4, 3, 1, 1, 0),
        };

        _auditDashboardServiceMock
            .Setup(service => service.GetRepositorySummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Audit>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='audit-summary-table']"));
            Assert.Contains("owner/repo-a", cut.Markup);
            Assert.Contains("owner/repo-b", cut.Markup);
            Assert.Contains("Total open issues", cut.Markup);
            Assert.Contains("Total open pull requests", cut.Markup);
            Assert.Contains(">5<", cut.Markup);
            Assert.Contains(">5<", cut.Markup);

            var links = cut.FindAll("a")
                .Select(link => link.GetAttribute("href"))
                .Where(href => !string.IsNullOrWhiteSpace(href))
                .ToList();

            Assert.Contains("https://github.com/owner/repo-a", links);
            Assert.Contains("https://github.com/owner/repo-b", links);
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => _auditDashboardServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }
}
