using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.PmWorkflow;
using SoloDevBoard.Domain.Entities.PmWorkflow;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="ProjectItemCatalogueService"/>.</summary>
public sealed class ProjectItemCatalogueServiceTests
{
    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();

    [Fact]
    public void Constructor_GitHubServiceIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        IGitHubService? gitHubService = null;

        // Act
        var action = () => _ = new ProjectItemCatalogueService(gitHubService!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task GetCatalogueAsync_CatalogueReturned_MapsDomainRecordsToDto()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var activityTimestamp = new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);
        var catalogue = new ProjectBoardItemCatalogue
        {
            FieldIds = new ProjectBoardFieldIds
            {
                StatusFieldId = "PVTF_status",
                FocusOrderFieldId = "PVTF_focus",
            },
            StatusOptions = [
                new ProjectBoardStatusOption { OptionId = "option-up-next", Name = "Up Next" },
                new ProjectBoardStatusOption { OptionId = "option-todo", Name = "Todo" },
            ],
            Items = [
                new ProjectBoardItem
                {
                    ProjectItemId = "PVTI_item",
                    Status = new ProjectBoardItemStatus { OptionId = "option-up-next", Name = "Up Next" },
                    FocusOrder = 2,
                    Content = new ProjectBoardItemContent
                    {
                        ContentType = TriageItemType.Issue,
                        Number = 40,
                        RepositoryOwner = "markheydon",
                        RepositoryName = "solo-dev-board",
                        Title = "Daily Focus",
                        Url = "https://github.com/markheydon/solo-dev-board/issues/40",
                    },
                    ActivityTimestamp = activityTimestamp,
                },
            ],
        };

        _gitHubService
            .GetProjectBoardItemsAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = new ProjectItemCatalogueService(_gitHubService);

        // Act
        var result = await sut.GetCatalogueAsync("project-id", cancellationToken);

        // Assert
        Assert.Equal("PVTF_status", result.FieldIds.StatusFieldId);
        Assert.Equal("PVTF_focus", result.FieldIds.FocusOrderFieldId);
        Assert.Single(result.Items);
        Assert.Equal("PVTI_item", result.Items[0].ProjectItemId);
        Assert.Equal("Up Next", result.Items[0].Status?.Name);
        Assert.Equal(2, result.StatusOptions.Count);
        Assert.Equal("option-up-next", result.StatusOptions[0].OptionId);
        Assert.Equal("Todo", result.StatusOptions[1].Name);
        Assert.Equal(2, result.Items[0].FocusOrder);
        Assert.Equal(ProjectBoardItemContentTypeDto.Issue, result.Items[0].Content.ContentType);
        Assert.Equal(40, result.Items[0].Content.Number);
        Assert.Equal(activityTimestamp, result.Items[0].ActivityTimestamp);
        await _gitHubService.Received(1).GetProjectBoardItemsAsync("project-id", cancellationToken);
    }

    [Fact]
    public async Task GetCatalogueAsync_MissingFocusOrderFieldId_MapsNullFocusOrderFieldId()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var catalogue = new ProjectBoardItemCatalogue
        {
            FieldIds = new ProjectBoardFieldIds
            {
                StatusFieldId = "PVTF_status",
                FocusOrderFieldId = null,
            },
            Items = [],
        };

        _gitHubService
            .GetProjectBoardItemsAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = new ProjectItemCatalogueService(_gitHubService);

        // Act
        var result = await sut.GetCatalogueAsync("project-id", cancellationToken);

        // Assert
        Assert.Null(result.FieldIds.FocusOrderFieldId);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task UpdateFocusOrderAsync_FocusOrderFieldIdMissing_ThrowsInvalidOperationException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new ProjectItemCatalogueService(_gitHubService);

        // Act
        var action = async () => await sut.UpdateFocusOrderAsync("project-id", "item-id", " ", 1, cancellationToken);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
        await _gitHubService.DidNotReceive().UpdateProjectBoardItemFocusOrderAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateFocusOrderAsync_ValidRequest_DelegatesToGitHubService()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new ProjectItemCatalogueService(_gitHubService);

        // Act
        await sut.UpdateFocusOrderAsync("project-id", "item-id", "focus-field-id", 3, cancellationToken);

        // Assert
        await _gitHubService.Received(1).UpdateProjectBoardItemFocusOrderAsync(
            "project-id",
            "item-id",
            "focus-field-id",
            3,
            cancellationToken);
    }

    [Fact]
    public async Task ClearFocusOrderAsync_FocusOrderFieldIdMissing_ThrowsInvalidOperationException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new ProjectItemCatalogueService(_gitHubService);

        // Act
        var action = async () => await sut.ClearFocusOrderAsync("project-id", "item-id", string.Empty, cancellationToken);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
        await _gitHubService.DidNotReceive().ClearProjectBoardItemFocusOrderAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearFocusOrderAsync_ValidRequest_DelegatesToGitHubService()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = new ProjectItemCatalogueService(_gitHubService);

        // Act
        await sut.ClearFocusOrderAsync("project-id", "item-id", "focus-field-id", cancellationToken);

        // Assert
        await _gitHubService.Received(1).ClearProjectBoardItemFocusOrderAsync(
            "project-id",
            "item-id",
            "focus-field-id",
            cancellationToken);
    }

    [Fact]
    public async Task GetCatalogueAsync_ConcurrentCallsForSameProject_ShareOneGitHubRequest()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogueReady = new TaskCompletionSource<ProjectBoardItemCatalogue>();
        _gitHubService
            .GetProjectBoardItemsAsync("project-id", Arg.Any<CancellationToken>())
            .Returns(_ => catalogueReady.Task);

        var sut = new ProjectItemCatalogueService(_gitHubService);

        var first = sut.GetCatalogueAsync("project-id", cancellationToken);
        var second = sut.GetCatalogueAsync("project-id", cancellationToken);

        catalogueReady.SetResult(new ProjectBoardItemCatalogue
        {
            FieldIds = new ProjectBoardFieldIds { StatusFieldId = "PVTF_status" },
            Items = [],
        });

        var results = await Task.WhenAll(first, second);

        Assert.Same(results[0], results[1]);
        await _gitHubService.Received(1).GetProjectBoardItemsAsync("project-id", Arg.Any<CancellationToken>());
    }
}
