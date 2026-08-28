using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.Migration.Pages;
using SoloDevBoard.App.Components.Shared.Components;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Migration"/> page.</summary>
public sealed class MigrationTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IMigrationService _migrationService = Substitute.For<IMigrationService>();
    private IRenderedComponent<MudSnackbarProvider> _snackbarProvider = default!;

    [Fact]
    public async Task Migration_RepositoryLoadFailure_ShowsInlineErrorWithRetry()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<RepositoryDto>>(new HttpRequestException("Bad gateway", null, HttpStatusCode.BadGateway)));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("[data-testid='migration-repositories-load-error']"));
            Assert.Contains("GitHub API request failed while loading repositories.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Try loading repositories again", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Migration_ProjectBoardColumnsScopeSwitch_RendersInScopeCard()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("[data-testid='migration-scope-columns-switch']"));
            Assert.Contains("Project board columns", cut.Markup);
        });
    }

    [Fact]
    public async Task Migration_RapidSourceRepositoryChange_IgnoresStaleProjectBoardResponse()
    {
        // Arrange
        var repositoryA = CreateRepository("owner", "repo-a");
        var repositoryB = CreateRepository("owner", "repo-b");
        var repositoryC = CreateRepository("owner", "repo-c");
        var repositoryADiscoveryTask = new TaskCompletionSource<MigrationProjectBoardDiscoveryDto>();

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([repositoryA, repositoryB, repositoryC]);

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-a", Arg.Any<CancellationToken>()).Returns(repositoryADiscoveryTask.Task);

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-b", Arg.Any<CancellationToken>()).Returns(
            new MigrationProjectBoardDiscoveryDto(
                [new MigrationProjectBoardOptionDto("PVT_beta", "Beta Board")],
                1,
                0));

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-c", Arg.Any<CancellationToken>()).Returns(
            new MigrationProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, repositoryA, repositoryB, repositoryC);
        await EnableProjectBoardColumnsScopeAsync(cut);

        await ChangeSourceRepositoryAsync(cut, repositoryB.FullName);
        cut.WaitForAssertion(() => Assert.Contains("Beta Board", cut.Markup));

        repositoryADiscoveryTask.SetResult(new MigrationProjectBoardDiscoveryDto(
            [new MigrationProjectBoardOptionDto("PVT_alpha", "Alpha Board")],
            1,
            0));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Beta Board", cut.Markup);
            Assert.DoesNotContain("Alpha Board", cut.Markup);
        });
    }

    [Fact]
    public async Task Migration_TargetBoardsLoading_HidesTargetSelectors()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");
        var targetBoardsTask = new TaskCompletionSource<MigrationProjectBoardDiscoveryDto>();

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-a", Arg.Any<CancellationToken>()).Returns(
            new MigrationProjectBoardDiscoveryDto([], 0, 0));

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-b", Arg.Any<CancellationToken>()).Returns(targetBoardsTask.Task);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, sourceRepository, targetRepository);
        await EnableProjectBoardColumnsScopeAsync(cut);
        var enableTargetTask = EnableTargetRepositoryAsync(cut, targetRepository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("[data-testid='migration-target-boards-loading-state']"));
            Assert.Empty(cut.FindAll("[data-testid='migration-target-board-selector']"));
        });

        targetBoardsTask.SetResult(new MigrationProjectBoardDiscoveryDto([], 0, 0));
        await enableTargetTask;

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='migration-target-board-selector']")));
    }

    [Fact]
    public async Task Migration_ProjectBoardColumnsEnabled_PreviewLockedUntilBoardsChosen()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-a", Arg.Any<CancellationToken>()).Returns(
            new MigrationProjectBoardDiscoveryDto(
                [
                    new MigrationProjectBoardOptionDto("PVT_alpha", "Alpha Board"),
                    new MigrationProjectBoardOptionDto("PVT_beta", "Beta Board"),
                ],
                2,
                0));

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-b", Arg.Any<CancellationToken>()).Returns(
            new MigrationProjectBoardDiscoveryDto(
                [new MigrationProjectBoardOptionDto("PVT_target", "Target Board")],
                1,
                0));

        await using var ctx = CreateContext();
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, sourceRepository, targetRepository);
        await DisableLabelsAndMilestonesScopeAsync(cut);
        await EnableProjectBoardColumnsScopeAsync(cut);
        await EnableTargetRepositoryAsync(cut, targetRepository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("unlock preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.True(cut.Find("[data-testid='migration-preview-button']").HasAttribute("disabled"));
        });

        await _migrationService.DidNotReceive().PreviewMigrationAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<MigrationScopeDto>(),
            Arg.Any<MigrationConflictStrategy>(),
            Arg.Any<MigrationBoardSelectionDto?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Migration_InaccessibleLinkedProjectBoards_ShowsWarning()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-a", Arg.Any<CancellationToken>()).Returns(
            new MigrationProjectBoardDiscoveryDto(
                [new MigrationProjectBoardOptionDto("PVT_public", "Public Board")],
                2,
                1));

        _migrationService.GetProjectBoardOptionsAsync("owner", "repo-b", Arg.Any<CancellationToken>()).Returns(
            new MigrationProjectBoardDiscoveryDto([], 0, 0));

        await using var ctx = CreateContext();
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));

        await SelectRepositoriesAsync(cut, sourceRepository, targetRepository);
        await EnableProjectBoardColumnsScopeAsync(cut);
        await EnableTargetRepositoryAsync(cut, targetRepository);

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("2 linked project boards", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("1 board could not be loaded", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Single(cut.FindAll("[data-testid='migration-inaccessible-project-boards-warning']"));
        });
    }

    [Fact]
    public async Task Migration_OverwriteStrategy_RendersStatusOptionWarning()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='migration-repository-autocomplete']"));

        var strategySelect = cut.FindComponents<MudSelect<MigrationConflictStrategy>>().Single();
        await cut.InvokeAsync(() => strategySelect.Instance.ValueChanged.InvokeAsync(MigrationConflictStrategy.Overwrite));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("unused Status options", cut.Markup);
        });
    }

    [Fact]
    public async Task Migration_ConflictStrategyOptions_RenderExplanatoryText()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
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
    public async Task Migration_PageLoaded_RendersWorkflowAndFeedbackRegions()
    {
        // Arrange
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
                CreateRepository("owner", "repo-a"),
                CreateRepository("owner", "repo-b"),
            ]);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Migration>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("[data-testid='migration-workflow-controls-card']"));
            Assert.NotNull(cut.Find("[data-testid='migration-preview-empty-state']"));
            Assert.NotNull(cut.Find("[data-testid='migration-feedback-region']"));
            Assert.NotNull(cut.Find("[data-testid='migration-requirements-info']"));
        });
    }

    [Fact]
    public async Task Migration_PreviewClicked_UsesSelectedConflictStrategy()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.PreviewMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Is<MigrationScopeDto>(scope => scope!.IncludeLabels && scope.IncludeMilestones), MigrationConflictStrategy.Overwrite, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>()).Returns(new MigrationPreviewDto(
                MigrationConflictStrategy.Overwrite,
                [new LabelSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [], [])],
                [new MilestoneSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])],
                []));

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

        await _migrationService.Received(1).PreviewMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Is<MigrationScopeDto>(scope => scope!.IncludeLabels && scope.IncludeMilestones), MigrationConflictStrategy.Overwrite, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Migration_PreviewContainsNoActionableChanges_HidesApplyButton()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.PreviewMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Any<MigrationScopeDto>(), MigrationConflictStrategy.Skip, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>()).Returns(new MigrationPreviewDto(
                MigrationConflictStrategy.Skip,
                [new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [],
                    [],
                    [],
                    [new LabelDto("type/story", "1d76db", "Story", "owner/repo-b")],
                    [])],
                [new MilestoneSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])],
                []));

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
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Migration preview (Skip)", cut.Markup);
            Assert.NotNull(cut.Find("[data-testid='migration-no-action-warning']"));
        });

        Assert.Empty(cut.FindAll("[data-testid='migration-apply-button']"));
    }

    [Fact]
    public async Task Migration_ActionablePreviewGenerated_ShowsReadyToApplyFeedbackMessage()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.PreviewMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Any<MigrationScopeDto>(), MigrationConflictStrategy.Skip, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>()).Returns(new MigrationPreviewDto(
                MigrationConflictStrategy.Skip,
                [new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High priority", "owner/repo-b")],
                    [],
                    [],
                    [],
                    [])],
                [new MilestoneSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])],
                []));

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
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("[data-testid='migration-apply-button']"));
            Assert.NotNull(cut.Find("[data-testid='migration-ready-to-apply-info']"));
        });
    }

    [Fact]
    public async Task Migration_PreviewContainsMilestoneChanges_RendersMilestoneDetailTable()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.PreviewMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Any<MigrationScopeDto>(), MigrationConflictStrategy.Merge, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>()).Returns(new MigrationPreviewDto(
                MigrationConflictStrategy.Merge,
                [new LabelSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [], [])],
                [new MilestoneSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new MilestoneDto(1, 1, "Sprint 12", "Delivery sprint", "open", DateTimeOffset.Parse("2026-04-30T00:00:00Z"), 0, 0)],
                    [],
                    [],
                    [])],
                []));

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
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([]);

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
        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([
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
        await _migrationService.DidNotReceive().PreviewMigrationAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<MigrationScopeDto>(), Arg.Any<MigrationConflictStrategy>(), Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Migration_ApplyClickedTwiceDuringPendingCall_InvokesApplyServiceOnce()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");
        var applyTaskSource = new TaskCompletionSource<MigrationResultDto>();

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.PreviewMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Any<MigrationScopeDto>(), MigrationConflictStrategy.Skip, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>()).Returns(new MigrationPreviewDto(
                MigrationConflictStrategy.Skip,
                [new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High priority", "owner/repo-b")],
                    [],
                    [],
                    [],
                    [])],
                [new MilestoneSyncRepositoryPreviewDto("owner/repo-b", [], [], [], [])],
                []));

        _migrationService.ApplyMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Any<MigrationScopeDto>(), MigrationConflictStrategy.Skip, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>()).Returns(applyTaskSource.Task);

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
            [],
            [])));

        // Assert
        _snackbarProvider.WaitForAssertion(() => SnackbarTestAssertions.AssertLatestContains(_snackbarProvider, "Migration completed successfully"));
        await _migrationService.Received(1).ApplyMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Any<MigrationScopeDto>(), MigrationConflictStrategy.Skip, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Migration_ApplyReturnsPartialFailure_RendersSummaryAndErrorDetails()
    {
        // Arrange
        var sourceRepository = CreateRepository("owner", "repo-a");
        var targetRepository = CreateRepository("owner", "repo-b");

        _repositoryService.GetActiveRepositoriesAsync(Arg.Any<CancellationToken>()).Returns([sourceRepository, targetRepository]);

        _migrationService.PreviewMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Any<MigrationScopeDto>(), MigrationConflictStrategy.Merge, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>()).Returns(new MigrationPreviewDto(
                MigrationConflictStrategy.Merge,
                [new LabelSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new LabelDto("priority/high", "d93f0b", "High priority", "owner/repo-b")],
                    [],
                    [],
                    [],
                    [])],
                [new MilestoneSyncRepositoryPreviewDto(
                    "owner/repo-b",
                    [new MilestoneDto(1, 2, "Sprint 2", "Delivery", "open", null, 0, 0)],
                    [],
                    [],
                    [])],
                []));

        _migrationService.ApplyMigrationAsync("owner/repo-a", Arg.Is<IReadOnlyList<string>>(targets => targets!.SequenceEqual(new[] { "owner/repo-b" })), Arg.Any<MigrationScopeDto>(), MigrationConflictStrategy.Merge, Arg.Any<MigrationBoardSelectionDto?>(), Arg.Any<CancellationToken>()).Returns(new MigrationResultDto(
                MigrationConflictStrategy.Merge,
                [new LabelSyncRepositoryResultDto("owner/repo-b", 1, 0, 0, 0, "GitHub label API rate limit reached")],
                [new MilestoneSyncRepositoryResultDto("owner/repo-b", 1, 0, 0, 0, null)],
                []));

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
            Assert.Contains("Conflict strategy used: Merge", cut.Markup);
            Assert.Contains("Label migration failed.", cut.Markup);
            Assert.Contains("GitHub label API rate limit reached", cut.Markup);
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _repositoryService);
        ctx.Services.AddScoped(_ => _migrationService);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        _snackbarProvider = ctx.Render<MudSnackbarProvider>();

        return ctx;
    }

    private static async Task SelectRepositoriesAsync(IRenderedComponent<Migration> cut, params RepositoryDto[] repositories)
    {
        var selector = cut.FindComponent<RepositorySelector>();
        var selectedFullNames = repositories.Select(repository => repository.FullName).ToArray();

        await cut.InvokeAsync(() => selector.Instance.SelectedRepositoriesChanged.InvokeAsync(selectedFullNames));
    }

    private static async Task EnableProjectBoardColumnsScopeAsync(IRenderedComponent<Migration> cut)
    {
        cut.Find("[data-testid='migration-scope-columns-switch']").Change(true);
        await cut.InvokeAsync(() => { });
    }

    private static async Task DisableLabelsAndMilestonesScopeAsync(IRenderedComponent<Migration> cut)
    {
        cut.Find("[data-testid='migration-scope-labels-switch']").Change(false);
        cut.Find("[data-testid='migration-scope-milestones-switch']").Change(false);
        await cut.InvokeAsync(() => { });
    }

    private static Task EnableTargetRepositoryAsync(IRenderedComponent<Migration> cut, RepositoryDto repository)
    {
        var targetCheckbox = cut.FindComponents<MudCheckBox<bool>>()
            .Single(checkbox => string.Equals(checkbox.Instance.Label, repository.FullName, StringComparison.Ordinal));

        return cut.InvokeAsync(() => targetCheckbox.Instance.ValueChanged.InvokeAsync(true));
    }

    private static async Task ChangeSourceRepositoryAsync(IRenderedComponent<Migration> cut, string repositoryFullName)
    {
        var sourceSelect = cut.FindComponents<MudSelect<string>>()
            .Single(select => string.Equals(select.Instance.Label, "Source repository", StringComparison.Ordinal));

        await cut.InvokeAsync(() => sourceSelect.Instance.ValueChanged.InvokeAsync(repositoryFullName));
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
            UpdatedAt: DateTimeOffset.UnixEpoch,
            Topics: [],
            IsOpenSource: false);
}
