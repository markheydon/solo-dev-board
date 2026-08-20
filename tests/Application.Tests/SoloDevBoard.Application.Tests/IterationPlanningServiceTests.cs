using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="IterationPlanningService"/>.</summary>
public sealed class IterationPlanningServiceTests
{
    private readonly IPmWorkItemCatalogueService _workItemCatalogueService = Substitute.For<IPmWorkItemCatalogueService>();
    private readonly IProjectItemCatalogueService _projectItemCatalogueService = Substitute.For<IProjectItemCatalogueService>();
    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();

    [Fact]
    public async Task AddToUpNextAsync_ItemNotOnBoard_AddsThenSetsStatusAndFocusOrder()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);
        _gitHubService
            .AddTriageItemToProjectBoardAsync("owner", "repo", 50, "project-id", cancellationToken)
            .Returns("PVTI_new");

        var sut = CreateSut();
        var labels = new[] { PlanningFocusOrderSequencer.StoryTypeLabel, "priority/medium" };

        var result = await sut.AddToUpNextAsync(
            "project-id",
            PmWorkItemTypeDto.Issue,
            "owner/repo",
            50,
            labels,
            cancellationToken);

        Assert.True(result.AddedBoardCard);
        Assert.Equal(3, result.FocusOrderAssigned);
        Assert.False(result.FocusOrderSkipped);

        await _gitHubService.Received(1)
            .AddTriageItemToProjectBoardAsync("owner", "repo", 50, "project-id", cancellationToken);
        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_new",
                "PVTF_status",
                "opt-up-next",
                cancellationToken);
        await _projectItemCatalogueService.Received(2)
            .GetCatalogueAsync("project-id", cancellationToken);
        await _projectItemCatalogueService.Received(1)
            .UpdateFocusOrderAsync("project-id", "PVTI_new", "PVTF_focus", 3, cancellationToken);
        _projectItemCatalogueService.Received(1).InvalidateCatalogue("project-id");
    }

    [Fact]
    public async Task AddToUpNextAsync_ItemAlreadyOnBoard_UpdatesStatusWithoutAdd()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var existingItem = CreateBoardItem("PVTI_existing", "Todo", focusOrder: null, number: 51);
        var catalogue = CreateCatalogue(existingItem);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = CreateSut();
        var labels = new[] { PlanningFocusOrderSequencer.EnablerTypeLabel };

        var result = await sut.AddToUpNextAsync(
            "project-id",
            PmWorkItemTypeDto.Issue,
            "owner/repo",
            51,
            labels,
            cancellationToken);

        Assert.False(result.AddedBoardCard);
        Assert.Equal(3, result.FocusOrderAssigned);
        Assert.False(result.FocusOrderSkipped);

        await _gitHubService.DidNotReceive()
            .AddTriageItemToProjectBoardAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_existing",
                "PVTF_status",
                "opt-up-next",
                cancellationToken);
        await _projectItemCatalogueService.Received(2)
            .GetCatalogueAsync("project-id", cancellationToken);
        await _projectItemCatalogueService.Received(1)
            .UpdateFocusOrderAsync("project-id", "PVTI_existing", "PVTF_focus", 3, cancellationToken);
        _projectItemCatalogueService.Received(1).InvalidateCatalogue("project-id");
    }

    [Fact]
    public async Task AddToUpNextAsync_FeatureLabel_SkipsFocusOrder()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);
        _gitHubService
            .AddTriageItemToProjectBoardAsync("owner", "repo", 52, "project-id", cancellationToken)
            .Returns("PVTI_feature");

        var sut = CreateSut();
        var labels = new[] { PmLabelHelpers.FeatureTypeLabel };

        var result = await sut.AddToUpNextAsync(
            "project-id",
            PmWorkItemTypeDto.Issue,
            "owner/repo",
            52,
            labels,
            cancellationToken);

        Assert.True(result.AddedBoardCard);
        Assert.Null(result.FocusOrderAssigned);
        Assert.True(result.FocusOrderSkipped);

        await _projectItemCatalogueService.DidNotReceive()
            .UpdateFocusOrderAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>());
        _projectItemCatalogueService.Received(1).InvalidateCatalogue("project-id");
    }

    [Fact]
    public async Task AddToUpNextAsync_StoryLabelWithoutFocusOrderField_SetsStatusAndSkipsFocusOrder()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null, includeFocusOrderField: false);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);
        _gitHubService
            .AddTriageItemToProjectBoardAsync("owner", "repo", 54, "project-id", cancellationToken)
            .Returns("PVTI_story");

        var sut = CreateSut();
        var labels = new[] { PlanningFocusOrderSequencer.StoryTypeLabel, "priority/medium" };

        var result = await sut.AddToUpNextAsync(
            "project-id",
            PmWorkItemTypeDto.Issue,
            "owner/repo",
            54,
            labels,
            cancellationToken);

        Assert.True(result.AddedBoardCard);
        Assert.Null(result.FocusOrderAssigned);
        Assert.True(result.FocusOrderSkipped);

        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_story",
                "PVTF_status",
                "opt-up-next",
                cancellationToken);
        await _projectItemCatalogueService.Received(1)
            .GetCatalogueAsync("project-id", cancellationToken);
        await _projectItemCatalogueService.DidNotReceive()
            .UpdateFocusOrderAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>());
        _projectItemCatalogueService.Received(1).InvalidateCatalogue("project-id");
    }

    [Fact]
    public async Task AddToUpNextAsync_MissingUpNextStatus_ThrowsInvalidOperationException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null, includeUpNextOption: false);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = CreateSut();

        var action = async () => await sut.AddToUpNextAsync(
            "project-id",
            PmWorkItemTypeDto.Issue,
            "owner/repo",
            53,
            ["type/story"],
            cancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains("Up Next", exception.Message, StringComparison.Ordinal);
    }

    private IterationPlanningService CreateSut() =>
        new(_workItemCatalogueService, _projectItemCatalogueService, _gitHubService);

    private static ProjectBoardItemCatalogueDto CreateCatalogue(
        ProjectBoardItemDto? existingItem,
        bool includeUpNextOption = true,
        bool includeFocusOrderField = true)
    {
        var statusOptions = new List<ProjectBoardStatusOptionDto>
        {
            new("opt-todo", "Todo"),
        };

        if (includeUpNextOption)
        {
            statusOptions.Add(new ProjectBoardStatusOptionDto("opt-up-next", "Up Next"));
        }

        var items = new List<ProjectBoardItemDto>
        {
            CreateBoardItem("PVTI_one", "Up Next", 1, 40),
            CreateBoardItem("PVTI_two", "Up Next", 2, 41),
        };

        if (existingItem is not null)
        {
            items.Add(existingItem);
        }

        return new ProjectBoardItemCatalogueDto(
            new ProjectBoardFieldIdsDto(
                "PVTF_status",
                includeFocusOrderField ? "PVTF_focus" : string.Empty),
            statusOptions,
            items);
    }

    private static ProjectBoardItemDto CreateBoardItem(
        string projectItemId,
        string statusName,
        double? focusOrder,
        int number) =>
        new(
            projectItemId,
            new ProjectBoardItemStatusDto($"opt-{statusName.Replace(' ', '-').ToLowerInvariant()}", statusName),
            focusOrder,
            new ProjectBoardItemContentDto(
                ProjectBoardItemContentTypeDto.Issue,
                number,
                "owner",
                "repo",
                $"Title {number}",
                $"https://github.com/owner/repo/issues/{number}"),
            DateTimeOffset.UtcNow,
            UsedItemUpdatedAtFallback: false);
}
