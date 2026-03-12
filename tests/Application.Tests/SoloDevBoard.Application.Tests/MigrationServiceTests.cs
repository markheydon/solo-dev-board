using Moq;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="MigrationService"/>.</summary>
public sealed class MigrationServiceTests
{
    private readonly Mock<ILabelRepository> _labelRepositoryMock = new();
    private readonly Mock<IMilestoneRepository> _milestoneRepositoryMock = new();

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

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("owner", "target", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, Label label, CancellationToken _) => label with { RepositoryName = repo });

        _labelRepositoryMock
            .Setup(repository => repository.UpdateLabelAsync("owner", "target", "type/story", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, string _, Label label, CancellationToken _) => label with { RepositoryName = repo });

        _labelRepositoryMock
            .Setup(repository => repository.DeleteLabelAsync("owner", "target", "legacy", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _milestoneRepositoryMock
            .Setup(repository => repository.CreateMilestoneAsync("owner", "target", It.IsAny<Milestone>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string _, Milestone milestone, CancellationToken _) => milestone);

        _milestoneRepositoryMock
            .Setup(repository => repository.UpdateMilestoneAsync("owner", "target", 9, It.IsAny<Milestone>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string _, int number, Milestone milestone, CancellationToken _) => milestone with { Number = number });

        _milestoneRepositoryMock
            .Setup(repository => repository.DeleteMilestoneAsync("owner", "target", 10, It.IsAny<CancellationToken>()))
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

        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync("owner", "target", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("owner", "target", "type/story", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("owner", "target", "legacy", It.IsAny<CancellationToken>()), Times.Once);

        _milestoneRepositoryMock.Verify(repository => repository.CreateMilestoneAsync("owner", "target", It.IsAny<Milestone>(), It.IsAny<CancellationToken>()), Times.Once);
        _milestoneRepositoryMock.Verify(repository => repository.UpdateMilestoneAsync("owner", "target", 9, It.IsAny<Milestone>(), It.IsAny<CancellationToken>()), Times.Once);
        _milestoneRepositoryMock.Verify(repository => repository.DeleteMilestoneAsync("owner", "target", 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyMigrationAsync_SkipStrategy_DoesNotUpdateOrDeleteConflicts()
    {
        // Arrange
        SetupSourceAndTargetData();

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("owner", "target", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, Label label, CancellationToken _) => label with { RepositoryName = repo });

        _milestoneRepositoryMock
            .Setup(repository => repository.CreateMilestoneAsync("owner", "target", It.IsAny<Milestone>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string _, Milestone milestone, CancellationToken _) => milestone);

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

        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _milestoneRepositoryMock.Verify(repository => repository.UpdateMilestoneAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Milestone>(), It.IsAny<CancellationToken>()), Times.Never);
        _milestoneRepositoryMock.Verify(repository => repository.DeleteMilestoneAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private MigrationService CreateSubject()
        => new(_labelRepositoryMock.Object, _milestoneRepositoryMock.Object);

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

        _milestoneRepositoryMock
            .Setup(repository => repository.CreateMilestoneAsync("owner", "target", It.IsAny<Milestone>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string _, Milestone milestone, CancellationToken _) => milestone);

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

        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupSourceAndTargetData()
    {
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "source", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Source story", RepositoryName = "source" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "Source high", RepositoryName = "source" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "target", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "ffffff", Description = "Target story", RepositoryName = "target" },
                new Label { Name = "legacy", Colour = "cfd3d7", Description = "Legacy", RepositoryName = "target" },
            ]);

        _milestoneRepositoryMock
            .Setup(repository => repository.GetMilestonesAsync("owner", "source", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Milestone { Id = 1, Number = 1, Title = "Sprint 1", Description = "Source sprint 1", State = "open", DueOn = DateTimeOffset.Parse("2026-04-01T00:00:00Z") },
                new Milestone { Id = 2, Number = 2, Title = "Sprint 2", Description = "Source sprint 2", State = "open", DueOn = null },
            ]);

        _milestoneRepositoryMock
            .Setup(repository => repository.GetMilestonesAsync("owner", "target", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Milestone { Id = 9, Number = 9, Title = "Sprint 1", Description = "Target sprint 1", State = "open", DueOn = DateTimeOffset.Parse("2026-03-20T00:00:00Z") },
                new Milestone { Id = 10, Number = 10, Title = "Legacy", Description = "Legacy milestone", State = "open", DueOn = null },
            ]);
    }
}
