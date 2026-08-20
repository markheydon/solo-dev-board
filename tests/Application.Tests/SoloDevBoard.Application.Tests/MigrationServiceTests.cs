using NSubstitute;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Migration;
using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="MigrationService"/>.</summary>
public sealed class MigrationServiceTests
{
    private readonly ILabelRepository _labelRepository = Substitute.For<ILabelRepository>();
    private readonly IMilestoneRepository _milestoneRepository = Substitute.For<IMilestoneRepository>();
    private readonly IProjectBoardStructureRepository _projectBoardStructureRepository = Substitute.For<IProjectBoardStructureRepository>();

    [Fact]
    public async Task PreviewMigrationAsync_SkipStrategy_ReturnsCreateAndSkipOnly()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Skip, cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(MigrationConflictStrategy.Skip, result.ConflictStrategy);
        Assert.Single(result.LabelPreviews);
        Assert.Single(result.MilestonePreviews);

        var labelPreview = result.LabelPreviews[0];
        Assert.Single(labelPreview.ToCreate);
        Assert.Equal("priority/high", labelPreview.ToCreate[0].Name);
        Assert.Empty(labelPreview.ToUpdate);
        Assert.Empty(labelPreview.ToDelete);
        Assert.Single(labelPreview.Skipped);
        Assert.Equal("type/story", labelPreview.Skipped[0].Name);

        var milestonePreview = result.MilestonePreviews[0];
        Assert.Single(milestonePreview.ToCreate);
        Assert.Equal("Sprint 2", milestonePreview.ToCreate[0].Title);
        Assert.Empty(milestonePreview.ToUpdate);
        Assert.Empty(milestonePreview.ToDelete);
        Assert.Single(milestonePreview.Skipped);
        Assert.Equal("Sprint 1", milestonePreview.Skipped[0].Title);
    }

    [Fact]
    public async Task PreviewMigrationAsync_OverwriteStrategy_ReturnsCreateUpdateAndDelete()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Overwrite, cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(MigrationConflictStrategy.Overwrite, result.ConflictStrategy);

        var labelPreview = result.LabelPreviews[0];
        Assert.Single(labelPreview.ToCreate);
        Assert.Single(labelPreview.ToUpdate);
        Assert.Single(labelPreview.ToDelete);

        var milestonePreview = result.MilestonePreviews[0];
        Assert.Single(milestonePreview.ToCreate);
        Assert.Single(milestonePreview.ToUpdate);
        Assert.Single(milestonePreview.ToDelete);
        Assert.Equal(9, milestonePreview.ToUpdate[0].Number);
    }

    [Fact]
    public async Task ApplyMigrationAsync_OverwriteStrategy_AppliesLabelAndMilestoneOperations()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();

        _labelRepository
            .CreateLabelAsync("owner", "target", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        _labelRepository
            .UpdateLabelAsync("owner", "target", "type/story", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(3);
                return label with { RepositoryName = repo };
            });

        _labelRepository
            .DeleteLabelAsync("owner", "target", "legacy", cancellationToken)
            .Returns(Task.CompletedTask);

        _milestoneRepository
            .CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        _milestoneRepository
            .UpdateMilestoneAsync("owner", "target", 9, Arg.Any<Milestone>(), cancellationToken)
            .Returns(callInfo =>
            {
                var number = callInfo.ArgAt<int>(2);
                var milestone = callInfo.ArgAt<Milestone>(3);
                return milestone with { Number = number };
            });

        _milestoneRepository
            .DeleteMilestoneAsync("owner", "target", 10, cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Overwrite, cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(MigrationConflictStrategy.Overwrite, result.ConflictStrategy);
        Assert.Single(result.LabelResults);
        Assert.Single(result.MilestoneResults);

        Assert.Equal(1, result.LabelResults[0].CreatedCount);
        Assert.Equal(1, result.LabelResults[0].UpdatedCount);
        Assert.Equal(1, result.LabelResults[0].DeletedCount);

        Assert.Equal(1, result.MilestoneResults[0].CreatedCount);
        Assert.Equal(1, result.MilestoneResults[0].UpdatedCount);
        Assert.Equal(1, result.MilestoneResults[0].DeletedCount);

        await _labelRepository.Received(1).CreateLabelAsync("owner", "target", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.Received(1).UpdateLabelAsync("owner", "target", "type/story", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "target", "legacy", cancellationToken);

        await _milestoneRepository.Received(1).CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), cancellationToken);
        await _milestoneRepository.Received(1).UpdateMilestoneAsync("owner", "target", 9, Arg.Any<Milestone>(), cancellationToken);
        await _milestoneRepository.Received(1).DeleteMilestoneAsync("owner", "target", 10, cancellationToken);
    }

    [Fact]
    public async Task ApplyMigrationAsync_SkipStrategy_DoesNotUpdateOrDeleteConflicts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();

        _labelRepository
            .CreateLabelAsync("owner", "target", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        _milestoneRepository
            .CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Skip, cancellationToken: cancellationToken);

        // Assert
        Assert.Single(result.LabelResults);
        Assert.Single(result.MilestoneResults);
        Assert.Equal(1, result.LabelResults[0].CreatedCount);
        Assert.Equal(0, result.LabelResults[0].UpdatedCount);
        Assert.Equal(0, result.LabelResults[0].DeletedCount);
        Assert.Equal(1, result.LabelResults[0].SkippedCount);

        Assert.Equal(1, result.MilestoneResults[0].CreatedCount);
        Assert.Equal(0, result.MilestoneResults[0].UpdatedCount);
        Assert.Equal(0, result.MilestoneResults[0].DeletedCount);
        Assert.Equal(1, result.MilestoneResults[0].SkippedCount);

        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken);
        await _milestoneRepository.DidNotReceive().UpdateMilestoneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Milestone>(), cancellationToken);
        await _milestoneRepository.DidNotReceive().DeleteMilestoneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), cancellationToken);
    }

    private MigrationService CreateSubject()
        => new(_labelRepository, _milestoneRepository, _projectBoardStructureRepository);

    [Fact]
    public async Task PreviewMigrationAsync_LabelsOnly_DoesNotReturnMilestonePreviews()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, false),
            MigrationConflictStrategy.Merge, cancellationToken: cancellationToken);

        // Assert
        Assert.Single(result.LabelPreviews);
        Assert.Empty(result.MilestonePreviews);
    }

    [Fact]
    public async Task ApplyMigrationAsync_MilestonesOnly_DoesNotRunLabelOperations()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();

        _milestoneRepository
            .CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, true),
            MigrationConflictStrategy.Skip, cancellationToken: cancellationToken);

        // Assert
        Assert.Empty(result.LabelResults);
        Assert.Single(result.MilestoneResults);

        await _labelRepository.DidNotReceive().CreateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken);
    }

    [Fact]
    public async Task PreviewMigrationAsync_NoScopeSelected_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var action = async () => await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false),
            MigrationConflictStrategy.Skip, cancellationToken: cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyMigrationAsync_TargetRepositoriesContainOnlySource_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var action = async () => await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/source"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Skip, cancellationToken: cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyMigrationAsync_MultipleTargetsOneLabelOperationFails_ReturnsPartialFailureAndContinues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();

        _labelRepository
            .GetLabelsAsync("owner", "failing", cancellationToken)
            .Returns(Task.FromException<IReadOnlyList<Label>>(new HttpRequestException("label operation failed")));

        _milestoneRepository
            .GetMilestonesAsync("owner", "failing", cancellationToken)
            .Returns([]);

        _labelRepository
            .CreateLabelAsync("owner", "target", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        _milestoneRepository
            .CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        _milestoneRepository
            .CreateMilestoneAsync("owner", "failing", Arg.Any<Milestone>(), cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target", "owner/failing"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Skip, cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(2, result.LabelResults.Count);
        Assert.Equal(2, result.MilestoneResults.Count);

        var targetLabelResult = Assert.Single(result.LabelResults, item => item.RepositoryFullName == "owner/target");
        Assert.False(targetLabelResult.HasError);
        Assert.Equal(1, targetLabelResult.CreatedCount);

        var failingLabelResult = Assert.Single(result.LabelResults, item => item.RepositoryFullName == "owner/failing");
        Assert.True(failingLabelResult.HasError);
        Assert.Contains("label operation failed", failingLabelResult.ErrorMessage, StringComparison.Ordinal);

        var targetMilestoneResult = Assert.Single(result.MilestoneResults, item => item.RepositoryFullName == "owner/target");
        Assert.False(targetMilestoneResult.HasError);
        Assert.Equal(1, targetMilestoneResult.CreatedCount);

        var failingMilestoneResult = Assert.Single(result.MilestoneResults, item => item.RepositoryFullName == "owner/failing");
        Assert.False(failingMilestoneResult.HasError);
        Assert.Equal(2, failingMilestoneResult.CreatedCount);
    }

    [Fact]
    public async Task ApplyMigrationAsync_UpdateFailsAfterCreate_ReturnsErrorWithPartialProgressCounts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        SetupSourceAndTargetData();

        _labelRepository
            .CreateLabelAsync("owner", "target", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        _labelRepository
            .UpdateLabelAsync("owner", "target", "type/story", Arg.Any<Label>(), cancellationToken)
            .Returns(Task.FromException<Label>(new HttpRequestException("update failed")));

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, false),
            MigrationConflictStrategy.Overwrite, cancellationToken: cancellationToken);

        // Assert
        var labelResult = Assert.Single(result.LabelResults);
        Assert.True(labelResult.HasError);
        Assert.Equal(1, labelResult.CreatedCount);
        Assert.Equal(0, labelResult.UpdatedCount);
        Assert.Contains("update failed", labelResult.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewMigrationAsync_StatusColumnsSkip_ReturnsCreateAndSkipOnly()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SetupStatusColumnData();
        var sut = CreateSubject();
        var boardSelection = CreateBoardSelection("source-project", "owner/target", "target-project");

        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false, true),
            MigrationConflictStrategy.Skip,
            boardSelection,
            cancellationToken);

        var preview = Assert.Single(result.ProjectBoardStatusPreviews);
        Assert.Equal(2, preview.ToCreate.Count);
        Assert.Contains(preview.ToCreate, option => option.Name == "Backlog");
        Assert.Contains(preview.ToCreate, option => option.Name == "Done");
        Assert.Empty(preview.ToUpdate);
        Assert.Empty(preview.ToDelete);
        Assert.Single(preview.Skipped);
        Assert.Equal("In Progress", preview.Skipped[0].Name);
    }

    [Fact]
    public async Task PreviewMigrationAsync_StatusColumnsMerge_ReturnsCreateUpdateAndKeepsTargetOnlyOption()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SetupStatusColumnData();
        var sut = CreateSubject();
        var boardSelection = CreateBoardSelection("source-project", "owner/target", "target-project");

        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false, true),
            MigrationConflictStrategy.Merge,
            boardSelection,
            cancellationToken);

        var preview = Assert.Single(result.ProjectBoardStatusPreviews);
        Assert.Equal(2, preview.ToCreate.Count);
        Assert.Single(preview.ToUpdate);
        Assert.Empty(preview.ToDelete);
        Assert.Equal("In Progress", preview.ToUpdate[0].Name);
    }

    [Fact]
    public async Task PreviewMigrationAsync_StatusColumnsOverwrite_BlocksDeleteWhenOptionStillInUse()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SetupStatusColumnData();
        _projectBoardStructureRepository
            .GetStatusOptionIdsInUseAsync("target-project", cancellationToken)
            .Returns(new HashSet<string>(StringComparer.Ordinal) { "option-blocked" });

        var sut = CreateSubject();
        var boardSelection = CreateBoardSelection("source-project", "owner/target", "target-project");

        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false, true),
            MigrationConflictStrategy.Overwrite,
            boardSelection,
            cancellationToken);

        var preview = Assert.Single(result.ProjectBoardStatusPreviews);
        Assert.Equal(2, preview.ToDelete.Count);
        Assert.Contains(preview.ToDelete, option => option.Name == "Legacy");
        Assert.Contains(preview.ToDelete, option => option.Name == "Todo");
        Assert.Single(preview.Warnings);
        Assert.Contains("Blocked", preview.Warnings[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewMigrationAsync_CreateNewBoard_ReturnsAllSourceOptionsAsCreates()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SetupStatusColumnData();
        var sut = CreateSubject();
        var boardSelection = new MigrationBoardSelectionDto(
            "source-project",
            [new MigrationTargetBoardSelectionDto("owner/target", null, "Target board")]);

        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false, true),
            MigrationConflictStrategy.Merge,
            boardSelection,
            cancellationToken);

        var preview = Assert.Single(result.ProjectBoardStatusPreviews);
        Assert.True(preview.CreateNewBoard);
        Assert.Equal(3, preview.ToCreate.Count);
        Assert.Empty(preview.ToUpdate);
        Assert.Empty(preview.ToDelete);
        Assert.Single(preview.Warnings);
        Assert.Contains("GitHub default Status options", preview.Warnings[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewMigrationAsync_StatusColumnsInaccessibleBoards_PropagatesVisibilityCounts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SetupStatusColumnData(inaccessibleCount: 2, totalLinkedCount: 3);
        var sut = CreateSubject();
        var boardSelection = CreateBoardSelection("source-project", "owner/target", "target-project");

        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false, true),
            MigrationConflictStrategy.Skip,
            boardSelection,
            cancellationToken);

        var preview = Assert.Single(result.ProjectBoardStatusPreviews);
        Assert.Equal(3, preview.TotalLinkedProjectCount);
        Assert.Equal(2, preview.InaccessibleLinkedProjectCount);
    }

    [Fact]
    public async Task ApplyMigrationAsync_StatusColumnsMerge_PreservesTargetOptionIdsInUpdatePayload()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SetupStatusColumnData();
        var sut = CreateSubject();
        var boardSelection = CreateBoardSelection("source-project", "owner/target", "target-project");

        IReadOnlyList<ProjectBoardStatusStructureOption>? capturedOptions = null;
        _projectBoardStructureRepository
            .UpdateStatusOptionsAsync("target-project", "status-field", Arg.Any<IReadOnlyList<ProjectBoardStatusStructureOption>>(), cancellationToken)
            .Returns(callInfo =>
            {
                capturedOptions = callInfo.ArgAt<IReadOnlyList<ProjectBoardStatusStructureOption>>(2);
                return CreateTargetStructure();
            });

        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false, true),
            MigrationConflictStrategy.Merge,
            boardSelection,
            cancellationToken);

        var statusResult = Assert.Single(result.ProjectBoardStatusResults);
        Assert.Null(statusResult.ErrorMessage);
        Assert.NotNull(capturedOptions);
        var inProgress = capturedOptions!.Single(option => option.Name.Equals("In Progress", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("option-in-progress", inProgress.Id);
        Assert.Equal("YELLOW", inProgress.Colour, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyMigrationAsync_CreateNewBoard_CreatesLinkedProjectAndReshapesStatusOptions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SetupStatusColumnData();
        var sut = CreateSubject();
        var boardSelection = new MigrationBoardSelectionDto(
            "source-project",
            [new MigrationTargetBoardSelectionDto("owner/target", null, "Imported board")]);

        _projectBoardStructureRepository
            .CreateLinkedProjectAsync("owner", "target", "Imported board", cancellationToken)
            .Returns(new ProjectBoardStatusStructure
            {
                ProjectId = "created-project",
                ProjectTitle = "Imported board",
                StatusFieldId = "created-status-field",
                Options =
                [
                    new ProjectBoardStatusStructureOption { Id = "default-todo", Name = "Todo", Colour = "GRAY", Description = string.Empty, Order = 0 },
                    new ProjectBoardStatusStructureOption { Id = "default-done", Name = "Done", Colour = "GREEN", Description = string.Empty, Order = 1 },
                ],
            });

        _projectBoardStructureRepository
            .UpdateStatusOptionsAsync("created-project", "created-status-field", Arg.Any<IReadOnlyList<ProjectBoardStatusStructureOption>>(), cancellationToken)
            .Returns(callInfo => new ProjectBoardStatusStructure
            {
                ProjectId = "created-project",
                StatusFieldId = "created-status-field",
                Options = callInfo.ArgAt<IReadOnlyList<ProjectBoardStatusStructureOption>>(2),
            });

        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false, true),
            MigrationConflictStrategy.Merge,
            boardSelection,
            cancellationToken);

        var statusResult = Assert.Single(result.ProjectBoardStatusResults);
        Assert.Equal("created-project", statusResult.CreatedProjectId);
        await _projectBoardStructureRepository.Received(1).CreateLinkedProjectAsync("owner", "target", "Imported board", cancellationToken);
        await _projectBoardStructureRepository.Received(1).UpdateStatusOptionsAsync(
            "created-project",
            "created-status-field",
            Arg.Any<IReadOnlyList<ProjectBoardStatusStructureOption>>(),
            cancellationToken);
    }

    [Fact]
    public async Task ApplyMigrationAsync_CreateNewBoardOverwrite_RemovesDefaultOptionsNotInSource()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SetupStatusColumnData();
        var sut = CreateSubject();
        var boardSelection = new MigrationBoardSelectionDto(
            "source-project",
            [new MigrationTargetBoardSelectionDto("owner/target", null, "Imported board")]);

        _projectBoardStructureRepository
            .CreateLinkedProjectAsync("owner", "target", "Imported board", cancellationToken)
            .Returns(new ProjectBoardStatusStructure
            {
                ProjectId = "created-project",
                ProjectTitle = "Imported board",
                StatusFieldId = "created-status-field",
                Options =
                [
                    new ProjectBoardStatusStructureOption { Id = "default-todo", Name = "Todo", Colour = "GRAY", Description = string.Empty, Order = 0 },
                    new ProjectBoardStatusStructureOption { Id = "default-done", Name = "Done", Colour = "GREEN", Description = string.Empty, Order = 1 },
                ],
            });

        IReadOnlyList<ProjectBoardStatusStructureOption>? capturedOptions = null;
        _projectBoardStructureRepository
            .UpdateStatusOptionsAsync("created-project", "created-status-field", Arg.Any<IReadOnlyList<ProjectBoardStatusStructureOption>>(), cancellationToken)
            .Returns(callInfo =>
            {
                capturedOptions = callInfo.ArgAt<IReadOnlyList<ProjectBoardStatusStructureOption>>(2);
                return new ProjectBoardStatusStructure
                {
                    ProjectId = "created-project",
                    StatusFieldId = "created-status-field",
                    Options = capturedOptions!,
                };
            });

        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false, true),
            MigrationConflictStrategy.Overwrite,
            boardSelection,
            cancellationToken);

        var statusResult = Assert.Single(result.ProjectBoardStatusResults);
        Assert.Null(statusResult.ErrorMessage);
        Assert.NotNull(capturedOptions);
        Assert.DoesNotContain(capturedOptions!, option => option.Name.Equals("Todo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(capturedOptions!, option => option.Name.Equals("Done", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, capturedOptions!.Count);
    }

    [Fact]
    public void ProjectBoardStatusSyncRepositoryResultDto_ErrorMessagePresent_HasErrorIsTrue()
    {
        var result = new ProjectBoardStatusSyncRepositoryResultDto(
            "owner/target",
            0,
            0,
            0,
            0,
            null,
            [],
            "GitHub API failed");

        Assert.True(result.HasError);
    }

    private static MigrationBoardSelectionDto CreateBoardSelection(
        string sourceProjectId,
        string targetRepositoryFullName,
        string targetProjectId)
        => new(
            sourceProjectId,
            [new MigrationTargetBoardSelectionDto(targetRepositoryFullName, targetProjectId, null)]);

    private void SetupStatusColumnData(int inaccessibleCount = 0, int totalLinkedCount = 1)
    {
        var sourceStructure = CreateSourceStructure();
        var targetStructure = CreateTargetStructure();

        _projectBoardStructureRepository
            .GetStatusStructureAsync("source-project", Arg.Any<CancellationToken>())
            .Returns(sourceStructure);

        _projectBoardStructureRepository
            .GetStatusStructureAsync("target-project", Arg.Any<CancellationToken>())
            .Returns(targetStructure);

        _projectBoardStructureRepository
            .DiscoverBoardsAsync("owner", "target", Arg.Any<CancellationToken>())
            .Returns(new ProjectBoardDiscovery
            {
                SupportedBoards = [targetStructure],
                TotalLinkedProjectCount = totalLinkedCount,
                InaccessibleLinkedProjectCount = inaccessibleCount,
            });

        _projectBoardStructureRepository
            .GetStatusOptionIdsInUseAsync("target-project", Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(StringComparer.Ordinal));
    }

    private static ProjectBoardStatusStructure CreateSourceStructure()
        => new()
        {
            ProjectId = "source-project",
            ProjectTitle = "Source board",
            StatusFieldId = "source-status-field",
            Options =
            [
                new ProjectBoardStatusStructureOption { Id = "source-backlog", Name = "Backlog", Colour = "GRAY", Description = "Queued", Order = 0 },
                new ProjectBoardStatusStructureOption { Id = "source-in-progress", Name = "In Progress", Colour = "YELLOW", Description = "Active", Order = 1 },
                new ProjectBoardStatusStructureOption { Id = "source-done", Name = "Done", Colour = "GREEN", Description = "Complete", Order = 2 },
            ],
        };

    private static ProjectBoardStatusStructure CreateTargetStructure()
        => new()
        {
            ProjectId = "target-project",
            ProjectTitle = "Target board",
            StatusFieldId = "status-field",
            Options =
            [
                new ProjectBoardStatusStructureOption { Id = "option-todo", Name = "Todo", Colour = "GRAY", Description = string.Empty, Order = 0 },
                new ProjectBoardStatusStructureOption { Id = "option-in-progress", Name = "In Progress", Colour = "BLUE", Description = "Existing", Order = 1 },
                new ProjectBoardStatusStructureOption { Id = "option-legacy", Name = "Legacy", Colour = "PURPLE", Description = string.Empty, Order = 2 },
                new ProjectBoardStatusStructureOption { Id = "option-blocked", Name = "Blocked", Colour = "RED", Description = string.Empty, Order = 3 },
            ],
        };

    private void SetupSourceAndTargetData()
    {
        _labelRepository
            .GetLabelsAsync("owner", "source", Arg.Any<CancellationToken>())
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Source story", RepositoryName = "source" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "Source high", RepositoryName = "source" },
            ]);

        _labelRepository
            .GetLabelsAsync("owner", "target", Arg.Any<CancellationToken>())
            .Returns([
                new Label { Name = "type/story", Colour = "ffffff", Description = "Target story", RepositoryName = "target" },
                new Label { Name = "legacy", Colour = "cfd3d7", Description = "Legacy", RepositoryName = "target" },
            ]);

        _milestoneRepository
            .GetMilestonesAsync("owner", "source", Arg.Any<CancellationToken>())
            .Returns([
                new Milestone { Id = 1, Number = 1, Title = "Sprint 1", Description = "Source sprint 1", State = "open", DueOn = DateTimeOffset.Parse("2026-04-01T00:00:00Z") },
                new Milestone { Id = 2, Number = 2, Title = "Sprint 2", Description = "Source sprint 2", State = "open", DueOn = null },
            ]);

        _milestoneRepository
            .GetMilestonesAsync("owner", "target", Arg.Any<CancellationToken>())
            .Returns([
                new Milestone { Id = 9, Number = 9, Title = "Sprint 1", Description = "Target sprint 1", State = "open", DueOn = DateTimeOffset.Parse("2026-03-20T00:00:00Z") },
                new Milestone { Id = 10, Number = 10, Title = "Legacy", Description = "Legacy milestone", State = "open", DueOn = null },
            ]);
    }
}
