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
        Assert.Equal("status/obsolete", result.ToDelete[0]);
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
            .Setup(repository => repository.DeleteLabelAsync("target-owner", "target-repo", "status/obsolete", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new LabelService(_labelRepositoryMock.Object);

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true);

        // Assert
        Assert.Empty(result.ToAdd);
        Assert.Single(result.ToUpdate);
        Assert.Single(result.ToDelete);
        _labelRepositoryMock.Verify(repository => repository.UpdateLabelAsync("target-owner", "target-repo", "type/story", It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
        _labelRepositoryMock.Verify(repository => repository.DeleteLabelAsync("target-owner", "target-repo", "status/obsolete", It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.Contains(result, label => label.Name == "priority/high" && label.Colour == "d93f0b");
        Assert.Contains(result, label => label.Name == "status/in-progress" && label.Colour == "0e8a16");
        Assert.Contains(result, label => label.Name == "area/labels" && label.Colour == "c5def5");
        Assert.Contains(result, label => label.Name == "size/m" && label.Colour == "fef2c0");
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
