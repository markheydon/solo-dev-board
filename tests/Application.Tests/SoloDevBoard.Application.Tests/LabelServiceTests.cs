using Moq;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="LabelService"/>.</summary>
public sealed class LabelServiceTests
{
    private readonly Mock<ILabelRepository> _labelRepositoryMock = new();

    [Fact]
    public void Constructor_LabelRepositoryIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        ILabelRepository? labelRepository = null;

        // Act
        var action = () => _ = new LabelService(labelRepository!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task GetLabelsAsync_RepositoryReturnsLabels_ReturnsMappedLabelDtos()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "1d76db", Description = "A user-facing Story delivering a discrete piece of value", RepositoryName = "repo" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "Should be addressed in the current sprint or release", RepositoryName = "repo" },
            ]);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.GetLabelsAsync("owner", "repo");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("type/story", result[0].Name);
        Assert.Equal("repo", result[0].RepositoryName);
    }

    [Fact]
    public async Task GetLabelsForRepositoriesAsync_MultipleRepositories_ReturnsMergedLabels()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "repo-a" }]);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "repo-b" }]);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.GetLabelsForRepositoriesAsync("owner", ["repo-a", "repo-b"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, label => label is { Name: "type/story", RepositoryName: "repo-a" });
        Assert.Contains(result, label => label is { Name: "priority/high", RepositoryName: "repo-b" });
    }

    [Fact]
    public async Task GetLabelsForRepositoriesAsync_DuplicateRepositoryNames_QueriesEachRepositoryOnce()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "repo-a" }]);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.GetLabelsForRepositoriesAsync("owner", ["repo-a", "repo-a", "repo-a"]);

        // Assert
        Assert.Single(result);
        _labelRepositoryMock.Verify(repository => repository.GetLabelsAsync("owner", "repo-a", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLabelsForRepositoriesAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.GetLabelsForRepositoriesAsync(" ", ["repo-a"]);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task CreateLabelAsync_MultipleRepositories_CreatesLabelInEachRepository()
    {
        // Arrange
        var label = new LabelDto("area/labels", "c5def5", "Label Manager feature", string.Empty);

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("owner", It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, Label value, CancellationToken _) => value with { RepositoryName = repo });

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.CreateLabelAsync("owner", ["repo-a", "repo-b"], label);

        // Assert
        Assert.Equal(2, result.Count);
        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync("owner", "repo-a", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync("owner", "repo-b", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLabelAsync_SecondRepositoryFails_ThrowsAndStopsFurtherProcessing()
    {
        // Arrange
        var label = new LabelDto("area/labels", "c5def5", "Label Manager feature", string.Empty);

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("owner", "repo-a", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, Label value, CancellationToken _) => value with { RepositoryName = repo });

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("owner", "repo-b", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub API failure"));

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.CreateLabelAsync("owner", ["repo-a", "repo-b", "repo-c"], label);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync("owner", "repo-a", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync("owner", "repo-b", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync("owner", "repo-c", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLabelAsync_LabelIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.CreateLabelAsync("owner", ["repo-a"], null!);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task CreateLabelAsync_RepositoriesEmpty_ThrowsArgumentException()
    {
        // Arrange
        var sut = new LabelService(_labelRepositoryMock.Object);
        var label = new LabelDto("area/labels", "c5def5", "Label Manager feature", string.Empty);

        // Act
        var action = async () => await sut.CreateLabelAsync("owner", [], label);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task UpdateLabelAsync_MultipleRepositories_UpdatesLabelInEachRepository()
    {
        // Arrange
        var updatedLabel = new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", string.Empty);

        _labelRepositoryMock
            .Setup(repository => repository.UpdateLabelAsync("owner", It.IsAny<string>(), "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, string _, Label value, CancellationToken _) => value with { RepositoryName = repo });

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.UpdateLabelAsync("owner", ["repo-a", "repo-b"], "priority/urgent", updatedLabel);

        // Assert
        Assert.Equal(2, result.Count);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("owner", "repo-a", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("owner", "repo-b", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLabelAsync_RepositoryReportsLabelMissing_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updatedLabel = new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", string.Empty);

        _labelRepositoryMock
            .Setup(repository => repository.UpdateLabelAsync("owner", "repo-a", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Label not found"));

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.UpdateLabelAsync("owner", ["repo-a", "repo-b"], "priority/urgent", updatedLabel);

        // Assert
        _ = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("owner", "repo-b", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLabelAsync_SecondRepositoryFails_ThrowsAndStopsFurtherProcessing()
    {
        // Arrange
        var updatedLabel = new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", string.Empty);

        _labelRepositoryMock
            .Setup(repository => repository.UpdateLabelAsync("owner", "repo-a", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, string _, Label value, CancellationToken _) => value with { RepositoryName = repo });

        _labelRepositoryMock
            .Setup(repository => repository.UpdateLabelAsync("owner", "repo-b", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub API failure"));

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.UpdateLabelAsync("owner", ["repo-a", "repo-b", "repo-c"], "priority/urgent", updatedLabel);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("owner", "repo-a", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("owner", "repo-b", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("owner", "repo-c", "priority/urgent", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLabelAsync_MultipleRepositories_DeletesLabelInEachRepository()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.DeleteLabelAsync("owner", It.IsAny<string>(), "status/blocked", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        await sut.DeleteLabelAsync("owner", ["repo-a", "repo-b"], "status/blocked");

        // Assert
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("owner", "repo-a", "status/blocked", It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("owner", "repo-b", "status/blocked", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLabelAsync_LabelMissingInFirstRepository_ThrowsAndStopsFurtherProcessing()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.DeleteLabelAsync("owner", "repo-a", "status/blocked", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Label not found"));

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.DeleteLabelAsync("owner", ["repo-a", "repo-b"], "status/blocked");

        // Assert
        _ = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("owner", "repo-b", "status/blocked", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLabelAsync_SecondRepositoryFails_ThrowsAndStopsFurtherProcessing()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.DeleteLabelAsync("owner", "repo-a", "status/blocked", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _labelRepositoryMock
            .Setup(repository => repository.DeleteLabelAsync("owner", "repo-b", "status/blocked", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub API failure"));

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.DeleteLabelAsync("owner", ["repo-a", "repo-b", "repo-c"], "status/blocked");

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("owner", "repo-a", "status/blocked", It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("owner", "repo-b", "status/blocked", It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("owner", "repo-c", "status/blocked", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncLabelsAsync_ApplyChangesFalse_ReturnsPreviewWithoutMutatingTarget()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("source-owner", "source-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High updated", RepositoryName = "source-repo" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("target-owner", "target-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "priority/high", Colour = "fbca04", Description = "High old", RepositoryName = "target-repo" },
                new Label { Name = "status/obsolete", Colour = "ffffff", Description = "Old", RepositoryName = "target-repo" },
            ]);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: false);

        // Assert
        Assert.Single(result.ToAdd);
        Assert.Single(result.ToUpdate);
        Assert.Single(result.ToDelete);
        Assert.Equal("type/story", result.ToAdd[0].Name);
        Assert.Equal("priority/high", result.ToUpdate[0].Name);
        Assert.Equal("status/obsolete", result.ToDelete[0].Name);
        Assert.Empty(result.Skipped);


        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncLabelsAsync_ApplyChangesTrue_AppliesAddUpdateAndDeleteOperations()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("source-owner", "source-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "source-repo" },
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("target-owner", "target-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "ffffff", Description = "Old Story", RepositoryName = "target-repo" },
                new Label { Name = "status/obsolete", Colour = "ffffff", Description = "Old", RepositoryName = "target-repo" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.UpdateLabelAsync("target-owner", "target-repo", "type/story", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, string name, Label value, CancellationToken _) => value with { Name = name, RepositoryName = repo });

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("target-owner", "target-repo", It.Is<Label>(label => label.Name == "priority/high"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, Label value, CancellationToken _) => value with { RepositoryName = repo });

        _labelRepositoryMock
            .Setup(repository => repository.DeleteLabelAsync("target-owner", "target-repo", "status/obsolete", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true);

        // Assert
        Assert.Single(result.ToAdd);
        Assert.Single(result.ToUpdate);
        Assert.Single(result.ToDelete);
        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync("target-owner", "target-repo", It.Is<Label>(label => label.Name == "priority/high"), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("target-owner", "target-repo", "type/story", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("target-owner", "target-repo", "status/obsolete", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(result.Skipped);


    }

    [Fact]
    public async Task SyncLabelsAsync_RepositoriesAlreadyAligned_ReturnsEmptyDiff()
    {
        // Arrange
        var labels = new[]
        {
            new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "repo" },
            new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "repo" },
        };

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("source-owner", "source-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("target-owner", "target-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true);

        // Assert
        Assert.Empty(result.ToAdd);
        Assert.Empty(result.ToUpdate);
        Assert.Empty(result.ToDelete);
        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(2, result.Skipped.Count);


    }

    [Fact]
    public async Task PreviewLabelSynchronisationAsync_MultipleTargets_ReturnsPerRepositoryPreviews()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("source-owner", "source-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "source-repo" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("target-owner", "target-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "priority/high", Colour = "fbca04", Description = "Old", RepositoryName = "target-a" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("target-owner", "target-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "target-b" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "target-b" },
            ]);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.PreviewLabelSynchronisationAsync(
            "source-owner/source-repo",
            ["target-owner/target-a", "target-owner/target-b"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "target-owner/target-a" && item.ToCreate.Count == 1 && item.ToUpdate.Count == 1 && item.Skipped.Count == 0);
        Assert.Contains(result, item => item.RepositoryFullName == "target-owner/target-b" && item.ToCreate.Count == 0 && item.ToUpdate.Count == 0 && item.Skipped.Count == 2);
    }

    [Fact]
    public async Task ApplyLabelSynchronisationAsync_TargetFails_ReturnsPartialFailureWithoutAbortingOtherTargets()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("source-owner", "source-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("target-owner", "target-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Label>());

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("target-owner", "target-b", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub API failure"));

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("target-owner", "target-a", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, Label label, CancellationToken _) => label with { RepositoryName = repo });

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.ApplyLabelSynchronisationAsync(
            "source-owner/source-repo",
            ["target-owner/target-a", "target-owner/target-b"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "target-owner/target-a" && !item.HasError && item.CreatedCount == 1);
        Assert.Contains(result, item => item.RepositoryFullName == "target-owner/target-b" && item.HasError);

        _labelRepositoryMock.Verify(
            repository => repository.CreateLabelAsync("target-owner", "target-a", It.Is<Label>(label => label.Name == "type/story"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncLabelsAsync_ApplyChangesAndDeleteFails_ThrowsHttpRequestException()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("source-owner", "source-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("target-owner", "target-repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "target-repo" },
                new Label { Name = "status/obsolete", Colour = "ffffff", Description = "Old", RepositoryName = "target-repo" },
            ]);

        _labelRepositoryMock
            .Setup(repository => repository.DeleteLabelAsync("target-owner", "target-repo", "status/obsolete", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub API failure"));

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
    }

    [Fact]
    public async Task GetRecommendedTaxonomyAsync_WhenCalled_ReturnsCanonicalTaxonomy()
    {
        // Arrange
        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.GetRecommendedTaxonomyAsync();

        // Assert
        Assert.Contains(result, label => label.Name == "type/story" && label.Colour == "1d76db");
        Assert.Contains(result, label => label.Name == "type/feature" && label.Description == "A Feature — groups related stories within an epic");
        Assert.Contains(result, label => label.Name == "type/enabler" && label.Description == "An Enabler — technical prerequisite that unblocks stories");
        Assert.Contains(result, label => label.Name == "type/test" && label.Description == "A Test issue — test coverage deliverable (unit, component, integration)");
        Assert.Contains(result, label => label.Name == "priority/high" && label.Colour == "d93f0b");
        Assert.Contains(result, label => label.Name == "priority/critical" && label.Description == "Blocking — must be resolved immediately");
        Assert.Contains(result, label => label.Name == "status/in-progress" && label.Colour == "0e8a16");
        Assert.Contains(result, label => label.Name == "area/labels" && label.Colour == "c5def5");
        Assert.Contains(result, label => label.Name == "size/m" && label.Colour == "fef2c0");
        Assert.Contains(result, label => label.Name == "size/xs" && label.Description == "Trivial — less than 1 hour (e.g. typo fix, config change)");
        Assert.Equal(30, result.Count);
        Assert.Contains(result, label => label.Name.StartsWith("type/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("priority/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("status/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("area/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("size/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetRecommendedLabelStrategiesAsync_WhenCalled_ReturnsBuiltInStrategies()
    {
        // Arrange
        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.GetRecommendedLabelStrategiesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, strategy => strategy.Id == "solodevboard");
        Assert.Contains(result, strategy => strategy.Id == "github-default");
    }

    [Fact]
    public async Task PreviewRecommendedTaxonomyAsync_RepositoryHasMixedState_ReturnsCreateUpdateAndSkipped()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "bug", Colour = "000000", Description = "Outdated", RepositoryName = "repo-a" },
                new Label { Name = "documentation", Colour = "0075ca", Description = "Improvements or additions to documentation", RepositoryName = "repo-a" },
            ]);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.PreviewRecommendedTaxonomyAsync("github-default", ["owner/repo-a"]);

        // Assert
        var preview = Assert.Single(result);
        Assert.Equal("owner/repo-a", preview.RepositoryFullName);
        Assert.Contains(preview.ToCreate, label => label.Name == "enhancement");
        Assert.Contains(preview.ToUpdate, label => label.Name == "bug");
        Assert.Contains(preview.Skipped, label => label.Name == "documentation");
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyAsync_LabelAlreadyMatches_SkipsWithoutMutatingApiCalls()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo-a" },
                new Label { Name = "documentation", Colour = "0075ca", Description = "Improvements or additions to documentation", RepositoryName = "repo-a" },
                new Label { Name = "duplicate", Colour = "cfd3d7", Description = "This issue or pull request already exists", RepositoryName = "repo-a" },
                new Label { Name = "enhancement", Colour = "a2eeef", Description = "New feature or request", RepositoryName = "repo-a" },
                new Label { Name = "good first issue", Colour = "7057ff", Description = "Good for newcomers", RepositoryName = "repo-a" },
                new Label { Name = "help wanted", Colour = "008672", Description = "Extra attention is needed", RepositoryName = "repo-a" },
                new Label { Name = "invalid", Colour = "e4e669", Description = "This does not appear to be valid", RepositoryName = "repo-a" },
                new Label { Name = "question", Colour = "d876e3", Description = "Further information is requested", RepositoryName = "repo-a" },
                new Label { Name = "wontfix", Colour = "ffffff", Description = "This will not be worked on", RepositoryName = "repo-a" },
            ]);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a"]);

        // Assert
        var summary = Assert.Single(result);
        Assert.Equal(0, summary.CreatedCount);
        Assert.Equal(0, summary.UpdatedCount);
        Assert.Equal(9, summary.SkippedCount);
        Assert.False(summary.HasError);
        _labelRepositoryMock.Verify(repository => repository.CreateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyAsync_OneRepositoryFails_ReturnsErrorAndContinues()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Label>());

        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo-b", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Rate limited"));

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("owner", "repo-a", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, Label value, CancellationToken _) => value with { RepositoryName = repo });

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a", "owner/repo-b"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "owner/repo-a" && item.CreatedCount == 9 && !item.HasError);
        Assert.Contains(result, item => item.RepositoryFullName == "owner/repo-b" && item.HasError && item.ErrorMessage == "Rate limited");
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyAsync_OneRepositoryHasInvalidFormat_ReturnsErrorAndContinues()
    {
        // Arrange
        _labelRepositoryMock
            .Setup(repository => repository.GetLabelsAsync("owner", "repo-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Label>());

        _labelRepositoryMock
            .Setup(repository => repository.CreateLabelAsync("owner", "repo-a", It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string repo, Label value, CancellationToken _) => value with { RepositoryName = repo });

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a", "invalid-format"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "owner/repo-a" && item.CreatedCount == 9 && !item.HasError);
        Assert.Contains(result, item => item.RepositoryFullName == "invalid-format" && item.HasError && item.ErrorMessage!.Contains("owner/repository format", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetLabelsForRepositoriesAsync_RepositoriesEmpty_ThrowsArgumentException()
    {
        // Arrange
        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var action = async () => await sut.GetLabelsForRepositoriesAsync("owner", []);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(action);
    }
}
