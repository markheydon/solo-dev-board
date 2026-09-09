using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="LabelService"/>.</summary>
public sealed class LabelServiceTests
{
    private readonly ILabelRepository _labelRepository = Substitute.For<ILabelRepository>();
    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();

    [Fact]
    public void Constructor_LabelRepositoryIsNull_ThrowsArgumentNullException()
    {
        ILabelRepository? labelRepository = null;

        var action = () => _ = new LabelService(labelRepository!, _gitHubService);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_GitHubServiceIsNull_ThrowsArgumentNullException()
    {
        IGitHubService? gitHubService = null;

        var action = () => _ = new LabelService(_labelRepository, gitHubService!);

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

        var sut = CreateSut();

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

        var sut = CreateSut();

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

        var sut = CreateSut();

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
        var sut = CreateSut();

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

        var sut = CreateSut();

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

        var sut = CreateSut();

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
        var sut = CreateSut();

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
        var sut = CreateSut();
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

        var sut = CreateSut();

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

        var sut = CreateSut();

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

        var sut = CreateSut();

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

        var sut = CreateSut();

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

        var sut = CreateSut();

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

        var sut = CreateSut();

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

        var sut = CreateSut();

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: false, keepAreaLabels: true, cancellationToken);

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

        var sut = CreateSut();

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true, keepAreaLabels: true, cancellationToken);

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

        var sut = CreateSut();

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true, keepAreaLabels: true, cancellationToken);

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
    public async Task SyncLabelsAsync_WhenKeepAreaLabelsEnabled_KeepsAreaOrphansOnTarget()
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
                new Label { Name = "area/docs", Colour = "0052cc", Description = "Documentation", RepositoryName = "target-repo" },
                new Label { Name = "legacy", Colour = "ffffff", Description = "Legacy", RepositoryName = "target-repo" },
            ]);

        var sut = CreateSut();

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: false, keepAreaLabels: true, cancellationToken);

        // Assert
        Assert.Single(result.KeptAreaLabels);
        Assert.Equal("area/docs", result.KeptAreaLabels[0].Name);
        Assert.Single(result.ToDelete);
        Assert.Equal("legacy", result.ToDelete[0].Name);
    }

    [Fact]
    public async Task SyncLabelsAsync_WhenKeepAreaLabelsDisabled_DeletesAreaOrphansOnTarget()
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
                new Label { Name = "area/docs", Colour = "0052cc", Description = "Documentation", RepositoryName = "target-repo" },
            ]);

        _labelRepository
            .DeleteLabelAsync("target-owner", "target-repo", Arg.Any<string>(), cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true, keepAreaLabels: false, cancellationToken);

        // Assert
        Assert.Empty(result.KeptAreaLabels);
        Assert.Single(result.ToDelete);
        Assert.Equal("area/docs", result.ToDelete[0].Name);
        await _labelRepository.Received(1).DeleteLabelAsync("target-owner", "target-repo", "area/docs", cancellationToken);
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

        var sut = CreateSut();

        // Act
        var result = await sut.PreviewLabelSynchronisationAsync(
            "source-owner/source-repo",
            ["target-owner/target-a", "target-owner/target-b"],
            keepAreaLabels: true,
            cancellationToken);

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

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyLabelSynchronisationAsync(
            "source-owner/source-repo",
            ["target-owner/target-a", "target-owner/target-b"],
            keepAreaLabels: true,
            cancellationToken);

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

        var sut = CreateSut();

        // Act
        var action = async () => await sut.SyncLabelsAsync("source-owner", "source-repo", "target-owner", "target-repo", applyChanges: true, keepAreaLabels: true, cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(action);
    }

    [Fact]
    public async Task GetRecommendedTaxonomyAsync_WhenCalled_ReturnsCanonicalTaxonomy()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetRecommendedTaxonomyAsync(cancellationToken);

        // Assert
        Assert.Contains(result, label => label.Name == "type/story" && label.Colour == "1d76db");
        Assert.Contains(result, label => label.Name == "type/feature" && label.Description == "A Feature - groups related stories within an epic");
        Assert.Contains(result, label => label.Name == "type/enabler" && label.Description == "An Enabler - technical prerequisite that unblocks stories");
        Assert.Contains(result, label => label.Name == "type/test" && label.Description == "A Test issue - test coverage deliverable (unit, component, integration)");
        Assert.Contains(result, label => label.Name == "priority/high" && label.Colour == "d93f0b");
        Assert.Contains(result, label => label.Name == "priority/critical" && label.Description == "Blocking - must be resolved immediately");
        Assert.Contains(result, label => label.Name == "status/in-progress" && label.Colour == "0e8a16");
        Assert.Contains(result, label => label.Name == "size/m" && label.Colour == "fef2c0");
        Assert.Contains(result, label => label.Name == "size/xs" && label.Description == "Trivial - less than 1 hour (e.g. typo fix, config change)");
        Assert.Equal(23, result.Count);
        Assert.Contains(result, label => label.Name.StartsWith("type/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("priority/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("status/", StringComparison.Ordinal));
        Assert.DoesNotContain(result, label => label.Name.StartsWith("area/", StringComparison.Ordinal));
        Assert.Contains(result, label => label.Name.StartsWith("size/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetRecommendedLabelStrategiesAsync_WhenCalled_ReturnsBuiltInStrategies()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

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

        var sut = CreateSut();

        // Act
        var result = await sut.PreviewRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], cancellationToken: cancellationToken);

        // Assert
        var preview = Assert.Single(result);
        Assert.Equal("owner/repo-a", preview.RepositoryFullName);
        Assert.Contains(preview.ToCreate, label => label.Name == "enhancement");
        Assert.Contains(preview.ToUpdate, label => label.Name == "bug");
        Assert.Contains(preview.Skipped, label => label.Name == "documentation");
        Assert.Empty(preview.ToDelete);
        Assert.Empty(preview.ToRemap);
    }

    [Fact]
    public async Task PreviewRecommendedTaxonomyAsync_WhenRemoveOutsideTaxonomyDisabled_ReturnsNoLabelsToDelete()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo-a" },
                new Label { Name = "dependencies", Colour = "0366d6", Description = "Pull requests that update a dependency", RepositoryName = "repo-a" },
                new Label { Name = "area/docs", Colour = "0052cc", Description = "Documentation", RepositoryName = "repo-a" },
            ]);

        var sut = CreateSut();

        // Act
        var result = await sut.PreviewRecommendedTaxonomyAsync(
            "github-default",
            ["owner/repo-a"],
            removeLabelsOutsideTaxonomy: false,
            keepAreaLabels: true,
            cancellationToken: cancellationToken);

        // Assert
        var preview = Assert.Single(result);
        Assert.Empty(preview.ToDelete);
        Assert.Empty(preview.ToRemap);
        Assert.Empty(preview.KeptAreaLabels);
        Assert.Contains(preview.Skipped, label => label.Name == "bug");
        Assert.DoesNotContain(preview.ToDelete, label => label.Name == "dependencies");
        Assert.DoesNotContain(preview.ToDelete, label => label.Name == "area/docs");
    }

    [Fact]
    public async Task PreviewRecommendedTaxonomyAsync_WhenRemoveOutsideTaxonomyEnabled_ReturnsLabelsToDelete()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo-a" },
                new Label { Name = "dependencies", Colour = "0366d6", Description = "Pull requests that update a dependency", RepositoryName = "repo-a" },
                new Label { Name = "epic", Colour = "5319e7", Description = "Legacy epic label", RepositoryName = "repo-a" },
            ]);

        var sut = CreateSut();

        // Act
        var result = await sut.PreviewRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], removeLabelsOutsideTaxonomy: true, keepAreaLabels: true, cancellationToken: cancellationToken);

        // Assert
        var preview = Assert.Single(result);
        Assert.Contains(preview.ToDelete, label => label.Name == "dependencies");
        Assert.Contains(preview.ToDelete, label => label.Name == "epic");
        Assert.DoesNotContain(preview.ToDelete, label => label.Name == "bug");
        Assert.Empty(preview.ToRemap);
    }

    [Fact]
    public async Task PreviewRecommendedTaxonomyAsync_WhenRemoveOutsideTaxonomyEnabled_ReturnsUsedExtrasInToRemap()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo-a" },
                new Label { Name = "dependencies", Colour = "0366d6", Description = "Pull requests that update a dependency", RepositoryName = "repo-a" },
                new Label { Name = "epic", Colour = "5319e7", Description = "Legacy epic label", RepositoryName = "repo-a" },
            ]);

        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo-a", "dependencies", cancellationToken)
            .Returns([new LabelledWorkItem { Number = 42 }]);

        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo-a", "epic", cancellationToken)
            .Returns([]);

        var sut = CreateSut();

        // Act
        var result = await sut.PreviewRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], removeLabelsOutsideTaxonomy: true, keepAreaLabels: true, cancellationToken: cancellationToken);

        // Assert
        var preview = Assert.Single(result);
        Assert.Contains(preview.ToRemap, label => label.Name == "dependencies");
        Assert.Contains(preview.ToDelete, label => label.Name == "epic");
        Assert.DoesNotContain(preview.ToRemap, label => label.Name == "epic");
        Assert.DoesNotContain(preview.ToDelete, label => label.Name == "dependencies");
    }

    [Fact]
    public async Task PreviewRecommendedTaxonomyAsync_WhenClassifyingExtras_ReportsProgressMessages()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo-a" },
                new Label { Name = "dependencies", Colour = "0366d6", Description = "Pull requests that update a dependency", RepositoryName = "repo-a" },
                new Label { Name = "epic", Colour = "5319e7", Description = "Legacy epic label", RepositoryName = "repo-a" },
            ]);

        _labelRepository
            .GetWorkItemsWithLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken)
            .Returns([]);

        var messages = new SynchronousProgressMessageList();
        var progress = messages.CreateProgress();
        var sut = CreateSut();

        // Act
        await sut.PreviewRecommendedTaxonomyAsync(
            "github-default",
            ["owner/repo-a"],
            removeLabelsOutsideTaxonomy: true,
            keepAreaLabels: true,
            progress: progress,
            cancellationToken: cancellationToken);

        // Assert
        Assert.Contains(messages, message => message.Contains("Previewing taxonomy changes", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("Loading labels for owner/repo-a", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("Checking extra label usage on owner/repo-a", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PreviewRecommendedTaxonomyAsync_WhenRemoveOutsideTaxonomyEnabled_WithKeepAreaLabels_KeepsAreaOrphans()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo-a" },
                new Label { Name = "dependencies", Colour = "0366d6", Description = "Pull requests that update a dependency", RepositoryName = "repo-a" },
                new Label { Name = "area/docs", Colour = "0052cc", Description = "Documentation", RepositoryName = "repo-a" },
            ]);

        var sut = CreateSut();

        // Act
        var result = await sut.PreviewRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], removeLabelsOutsideTaxonomy: true, keepAreaLabels: true, cancellationToken: cancellationToken);

        // Assert
        var preview = Assert.Single(result);
        Assert.Contains(preview.ToDelete, label => label.Name == "dependencies");
        Assert.Contains(preview.KeptAreaLabels, label => label.Name == "area/docs");
        Assert.DoesNotContain(preview.ToDelete, label => label.Name == "area/docs");
    }

    [Fact]
    public async Task PreviewRecommendedTaxonomyAsync_WhenRemoveOutsideTaxonomyEnabled_WithoutKeepAreaLabels_DeletesAreaOrphans()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo-a" },
                new Label { Name = "area/docs", Colour = "0052cc", Description = "Documentation", RepositoryName = "repo-a" },
            ]);

        var sut = CreateSut();

        // Act
        var result = await sut.PreviewRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], removeLabelsOutsideTaxonomy: true, keepAreaLabels: false, cancellationToken: cancellationToken);

        // Assert
        var preview = Assert.Single(result);
        Assert.Contains(preview.ToDelete, label => label.Name == "area/docs");
        Assert.Empty(preview.KeptAreaLabels);
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyAsync_WhenRemoveOutsideTaxonomyEnabled_DeletesExtraneousLabels()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Something is not working", RepositoryName = "repo-a" },
                new Label { Name = "dependencies", Colour = "0366d6", Description = "Pull requests that update a dependency", RepositoryName = "repo-a" },
                new Label { Name = "documentation", Colour = "0075ca", Description = "Improvements or additions to documentation", RepositoryName = "repo-a" },
                new Label { Name = "duplicate", Colour = "cfd3d7", Description = "This issue or pull request already exists", RepositoryName = "repo-a" },
                new Label { Name = "enhancement", Colour = "a2eeef", Description = "New feature or request", RepositoryName = "repo-a" },
                new Label { Name = "good first issue", Colour = "7057ff", Description = "Good for newcomers", RepositoryName = "repo-a" },
                new Label { Name = "help wanted", Colour = "008672", Description = "Extra attention is needed", RepositoryName = "repo-a" },
                new Label { Name = "invalid", Colour = "e4e669", Description = "This does not appear to be valid", RepositoryName = "repo-a" },
                new Label { Name = "question", Colour = "d876e3", Description = "Further information is requested", RepositoryName = "repo-a" },
                new Label { Name = "wontfix", Colour = "ffffff", Description = "This will not be worked on", RepositoryName = "repo-a" },
                new Label { Name = "epic", Colour = "5319e7", Description = "Legacy epic label", RepositoryName = "repo-a" },
            ]);

        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", Arg.Any<string>(), cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], removeLabelsOutsideTaxonomy: true, keepAreaLabels: true, cancellationToken: cancellationToken);

        // Assert
        var summary = Assert.Single(result);
        Assert.Equal(2, summary.DeletedCount);
        Assert.Empty(summary.DeleteErrors);
        Assert.False(summary.HasError);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-a", "dependencies", cancellationToken);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-a", "epic", cancellationToken);
        await _labelRepository.DidNotReceive().DeleteLabelAsync("owner", "repo-a", "bug", cancellationToken);
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyAsync_WhenDeleteFails_RecordsPerLabelErrorAndContinues()
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
                new Label { Name = "epic", Colour = "5319e7", Description = "Legacy epic label", RepositoryName = "repo-a" },
                new Label { Name = "story", Colour = "1d76db", Description = "Legacy story label", RepositoryName = "repo-a" },
            ]);

        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", "epic", cancellationToken)
            .Returns(Task.FromException(new HttpRequestException("Label is still in use")));

        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", "story", cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], removeLabelsOutsideTaxonomy: true, keepAreaLabels: true, cancellationToken: cancellationToken);

        // Assert
        var summary = Assert.Single(result);
        Assert.Equal(1, summary.DeletedCount);
        Assert.True(summary.HasError);
        var deleteError = Assert.Single(summary.DeleteErrors);
        Assert.Equal("epic", deleteError.LabelName);
        Assert.Equal("Label is still in use", deleteError.ErrorMessage);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-a", "story", cancellationToken);
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

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a"], cancellationToken: cancellationToken);

        // Assert
        var summary = Assert.Single(result);
        Assert.Equal(0, summary.CreatedCount);
        Assert.Equal(0, summary.UpdatedCount);
        Assert.Equal(0, summary.DeletedCount);
        Assert.Equal(9, summary.SkippedCount);
        Assert.Empty(summary.DeleteErrors);
        Assert.False(summary.HasError);
        await _labelRepository.DidNotReceive().CreateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), cancellationToken);
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken);
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

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a", "owner/repo-b"], cancellationToken: cancellationToken);

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

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyRecommendedTaxonomyAsync("github-default", ["owner/repo-a", "invalid-format"], cancellationToken: cancellationToken);

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
        var sut = CreateSut();

        // Act
        var action = async () => await sut.GetLabelsForRepositoriesAsync("owner", [], cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task BulkDeleteLabelsAsync_MultipleTargets_DeletesEachLabelRepositoryPair()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var targets = new[]
        {
            new LabelBulkDeleteTargetDto("bug", ["owner/repo-a", "owner/repo-b"]),
            new LabelBulkDeleteTargetDto("enhancement", ["owner/repo-a"]),
        };

        // Act
        var result = await sut.BulkDeleteLabelsAsync(targets, cancellationToken);

        // Assert
        Assert.Equal(3, result.DeletedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.Errors);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-a", "bug", cancellationToken);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-b", "bug", cancellationToken);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-a", "enhancement", cancellationToken);
    }

    [Fact]
    public async Task BulkDeleteLabelsAsync_LabelMissingInRepository_CountsAsSkippedAndContinues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", "bug", cancellationToken)
            .Returns(Task.FromException(new KeyNotFoundException("Label not found")));

        _labelRepository
            .DeleteLabelAsync("owner", "repo-b", "bug", cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var targets = new[] { new LabelBulkDeleteTargetDto("bug", ["owner/repo-a", "owner/repo-b"]) };

        // Act
        var result = await sut.BulkDeleteLabelsAsync(targets, cancellationToken);

        // Assert
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task BulkDeleteLabelsAsync_OneDeleteFails_RecordsErrorAndContinues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", "bug", cancellationToken)
            .Returns(Task.FromException(new HttpRequestException("GitHub API failure")));

        _labelRepository
            .DeleteLabelAsync("owner", "repo-b", "bug", cancellationToken)
            .Returns(Task.CompletedTask);

        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", "enhancement", cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var targets = new[]
        {
            new LabelBulkDeleteTargetDto("bug", ["owner/repo-a", "owner/repo-b"]),
            new LabelBulkDeleteTargetDto("enhancement", ["owner/repo-a"]),
        };

        // Act
        var result = await sut.BulkDeleteLabelsAsync(targets, cancellationToken);

        // Assert
        Assert.Equal(2, result.DeletedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Single(result.Errors);
        Assert.Equal("bug", result.Errors[0].LabelName);
        Assert.Equal("owner/repo-a", result.Errors[0].RepositoryFullName);
        Assert.Contains("GitHub API failure", result.Errors[0].ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BulkDeleteLabelsAsync_TargetsEmpty_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var action = async () => await sut.BulkDeleteLabelsAsync([], cancellationToken);

        // Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task GetLabelMatrixAsync_RepositoryFullNamesNull_ThrowsArgumentNullException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        var action = async () => await sut.GetLabelMatrixAsync(null!, cancellationToken);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task GetLabelMatrixAsync_EmptySelection_ReturnsEmptyWithoutCallingRepository()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        var result = await sut.GetLabelMatrixAsync([], cancellationToken);

        Assert.Empty(result);
        await _labelRepository.DidNotReceive().GetLabelsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task GetLabelMatrixAsync_MultipleOwners_GroupsRowsAndComputesMissingRepositories()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _labelRepository
            .GetLabelsAsync("owner-a", "repo-a", cancellationToken, Arg.Any<bool>())
            .Returns([
                new Label { Name = "type/story", Colour = "1d76db", Description = "Story label", RepositoryName = "repo-a" },
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High priority", RepositoryName = "repo-a" },
            ]);
        _labelRepository
            .GetLabelsAsync("owner-b", "repo-b", cancellationToken, Arg.Any<bool>())
            .Returns([
                new Label { Name = "priority/high", Colour = "d93f0b", Description = "High priority", RepositoryName = "repo-b" },
            ]);

        var sut = CreateSut();

        var result = await sut.GetLabelMatrixAsync(["owner-b/repo-b", "owner-a/repo-a"], cancellationToken);

        Assert.Equal(2, result.Count);

        var story = Assert.Single(result, row => row.Name == "type/story");
        Assert.Equal("1d76db", story.Colour);
        Assert.Equal("Story label", story.Description);
        Assert.Equal(["owner-a/repo-a"], story.RepositoriesWithLabel);
        Assert.Equal(["owner-b/repo-b"], story.MissingRepositories);
        Assert.Equal("owner-a/repo-a", story.RepositoriesWithLabelText);
        Assert.Equal("owner-b/repo-b", story.MissingRepositoriesText);

        var priority = Assert.Single(result, row => row.Name == "priority/high");
        Assert.Equal(["owner-a/repo-a", "owner-b/repo-b"], priority.RepositoriesWithLabel);
        Assert.Empty(priority.MissingRepositories);
        Assert.Equal("None", priority.MissingRepositoriesText);

        await _labelRepository.Received(1).GetLabelsAsync("owner-a", "repo-a", cancellationToken, false);
        await _labelRepository.Received(1).GetLabelsAsync("owner-b", "repo-b", cancellationToken, false);
    }

    [Fact]
    public async Task GetLabelMatrixAsync_EmptyDescription_UsesMissingDescriptionDisplay()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken, Arg.Any<bool>())
            .Returns([new Label { Name = "bug", Colour = "d73a4a", Description = " ", RepositoryName = "repo-a" }]);

        var sut = CreateSut();

        var result = await sut.GetLabelMatrixAsync(["owner/repo-a"], cancellationToken);

        var row = Assert.Single(result);
        Assert.Equal(LabelMatrixRowDto.MissingDescriptionDisplay, row.Description);
    }

    [Fact]
    public async Task GetLabelMatrixAsync_ForceReloadTrue_PassesFlagToRepository()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken, true)
            .Returns([new Label { Name = "bug", Colour = "d73a4a", Description = "Bug", RepositoryName = "repo-a" }]);

        var sut = CreateSut();

        _ = await sut.GetLabelMatrixAsync(["owner/repo-a"], cancellationToken, forceReload: true);

        await _labelRepository.Received(1).GetLabelsAsync("owner", "repo-a", cancellationToken, true);
    }

    [Fact]
    public async Task RemapLabelAsync_WhenRetaggingItems_ReportsProgressMessages()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ArrangeLabels("story", "type/story");
        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo", "story", cancellationToken)
            .Returns([new LabelledWorkItem { Number = 10 }, new LabelledWorkItem { Number = 11 }]);

        var messages = new SynchronousProgressMessageList();
        var progress = messages.CreateProgress();

        _ = await CreateSut().RemapLabelAsync("owner", "repo", "story", "type/story", progress, cancellationToken);

        Assert.Contains(messages, message => message.Contains("finding labelled items", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, message => message.Contains("retagging item 1 of 2", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, message => message.Contains("retagging item 2 of 2", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RemapLabelAsync_ItemsHaveSource_SetsLabelsInSingleRequestAndDeletesSource()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ArrangeLabels("story", "type/story");
        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo", "story", cancellationToken)
            .Returns([
                CreateLabelledWorkItem(10, "story"),
                CreateLabelledWorkItem(11, "story", "priority/high"),
            ]);

        var result = await CreateSut().RemapLabelAsync("owner", "repo", "story", "type/story", cancellationToken: cancellationToken);

        Assert.Equal(2, result.SucceededItemCount);
        Assert.Equal(0, result.FailedItemCount);
        Assert.True(result.SourceDeleted);
        Assert.False(result.DestinationCreated);
        await _gitHubService.Received(1).SetLabelsOnTriageItemAsync(
            "owner",
            "repo",
            10,
            Arg.Is<IReadOnlyList<string>>(labels => labels.SequenceEqual(new[] { "type/story" })),
            cancellationToken);
        await _gitHubService.Received(1).SetLabelsOnTriageItemAsync(
            "owner",
            "repo",
            11,
            Arg.Is<IReadOnlyList<string>>(labels => labels.SequenceEqual(new[] { "priority/high", "type/story" })),
            cancellationToken);
        await _gitHubService.DidNotReceive().AddLabelsToTriageItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        await _gitHubService.DidNotReceive().RemoveLabelFromTriageItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo", "story", cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), Arg.Any<CancellationToken>());
        await _labelRepository.DidNotReceive().CreateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemapLabelAsync_WhenCurrentLabelsUnknown_FallsBackToAddAndRemove()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ArrangeLabels("story", "type/story");
        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo", "story", cancellationToken)
            .Returns([new LabelledWorkItem { Number = 10 }]);

        var result = await CreateSut().RemapLabelAsync("owner", "repo", "story", "type/story", cancellationToken: cancellationToken);

        Assert.Equal(1, result.SucceededItemCount);
        Assert.True(result.SourceDeleted);
        await _gitHubService.Received(1).AddLabelsToTriageItemAsync("owner", "repo", 10, Arg.Is<IReadOnlyList<string>>(labels => labels.Count == 1 && labels[0] == "type/story"), cancellationToken);
        await _gitHubService.Received(1).RemoveLabelFromTriageItemAsync("owner", "repo", 10, "story", cancellationToken);
        await _gitHubService.DidNotReceive().SetLabelsOnTriageItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemapLabelAsync_DestinationMissing_CreatesDestinationBeforeRetagging()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _labelRepository
            .GetLabelsAsync("owner", "repo", cancellationToken, true)
            .Returns([new Label { Name = "story", Colour = "1d76db", Description = "User story", RepositoryName = "repo" }]);
        _labelRepository
            .CreateLabelAsync("owner", "repo", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Label>(2) with { RepositoryName = "repo" });
        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo", "story", cancellationToken)
            .Returns([CreateLabelledWorkItem(7, "story")]);

        var result = await CreateSut().RemapLabelAsync("owner", "repo", "story", "type/story", cancellationToken: cancellationToken);

        Assert.True(result.DestinationCreated);
        Assert.Equal("type/story", result.DestinationLabelName);
        Assert.True(result.SourceDeleted);
        await _labelRepository.Received(1).CreateLabelAsync(
            "owner",
            "repo",
            Arg.Is<Label>(label => label.Name == "type/story" && label.Colour == "1d76db" && label.Description == "User story"),
            cancellationToken);
        await _gitHubService.Received(1).SetLabelsOnTriageItemAsync(
            "owner",
            "repo",
            7,
            Arg.Is<IReadOnlyList<string>>(labels => labels.SequenceEqual(new[] { "type/story" })),
            cancellationToken);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo", "story", cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemapLabelAsync_OneItemFails_ContinuesAndDoesNotDeleteSource()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ArrangeLabels("story", "type/story");
        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo", "story", cancellationToken)
            .Returns([
                CreateLabelledWorkItem(10, "story"),
                CreateLabelledWorkItem(11, "story"),
                CreateLabelledWorkItem(12, "story"),
            ]);
        _gitHubService
            .When(service => service.SetLabelsOnTriageItemAsync("owner", "repo", 11, Arg.Any<IReadOnlyList<string>>(), cancellationToken))
            .Throw(new HttpRequestException("GitHub API failure"));

        var result = await CreateSut().RemapLabelAsync("owner", "repo", "story", "type/story", cancellationToken: cancellationToken);

        Assert.Equal(2, result.SucceededItemCount);
        Assert.Equal(1, result.FailedItemCount);
        Assert.False(result.SourceDeleted);
        Assert.True(result.HasErrors);
        Assert.Equal(11, Assert.Single(result.Errors).ItemNumber);
        await _gitHubService.Received(1).SetLabelsOnTriageItemAsync("owner", "repo", 12, Arg.Any<IReadOnlyList<string>>(), cancellationToken);
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _labelRepository.DidNotReceive().UpdateLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Label>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemapLabelAsync_NoWorkItems_DeletesSource()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ArrangeLabels("story", "type/story");
        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo", "story", cancellationToken)
            .Returns([]);

        var result = await CreateSut().RemapLabelAsync("owner", "repo", "story", "type/story", cancellationToken: cancellationToken);

        Assert.Equal(0, result.SucceededItemCount);
        Assert.Equal(0, result.FailedItemCount);
        Assert.True(result.SourceDeleted);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo", "story", cancellationToken);
        await _gitHubService.DidNotReceive().AddLabelsToTriageItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemapLabelAsync_SameSourceAndDestination_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var action = async () => await CreateSut().RemapLabelAsync("owner", "repo", "story", "Story", cancellationToken: cancellationToken);

        _ = await Assert.ThrowsAsync<ArgumentException>(action);
        await _labelRepository.DidNotReceive().GetLabelsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task RemapLabelAsync_SourceMissing_ThrowsKeyNotFoundException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        _labelRepository
            .GetLabelsAsync("owner", "repo", cancellationToken, true)
            .Returns([new Label { Name = "type/story", Colour = "1d76db", Description = "Story", RepositoryName = "repo" }]);

        var action = async () => await CreateSut().RemapLabelAsync("owner", "repo", "story", "type/story", cancellationToken: cancellationToken);

        _ = await Assert.ThrowsAsync<KeyNotFoundException>(action);
        await _labelRepository.DidNotReceive().DeleteLabelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyWithRemapAsync_WhenRemapAndUnusedDeleteRequested_EnrichesDeletedCounts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var previews = new[]
        {
            new RecommendedTaxonomyRepositoryPreviewDto(
                "owner/repo-a",
                [new LabelDto("type/bug", "d73a4a", "Bug", "owner/repo-a")],
                [],
                [new LabelDto("legacy", "ededed", "Legacy", "owner/repo-a")],
                [new LabelDto("bug", "d73a4a", "Bug", "owner/repo-a")],
                [],
                []),
        };

        var remapActions = new Dictionary<string, IReadOnlyList<RecommendedTaxonomyRemapActionDto>>(StringComparer.OrdinalIgnoreCase)
        {
            ["owner/repo-a"] = [new RecommendedTaxonomyRemapActionDto("bug", "type/bug", false)],
        };

        var existingLabels = RecommendedLabelTaxonomyCatalog.SoloDevBoard
            .Where(label => !label.Name.Equals("type/bug", StringComparison.OrdinalIgnoreCase))
            .Select(label => new Label
            {
                Name = label.Name,
                Colour = label.Colour,
                Description = label.Description,
                RepositoryName = "repo-a",
            })
            .Concat([new Label { Name = "bug", Colour = "d73a4a", Description = "Bug", RepositoryName = "repo-a" }])
            .ToArray();

        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken)
            .Returns(existingLabels);

        _labelRepository
            .CreateLabelAsync("owner", "repo-a", Arg.Any<Label>(), cancellationToken)
            .Returns(callInfo => callInfo.ArgAt<Label>(2) with { RepositoryName = "repo-a" });

        _labelRepository
            .GetLabelsAsync("owner", "repo-a", cancellationToken, true)
            .Returns([
                new Label { Name = "bug", Colour = "d73a4a", Description = "Bug", RepositoryName = "repo-a" },
                new Label { Name = "type/bug", Colour = "d73a4a", Description = "Bug", RepositoryName = "repo-a" },
            ]);

        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo-a", "bug", cancellationToken)
            .Returns([CreateLabelledWorkItem(7, "bug")]);

        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", Arg.Any<string>(), cancellationToken)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.ApplyRecommendedTaxonomyWithRemapAsync(
            RecommendedLabelTaxonomyCatalog.SoloDevBoardStrategyId,
            ["owner/repo-a"],
            previews,
            remapActions,
            keepAreaLabels: true,
            cancellationToken: cancellationToken);

        var summary = Assert.Single(result.ApplyResults);
        Assert.Equal(1, summary.CreatedCount);
        Assert.Equal(2, summary.DeletedCount);
        Assert.False(summary.HasError);
        Assert.True(Assert.Single(result.RemapResults).SourceDeleted);
        await _labelRepository.Received(1).DeleteLabelAsync("owner", "repo-a", "legacy", cancellationToken);
    }

    [Fact]
    public async Task ApplyRecommendedTaxonomyWithRemapAsync_WhenUnusedDeleteFails_RecordsDeleteError()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var previews = new[]
        {
            new RecommendedTaxonomyRepositoryPreviewDto(
                "owner/repo-a",
                [],
                [],
                [new LabelDto("legacy", "ededed", "Legacy", "owner/repo-a")],
                [],
                [],
                []),
        };

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
                new Label { Name = "legacy", Colour = "ededed", Description = "Legacy", RepositoryName = "repo-a" },
            ]);

        _labelRepository
            .DeleteLabelAsync("owner", "repo-a", "legacy", cancellationToken)
            .Returns(Task.FromException(new HttpRequestException("Label still referenced")));

        var sut = CreateSut();

        var result = await sut.ApplyRecommendedTaxonomyWithRemapAsync(
            "github-default",
            ["owner/repo-a"],
            previews,
            new Dictionary<string, IReadOnlyList<RecommendedTaxonomyRemapActionDto>>(StringComparer.OrdinalIgnoreCase),
            keepAreaLabels: true,
            cancellationToken: cancellationToken);

        var summary = Assert.Single(result.ApplyResults);
        Assert.True(summary.HasError);
        Assert.Equal(0, summary.DeletedCount);
        var deleteError = Assert.Single(summary.DeleteErrors);
        Assert.Equal("legacy", deleteError.LabelName);
        Assert.Equal("Label still referenced", deleteError.ErrorMessage);
    }

    [Fact]
    public async Task RemapLabelAsync_WhenItemHasOtherLabels_AddsDestinationAndRemovesSourceOnly()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ArrangeLabels("story", "type/story");
        _labelRepository
            .GetWorkItemsWithLabelAsync("owner", "repo", "story", cancellationToken)
            .Returns([new LabelledWorkItem { Number = 42 }]);

        await CreateSut().RemapLabelAsync("owner", "repo", "story", "type/story", cancellationToken: cancellationToken);

        await _gitHubService.Received(1).AddLabelsToTriageItemAsync(
            "owner",
            "repo",
            42,
            Arg.Is<IReadOnlyList<string>>(labels => labels.Count == 1 && labels[0] == "type/story"),
            cancellationToken);
        await _gitHubService.Received(1).RemoveLabelFromTriageItemAsync("owner", "repo", 42, "story", cancellationToken);
        await _labelRepository.DidNotReceive().UpdateLabelAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Label>(),
            Arg.Any<CancellationToken>());
    }

    private LabelService CreateSut() => new(_labelRepository, _gitHubService);

    private static LabelledWorkItem CreateLabelledWorkItem(int number, params string[] labelNames)
        => new() { Number = number, LabelNames = labelNames };

    private sealed class SynchronousProgressMessageList : List<string>, IProgress<string>
    {
        public void Report(string value) => Add(value);

        public IProgress<string> CreateProgress() => this;
    }

    private void ArrangeLabels(string sourceName, string destinationName)
    {
        _labelRepository
            .GetLabelsAsync("owner", "repo", Arg.Any<CancellationToken>(), true)
            .Returns(
            [
                new Label { Name = sourceName, Colour = "ededed", Description = "Source", RepositoryName = "repo" },
                new Label { Name = destinationName, Colour = "1d76db", Description = "Destination", RepositoryName = "repo" },
            ]);
    }
}
