using NSubstitute;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="LabelService"/>.</summary>
public sealed class LabelServiceTests
{
    private readonly ILabelRepository _labelRepository = Substitute.For<ILabelRepository>();

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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo", cancellationToken)
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "A user-facing Story delivering a discrete piece of value", RepositoryName = "repo" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "Should be addressed in the current sprint or release", RepositoryName = "repo" },
            ]);

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.GetLabelsAsync("owner", "repo", cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("type/story", result[0].Name);
        Assert.Equal("repo", result[0].RepositoryName);
    }

    [Fact]
    public async Task GetLabelsForRepositoriesAsync_MultipleRepositories_ReturnsMergedLabels()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "repo-a" }]);

        _labelRepository
            .GetLabelsAsync("owner", "repo-b", cancellationToken)
            .Returns([new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "repo-b" }]);

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.GetLabelsForRepositoriesAsync("owner", ["repo-a", "repo-b"], cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, label => label is { Name: "type/story", RepositoryName: "repo-a" });
        Assert.Contains(result, label => label is { Name: "priority/high", RepositoryName: "repo-b" });
    }

    [Fact]
    public async Task GetLabelsForRepositoriesAsync_DuplicateRepositoryNames_QueriesEachRepositoryOnce()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "repo-a" }]);

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.GetLabelsForRepositoriesAsync("owner", ["repo-a", "repo-a", "repo-a"], cancellationToken);

        // Assert
        Assert.Single(result);
        await _labelRepository.Received(1).GetLabelsAsync("owner", "repo-a", cancellationToken);
    }

    [Fact]
    public async Task GetLabelsForRepositoriesAsync_OwnerIsWhitespace_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.GetLabelsForRepositoriesAsync(" ", ["repo-a"], cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task CreateLabelAsync_MultipleRepositories_CreatesLabelInEachRepository()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var label = new LabelDto("area/labels", "c5def5", "Label Manager feature", string.Empty);

        _labelRepository
            .CreateLabelAsync("owner", Arg.Any<string>(), Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var value = callInfo.ArgAt<Label>(2);
                return value with { RepositoryName = repo };
            });

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.CreateLabelAsync("owner", ["repo-a", "repo-b"], label, cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        await _labelRepository.Received(1).CreateLabelAsync("owner", "repo-a", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.Received(1).CreateLabelAsync("owner", "repo-b", Arg.Any<Label>(), cancellationToken);
    }

    [Fact]
    public async Task CreateLabelAsync_SecondRepositoryFails_ThrowsAndStopsFurtherProcessing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var label = new LabelDto("area/labels", "c5def5", "Label Manager feature", string.Empty);

        _labelRepository
            .CreateLabelAsync("owner", "repo-a", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var value = callInfo.ArgAt<Label>(2);
                return value with { RepositoryName = repo };
            });

        _labelRepository
            .CreateLabelAsync("owner", "repo-b", Arg.Any<Label>(), cancellationToken)
            .Returns(Task.FromException<Label>(new HttpRequestException("GitHub API failure")));

        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.CreateLabelAsync("owner", ["repo-a", "repo-b", "repo-c"], label, cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
        await _labelRepository.Received(1).CreateLabelAsync("owner", "repo-a", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.Received(1).CreateLabelAsync("owner", "repo-b", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().CreateLabelAsync("owner", "repo-c", Arg.Any<Label>(), cancellationToken);
    }

    [Fact]
    public async Task CreateLabelAsync_LabelIsNull_ThrowsArgumentNullException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.CreateLabelAsync("owner", ["repo-a"], null!, cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task CreateLabelAsync_RepositoriesEmpty_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new LabelService(_labelRepository);
        var label = new LabelDto("area/labels", "c5def5", "Label Manager feature", string.Empty);

        // Act
        var action = async () => await sut.CreateLabelAsync("owner", [], label, cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task UpdateLabelAsync_MultipleRepositories_UpdatesLabelInEachRepository()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var updatedLabel = new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", string.Empty);

        _labelRepository
            .UpdateLabelAsync("owner", Arg.Any<string>(), "priority/urgent", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var value = callInfo.ArgAt<Label>(3);
                return value with { RepositoryName = repo };
            });

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.UpdateLabelAsync("owner", ["repo-a", "repo-b"], "priority/urgent", updatedLabel, cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        await _labelRepository.Received(1).UpdateLabelAsync("owner", "repo-a", "priority/urgent", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.Received(1).UpdateLabelAsync("owner", "repo-b", "priority/urgent", Arg.Any<Label>(), cancellationToken);
    }

    [Fact]
    public async Task UpdateLabelAsync_RepositoryReportsLabelMissing_ThrowsKeyNotFoundException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var updatedLabel = new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", string.Empty);

        _labelRepository
            .UpdateLabelAsync("owner", "repo-a", "priority/urgent", Arg.Any<Label>(), cancellationToken)
            .Returns(Task.FromException<Label>(new KeyNotFoundException("Label not found")));

        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.UpdateLabelAsync("owner", ["repo-a", "repo-b"], "priority/urgent", updatedLabel, cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        await _labelRepository.DidNotReceive().UpdateLabelAsync("owner", "repo-b", "priority/urgent", Arg.Any<Label>(), cancellationToken);
    }

    [Fact]
    public async Task UpdateLabelAsync_SecondRepositoryFails_ThrowsAndStopsFurtherProcessing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var updatedLabel = new LabelDto("priority/high", "d93f0b", "Should be addressed in the current sprint or release", string.Empty);

        _labelRepository
            .UpdateLabelAsync("owner", "repo-a", "priority/urgent", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var value = callInfo.ArgAt<Label>(3);
                return value with { RepositoryName = repo };
            });

        _labelRepository
            .UpdateLabelAsync("owner", "repo-b", "priority/urgent", Arg.Any<Label>(), cancellationToken)
            .Returns(Task.FromException<Label>(new HttpRequestException("GitHub API failure")));

        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.UpdateLabelAsync("owner", ["repo-a", "repo-b", "repo-c"], "priority/urgent", updatedLabel, cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
        await _labelRepository.Received(1).UpdateLabelAsync("owner", "repo-a", "priority/urgent", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.Received(1).UpdateLabelAsync("owner", "repo-b", "priority/urgent", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync("owner", "repo-c", "priority/urgent", Arg.Any<Label>(), cancellationToken);
    }

    [Fact]
    public async Task DeleteLabelAsync_MultipleRepositories_DeletesLabelInEachRepository()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .DeleteLabelAsync("owner", Arg.Any<string>(), "status/blocked", cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = new LabelService(_labelRepository);

        // Act
        await sut.DeleteLabelAsync("owner", ["repo-a", "repo-b"], "status/blocked", cancellationToken);

        // Assert
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-a", "status/blocked", cancellationToken);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-b", "status/blocked", cancellationToken);
    }

    [Fact]
    public async Task DeleteLabelAsync_LabelMissingInFirstRepository_ThrowsAndStopsFurtherProcessing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", "status/blocked", cancellationToken)
            .Returns(Task.FromException(new KeyNotFoundException("Label not found")));

        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.DeleteLabelAsync("owner", ["repo-a", "repo-b"], "status/blocked", cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        await _labelRepository.DidNotReceive().DeleteLabelAsync("owner", "repo-b", "status/blocked", cancellationToken);
    }

    [Fact]
    public async Task DeleteLabelAsync_SecondRepositoryFails_ThrowsAndStopsFurtherProcessing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", "status/blocked", cancellationToken)
            .Returns(Task.CompletedTask);

        _labelRepository
            .DeleteLabelAsync("owner", "repo-b", "status/blocked", cancellationToken)
            .Returns(Task.FromException(new HttpRequestException("GitHub API failure")));

        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.DeleteLabelAsync("owner", ["repo-a", "repo-b", "repo-c"], "status/blocked", cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-a", "status/blocked", cancellationToken);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-b", "status/blocked", cancellationToken);
        await _labelRepository.DidNotReceive().DeleteLabelAsync("owner", "repo-c", "status/blocked", cancellationToken);
    }

    [Fact]
    public async Task SyncLabelsAsync_ApplyChangesFalse_ReturnsPreviewWithoutMutatingTarget()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("source-owner", "source-repo", cancellationToken)
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High updated", RepositoryName = "source-repo" },
            ]);

        _labelRepository
            .GetLabelsAsync("target-owner", "target-repo", cancellationToken)
            .Returns([
                new Label { Name = "priority/high", Colour = "fbca04", Description = "High old", RepositoryName = "target-repo" },
                new Label { Name = "status/obsolete", Colour = "ffffff", Description = "Old", RepositoryName = "target-repo" },
            ]);

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: false, cancellationToken);

        // Assert
        Assert.Single(result.ToAdd);
        Assert.Single(result.ToUpdate);
        Assert.Single(result.ToDelete);
        Assert.Equal("type/story", result.ToAdd[0].Name);
        Assert.Equal("priority/high", result.ToUpdate[0].Name);
        Assert.Equal("status/obsolete", result.ToDelete[0].Name);
        Assert.Empty(result.Skipped);

        await _labelRepository.DidNotReceive().CreateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken);
    }

    [Fact]
    public async Task SyncLabelsAsync_ApplyChangesTrue_AppliesAddUpdateAndDeleteOperations()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("source-owner", "source-repo", cancellationToken)
            .Returns([
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "source-repo" },
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
            ]);

        _labelRepository
            .GetLabelsAsync("target-owner", "target-repo", cancellationToken)
            .Returns([
                new Label { Name = "type/story", Colour = "ffffff", Description = "Old Story", RepositoryName = "target-repo" },
                new Label { Name = "status/obsolete", Colour = "ffffff", Description = "Old", RepositoryName = "target-repo" },
            ]);

        _labelRepository
            .UpdateLabelAsync("target-owner", "target-repo", "type/story", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var name = callInfo.ArgAt<string>(2);
                var value = callInfo.ArgAt<Label>(3);
                return value with { Name = name, RepositoryName = repo };
            });

        _labelRepository
            .CreateLabelAsync("target-owner", "target-repo", Arg.Is<Label>(label => label!.Name == "priority/high"), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var value = callInfo.ArgAt<Label>(2);
                return value with { RepositoryName = repo };
            });

        _labelRepository
            .DeleteLabelAsync("target-owner", "target-repo", "status/obsolete", cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true, cancellationToken);

        // Assert
        Assert.Single(result.ToAdd);
        Assert.Single(result.ToUpdate);
        Assert.Single(result.ToDelete);
        await _labelRepository.Received(1).CreateLabelAsync("target-owner", "target-repo", Arg.Is<Label>(label => label!.Name == "priority/high"), cancellationToken);
        await _labelRepository.Received(1).UpdateLabelAsync("target-owner", "target-repo", "type/story", Arg.Any<Label>(), cancellationToken);
        await _labelRepository.Received(1).DeleteLabelAsync("target-owner", "target-repo", "status/obsolete", cancellationToken);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task SyncLabelsAsync_RepositoriesAlreadyAligned_ReturnsEmptyDiff()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var labels = new[]
        {
            new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "repo" },
            new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "repo" },
        };

        _labelRepository
            .GetLabelsAsync("source-owner", "source-repo", cancellationToken)
            .Returns(labels);

        _labelRepository
            .GetLabelsAsync("target-owner", "target-repo", cancellationToken)
            .Returns(labels);

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true, cancellationToken);

        // Assert
        Assert.Empty(result.ToAdd);
        Assert.Empty(result.ToUpdate);
        Assert.Empty(result.ToDelete);
        await _labelRepository.DidNotReceive().CreateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken);
        Assert.Equal(2, result.Skipped.Count);
    }

    [Fact]
    public async Task PreviewLabelSynchronisationAsync_MultipleTargets_ReturnsPerRepositoryPreviews()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("source-owner", "source-repo", cancellationToken)
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "source-repo" },
            ]);

        _labelRepository
            .GetLabelsAsync("target-owner", "target-a", cancellationToken)
            .Returns([
                new Label { Name = "priority/high", Colour = "fbca04", Description = "Old", RepositoryName = "target-a" },
            ]);

        _labelRepository
            .GetLabelsAsync("target-owner", "target-b", cancellationToken)
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "target-b" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High", RepositoryName = "target-b" },
            ]);

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.PreviewLabelSynchronisationAsync(
            "source-owner/source-repo",
            ["target-owner/target-a", "target-owner/target-b"], cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "target-owner/target-a" && item.ToCreate.Count == 1 && item.ToUpdate.Count == 1 && item.Skipped.Count == 0);
        Assert.Contains(result, item => item.RepositoryFullName == "target-owner/target-b" && item.ToCreate.Count == 0 && item.ToUpdate.Count == 0 && item.Skipped.Count == 2);
    }

    [Fact]
    public async Task ApplyLabelSynchronisationAsync_TargetFails_ReturnsPartialFailureWithoutAbortingOtherTargets()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("source-owner", "source-repo", cancellationToken)
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
            ]);

        _labelRepository
            .GetLabelsAsync("target-owner", "target-a", cancellationToken)
            .Returns(Array.Empty<Label>());

        _labelRepository
            .GetLabelsAsync("target-owner", "target-b", cancellationToken)
            .Returns(Task.FromException<IReadOnlyList<Label>>(new HttpRequestException("GitHub API failure")));

        _labelRepository
            .CreateLabelAsync("target-owner", "target-a", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var label = callInfo.ArgAt<Label>(2);
                return label with { RepositoryName = repo };
            });

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.ApplyLabelSynchronisationAsync(
            "source-owner/source-repo",
            ["target-owner/target-a", "target-owner/target-b"], cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "target-owner/target-a" && !item.HasError && item.CreatedCount == 1);
        Assert.Contains(result, item => item.RepositoryFullName == "target-owner/target-b" && item.HasError);

        await _labelRepository.Received(1).CreateLabelAsync("target-owner", "target-a", Arg.Is<Label>(label => label!.Name == "type/story"), cancellationToken);
    }

    [Fact]
    public async Task SyncLabelsAsync_ApplyChangesAndDeleteFails_ThrowsHttpRequestException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("source-owner", "source-repo", cancellationToken)
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "source-repo" },
            ]);

        _labelRepository
            .GetLabelsAsync("target-owner", "target-repo", cancellationToken)
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "target-repo" },
                new Label { Name = "status/obsolete", Colour = "ffffff", Description = "Old", RepositoryName = "target-repo" },
            ]);

        _labelRepository
            .DeleteLabelAsync("target-owner", "target-repo", "status/obsolete", cancellationToken)
            .Returns(Task.FromException(new HttpRequestException("GitHub API failure")));

        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true, cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
    }

    [Fact]
    public async Task GetRecommendedTaxonomyAsync_WhenCalled_ReturnsCanonicalTaxonomy()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.GetRecommendedTaxonomyAsync(cancellationToken);

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
        Assert.Equal(31, result.Count);
        Assert.Contains(result, label => label.Name.StartsWith("type/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("priority/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("status/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("area/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("size/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetRecommendedLabelStrategiesAsync_WhenCalled_ReturnsBuiltInStrategies()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.GetRecommendedLabelStrategiesAsync(cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, strategy => strategy.Id == "solodevboard");
        Assert.Contains(result, strategy => strategy.Id == "github-default");
    }

    [Fact]
    public async Task PreviewRecommendedTaxonomyAsync_RepositoryHasMixedState_ReturnsCreateUpdateAndSkipped()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
                new Label { Name = "bug", Colour = "000000", Description = "Outdated", RepositoryName = "repo-a" },
                new Label { Name = "documentation", Colour = "0075ca", Description = "Improvements or additions to documentation", RepositoryName = "repo-a" },
            ]);

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.PreviewRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], cancellationToken);

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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
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

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], cancellationToken);

        // Assert
        var summary = Assert.Single(result);
        Assert.Equal(0, summary.CreatedCount);
        Assert.Equal(0, summary.UpdatedCount);
        Assert.Equal(9, summary.SkippedCount);
        Assert.False(summary.HasError);
        await _labelRepository.DidNotReceive().CreateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyAsync_OneRepositoryFails_ReturnsErrorAndContinues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns(Array.Empty<Label>());

        _labelRepository
            .GetLabelsAsync("owner", "repo-b", cancellationToken)
            .Returns(Task.FromException<IReadOnlyList<Label>>(new HttpRequestException("Rate limited")));

        _labelRepository
            .CreateLabelAsync("owner", "repo-a", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var value = callInfo.ArgAt<Label>(2);
                return value with { RepositoryName = repo };
            });

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a", "owner/repo-b"], cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "owner/repo-a" && item.CreatedCount == 9 && !item.HasError);
        Assert.Contains(result, item => item.RepositoryFullName == "owner/repo-b" && item.HasError && item.ErrorMessage == "Rate limited");
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyAsync_OneRepositoryHasInvalidFormat_ReturnsErrorAndContinues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns(Array.Empty<Label>());

        _labelRepository
            .CreateLabelAsync("owner", "repo-a", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo =>
            {
                var repo = callInfo.ArgAt<string>(1);
                var value = callInfo.ArgAt<Label>(2);
                return value with { RepositoryName = repo };
            });

        var sut = new LabelService(_labelRepository);

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a", "invalid-format"], cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "owner/repo-a" && item.CreatedCount == 9 && !item.HasError);
        Assert.Contains(result, item => item.RepositoryFullName == "invalid-format" && item.HasError && item.ErrorMessage!.Contains("owner/repository format", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetLabelsForRepositoriesAsync_RepositoriesEmpty_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new LabelService(_labelRepository);

        // Act
        var action = async () => await sut.GetLabelsForRepositoriesAsync("owner", [], cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(action);
    }
}
