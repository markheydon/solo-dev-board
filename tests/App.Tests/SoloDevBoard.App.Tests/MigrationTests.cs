using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Features.Migration.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Migration"/> page.</summary>
public sealed class MigrationTests
{
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();
    private readonly Mock<IMigrationService> _migrationServiceMock = new();

    [Fact]
    public async Task Migration_ConflictStrategyOptions_RenderExplanatoryText()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Conflict behaviour", cut.Markup);
            Assert.Contains("Skip", cut.Markup);
            Assert.Contains("Overwrite", cut.Markup);
            Assert.Contains("Merge", cut.Markup);
        });
    }

    [Fact]
    public async Task Migration_PreviewClicked_UsesSelectedConflictStrategy()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sourceRepository, targetRepository]);

        _migrationServiceMock
            .Setup(service => service.PreviewMigrationAsync(
                "owner/repo-a",
                It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
                It.Is<MigrationScopeDto>(scope => scope.IncludeLabels && scope.IncludeMilestones),
                MigrationConflictStrategy.Overwrite,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationPreviewDto(
                MigrationConflictStrategy.Overwrite,
                [new LabelSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])],
                [new MilestoneSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])]));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, sourceRepository, targetRepository);

        var strategySelect = cut.FindComponents<MudSelect<MigrationConflictStrategy>>().Single();
        await cut.InvokeAsync(() => strategySelect.Instance.ValueChanged.InvokeAsync(MigrationConflictStrategy.Overwrite));

        var targetCheckboxes = cut.FindAll("[data-testid='migration-target-checkbox']");
        Assert.Equal(2, targetCheckboxes.Count);
        targetCheckboxes[1].Change(true);

        cut.Find("[data-testid='migration-preview-button']").Click();

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Migration preview (Overwrite)", cut.Markup));

        _migrationServiceMock.Verify(service => service.PreviewMigrationAsync(
            "owner/repo-a",
            It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
            It.Is<MigrationScopeDto>(scope => scope.IncludeLabels && scope.IncludeMilestones),
            MigrationConflictStrategy.Overwrite,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Migration_PreviewContainsNoActionableChanges_HidesApplyButton()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sourceRepository, targetRepository]);

        _migrationServiceMock
            .Setup(service => service.PreviewMigrationAsync(
                "owner/repo-a",
                It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
                It.IsAny<MigrationScopeDto>(),
                MigrationConflictStrategy.Skip,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationPreviewDto(
                MigrationConflictStrategy.Skip,
                [new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [],
                    [],
                    [],
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-b")])],
                [new MilestoneSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])]));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, sourceRepository, targetRepository);

        var targetCheckboxes = cut.FindAll("[data-testid='migration-target-checkbox']");
        Assert.Equal(2, targetCheckboxes.Count);
        targetCheckboxes[1].Change(true);

        cut.Find("[data-testid='migration-preview-button']").Click();

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Migration preview (Skip)", cut.Markup));
        Assert.Empty(cut.FindAll("[data-testid='migration-apply-button']"));
    }

    [Fact]
    public async Task Migration_PreviewContainsMilestoneChanges_RendersMilestoneDetailTable()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sourceRepository, targetRepository]);

        _migrationServiceMock
            .Setup(service => service.PreviewMigrationAsync(
                "owner/repo-a",
                It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
                It.IsAny<MigrationScopeDto>(),
                MigrationConflictStrategy.Merge,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationPreviewDto(
                MigrationConflictStrategy.Merge,
                [new LabelSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])],
                [new MilestoneSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new MilestoneDto(1, 1, "Sprint 12", "Delivery sprint", "open", DateTimeOffset.Parse("2026-04-30T00:00:00Z"), 0, 0)],
                    [],
                    [],
                    [])]));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, sourceRepository, targetRepository);

        var strategySelect = cut.FindComponents<MudSelect<MigrationConflictStrategy>>().Single();
        await cut.InvokeAsync(() => strategySelect.Instance.ValueChanged.InvokeAsync(MigrationConflictStrategy.Merge));

        var targetCheckboxes = cut.FindAll("[data-testid='migration-target-checkbox']");
        Assert.Equal(2, targetCheckboxes.Count);
        targetCheckboxes[1].Change(true);

        cut.Find("[data-testid='migration-preview-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Milestones to create", cut.Markup);
            Assert.Contains("Sprint 12", cut.Markup);
        });
    }

    [Fact]
    public async Task Migration_NoRepositoriesAvailable_ShowsEmptyStateMessage()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("No active repositories are available for migration.", cut.Markup));
    }

    [Fact]
    public async Task Migration_PreviewClickedWithoutRequiredSelection_DoesNotInvokePreviewService()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-preview-button']"));
        cut.Find("[data-testid='migration-preview-button']").Click();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='migration-preview-card']"));
        _migrationServiceMock.Verify(service => service.PreviewMigrationAsync(
            It.IsAny<string>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<MigrationScopeDto>(),
            It.IsAny<MigrationConflictStrategy>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Migration_ApplyClickedTwiceDuringPendingCall_InvokesApplyServiceOnce()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");
        var applyTaskSource = new TaskCompletionSource<MigrationResultDto>();

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sourceRepository, targetRepository]);

        _migrationServiceMock
            .Setup(service => service.PreviewMigrationAsync(
                "owner/repo-a",
                It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
                It.IsAny<MigrationScopeDto>(),
                MigrationConflictStrategy.Skip,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationPreviewDto(
                MigrationConflictStrategy.Skip,
                [new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High priority", "owner/repo-b")],
                    [],
                    [],
                    [])],
                [new MilestoneSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])]));

        _migrationServiceMock
            .Setup(service => service.ApplyMigrationAsync(
                "owner/repo-a",
                It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
                It.IsAny<MigrationScopeDto>(),
                MigrationConflictStrategy.Skip,
                It.IsAny<CancellationToken>()))
            .Returns(applyTaskSource.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, sourceRepository, targetRepository);

        var targetCheckboxes = cut.FindAll("[data-testid='migration-target-checkbox']");
        Assert.Equal(2, targetCheckboxes.Count);
        targetCheckboxes[1].Change(true);

        cut.Find("[data-testid='migration-preview-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-apply-button']"));

        var applyButton = cut.Find("[data-testid='migration-apply-button']");
        applyButton.Click();
        applyButton.Click();

        await cut.InvokeAsync(() => applyTaskSource.SetResult(new MigrationResultDto(
            MigrationConflictStrategy.Skip,
            [new LabelSyncRepositoryResultDto("owner/repo-b", 1, 0, 0, 0, null)],
            [])));

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Migration completed successfully", cut.Markup));
        _migrationServiceMock.Verify(service => service.ApplyMigrationAsync(
            "owner/repo-a",
            It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
            It.IsAny<MigrationScopeDto>(),
            MigrationConflictStrategy.Skip,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Migration_ApplyReturnsPartialFailure_RendersSummaryAndErrorDetails()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryServiceMock
            .Setup(service => service.GetActiveRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sourceRepository, targetRepository]);

        _migrationServiceMock
            .Setup(service => service.PreviewMigrationAsync(
                "owner/repo-a",
                It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
                It.IsAny<MigrationScopeDto>(),
                MigrationConflictStrategy.Merge,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationPreviewDto(
                MigrationConflictStrategy.Merge,
                [new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High priority", "owner/repo-b")],
                    [],
                    [],
                    [])],
                [new MilestoneSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new MilestoneDto(1, 2, "Sprint 2", "Delivery", "open", null, 0, 0)],
                    [],
                    [],
                    [])]));

        _migrationServiceMock
            .Setup(service => service.ApplyMigrationAsync(
                "owner/repo-a",
                It.Is<IReadOnlyList<string>>(targets => targets.SequenceEqual(new[] { "owner/repo-b" })),
                It.IsAny<MigrationScopeDto>(),
                MigrationConflictStrategy.Merge,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationResultDto(
                MigrationConflictStrategy.Merge,
                [new LabelSyncRepositoryResultDto("owner/repo-b", 1, 0, 0, 0, "GitHub label API rate limit reached")],
                [new MilestoneSyncRepositoryResultDto("owner/repo-b", 1, 0, 0, 0, null)]));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));
        await SelectRepositoriesAsync(cut, sourceRepository, targetRepository);

        var strategySelect = cut.FindComponents<MudSelect<MigrationConflictStrategy>>().Single();
        await cut.InvokeAsync(() => strategySelect.Instance.ValueChanged.InvokeAsync(MigrationConflictStrategy.Merge));

        var targetCheckboxes = cut.FindAll("[data-testid='migration-target-checkbox']");
        targetCheckboxes[1].Change(true);

        cut.Find("[data-testid='migration-preview-button']").Click();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-apply-button']"));
        cut.Find("[data-testid='migration-apply-button']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Migration summary (Merge)", cut.Markup);
            Assert.Contains("Label migration failed.", cut.Markup);
            Assert.Contains("GitHub label API rate limit reached", cut.Markup);
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => _repositoryServiceMock.Object);
        ctx.Services.AddScoped(_ => _migrationServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static async Task SelectRepositoriesAsync(IRenderedComponent<Migration> cut, params RepositoryDto[] repositories)
    {
        var selector = cut.FindComponent<RepositorySelector>();
        var selectedFullNames = repositories.Select(repository => repository.FullName).ToArray();

        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(selectedFullNames));
    }

    private static RepositoryDto CreateRepository(string owner, string name)
        => new(
            Id: 0,
            Name: name,
            FullName: $"{owner}/{name}",
            Description: string.Empty,
            Url: string.Empty,
            IsPrivate: false,
            IsArchived: false,
            CreatedAt: DateTimeOffset.UnixEpoch,
            UpdatedAt: DateTimeOffset.UnixEpoch);
}
