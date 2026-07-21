using NSubstitute;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Migration;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="MigrationService"/>.</summary>
public sealed class MigrationServiceTests
{
    private readonly ILabelRepository _labelRepository = Substitute.For<ILabelRepository>();
    private readonly IMilestoneRepository _milestoneRepository = Substitute.For<IMilestoneRepository>();

    [Fact]
    public async Task PreviewMigrationAsync_SkipStrategy_ReturnsCreateAndSkipOnly()
    {
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Skip);

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
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Overwrite);

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
        // Arrange
        SetupSourceAndTargetData();

        _labelRepository
            .CreateLabelAsync("owner", "target", Arg.Any<Label>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        _labelRepository
            .UpdateLabelAsync("owner", "target", "type/story", Arg.Any<Label>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(3);
                return label with { RepositoryName = repo };
            });

        _labelRepository
            .DeleteLabelAsync("owner", "target", "legacy", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _milestoneRepository
            .CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        _milestoneRepository
            .UpdateMilestoneAsync("owner", "target", 9, Arg.Any<Milestone>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var number = callInfo.ArgAt<int>(2);
                var milestone = callInfo.ArgAt<Milestone>(3);
                return milestone with { Number = number };
            });

        _milestoneRepository
            .DeleteMilestoneAsync("owner", "target", 10, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Overwrite);

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

        await _labelRepository.Received(1).CreateLabelAsync("owner", "target", Arg.Any<Label>(), Arg.Any<CancellationToken>());
        await _labelRepository.Received(1).UpdateLabelAsync("owner", "target", "type/story", Arg.Any<Label>(), Arg.Any<CancellationToken>());
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "target", "legacy", Arg.Any<CancellationToken>());

        await _milestoneRepository.Received(1).CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), Arg.Any<CancellationToken>());
        await _milestoneRepository.Received(1).UpdateMilestoneAsync("owner", "target", 9, Arg.Any<Milestone>(), Arg.Any<CancellationToken>());
        await _milestoneRepository.Received(1).DeleteMilestoneAsync("owner", "target", 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyMigrationAsync_SkipStrategy_DoesNotUpdateOrDeleteConflicts()
    {
        // Arrange
        SetupSourceAndTargetData();

        _labelRepository
            .CreateLabelAsync("owner", "target", Arg.Any<Label>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        _milestoneRepository
            .CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Skip);

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

        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), Arg.Any<CancellationToken>());
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _milestoneRepository.DidNotReceive().UpdateMilestoneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Milestone>(), Arg.Any<CancellationToken>());
        await _milestoneRepository.DidNotReceive().DeleteMilestoneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private MigrationService CreateSubject()
        => new(_labelRepository, _milestoneRepository);

    [Fact]
    public async Task PreviewMigrationAsync_LabelsOnly_DoesNotReturnMilestonePreviews()
    {
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var result = await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, false),
            MigrationConflictStrategy.Merge);

        // Assert
        Assert.Single(result.LabelPreviews);
        Assert.Empty(result.MilestonePreviews);
    }

    [Fact]
    public async Task ApplyMigrationAsync_MilestonesOnly_DoesNotRunLabelOperations()
    {
        // Arrange
        SetupSourceAndTargetData();

        _milestoneRepository
            .CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, true),
            MigrationConflictStrategy.Skip);

        // Assert
        Assert.Empty(result.LabelResults);
        Assert.Single(result.MilestoneResults);

        await _labelRepository.DidNotReceive().CreateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), Arg.Any<CancellationToken>());
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), Arg.Any<CancellationToken>());
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewMigrationAsync_NoScopeSelected_ThrowsArgumentException()
    {
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var action = async () => await sut.PreviewMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(false, false),
            MigrationConflictStrategy.Skip);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyMigrationAsync_TargetRepositoriesContainOnlySource_ThrowsArgumentException()
    {
        // Arrange
        SetupSourceAndTargetData();
        var sut = CreateSubject();

        // Act
        var action = async () => await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/source"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Skip);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyMigrationAsync_MultipleTargetsOneLabelOperationFails_ReturnsPartialFailureAndContinues()
    {
        // Arrange
        SetupSourceAndTargetData();

        _labelRepository
            .GetLabelsAsync("owner", "failing", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Label>>(new HttpRequestException("label operation failed")));

        _milestoneRepository
            .GetMilestonesAsync("owner", "failing", Arg.Any<CancellationToken>())
            .Returns([]);

        _labelRepository
            .CreateLabelAsync("owner", "target", Arg.Any<Label>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        _milestoneRepository
            .CreateMilestoneAsync("owner", "target", Arg.Any<Milestone>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        _milestoneRepository
            .CreateMilestoneAsync("owner", "failing", Arg.Any<Milestone>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<Milestone>(2));

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target", "owner/failing"],
            new MigrationScopeDto(true, true),
            MigrationConflictStrategy.Skip);

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
        // Arrange
        SetupSourceAndTargetData();

        _labelRepository
            .CreateLabelAsync("owner", "target", Arg.Any<Label>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        _labelRepository
            .UpdateLabelAsync("owner", "target", "type/story", Arg.Any<Label>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Label>(new HttpRequestException("update failed")));

        var sut = CreateSubject();

        // Act
        var result = await sut.ApplyMigrationAsync(
            "owner/source",
            ["owner/target"],
            new MigrationScopeDto(true, false),
            MigrationConflictStrategy.Overwrite);

        // Assert
        var labelResult = Assert.Single(result.LabelResults);
        Assert.True(labelResult.HasError);
        Assert.Equal(1, labelResult.CreatedCount);
        Assert.Equal(0, labelResult.UpdatedCount);
        Assert.Contains("update failed", labelResult.ErrorMessage, StringComparison.Ordinal);
    }

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
