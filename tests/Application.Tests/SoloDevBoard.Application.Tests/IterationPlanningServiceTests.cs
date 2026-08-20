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
            3,
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
            3,
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
            3,
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
            3,
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
            3,
            cancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains("Up Next", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddToUpNextAsync_WhenStalledUpNextItemsRemain_ThrowsInvalidOperationException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var stalledItem = CreateBoardItem(
            "PVTI_stalled",
            "Up Next",
            focusOrder: 1,
            number: 99,
            activityTimestamp: DateTimeOffset.UtcNow.AddDays(-5));
        var catalogue = CreateCatalogue(stalledItem);
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
            3,
            cancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains("Resolve stalled Up Next items", exception.Message, StringComparison.Ordinal);
        await _gitHubService.DidNotReceive()
            .AddTriageItemToProjectBoardAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReCommitStalledUpNextItemAsync_ValidItem_MovesTodoThenUpNext()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = CreateSut();

        await sut.ReCommitStalledUpNextItemAsync("project-id", "PVTI_one", cancellationToken);

        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_one",
                "PVTF_status",
                "opt-todo",
                cancellationToken);
        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_one",
                "PVTF_status",
                "opt-up-next",
                cancellationToken);
        _projectItemCatalogueService.Received(1).InvalidateCatalogue("project-id");
    }

    [Fact]
    public async Task ReCommitStalledUpNextItemAsync_WhenUpNextUpdateFails_RestoresUpNextAndThrows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);
        _gitHubService
            .UpdateProjectBoardItemStatusAsync(
                Arg.Any<string>(),
                "PVTI_one",
                "PVTF_status",
                "opt-up-next",
                cancellationToken)
            .Returns(
                _ => throw new HttpRequestException("GitHub unavailable"),
                _ => Task.CompletedTask);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReCommitStalledUpNextItemAsync("project-id", "PVTI_one", cancellationToken));

        Assert.Contains("restored to Up Next", exception.Message, StringComparison.Ordinal);
        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_one",
                "PVTF_status",
                "opt-todo",
                cancellationToken);
        await _gitHubService.Received(2)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_one",
                "PVTF_status",
                "opt-up-next",
                cancellationToken);
    }

    [Fact]
    public async Task MarkStalledUpNextItemBlockedAsync_ValidItem_UpdatesStatusAndLabels()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = CreateSut();
        var item = new IterationPlanningStalledItemDto(
            "PVTI_one",
            PmWorkItemTypeDto.Issue,
            40,
            "Blocked story",
            "https://github.com/owner/repo/issues/40",
            "owner/repo",
            4,
            false,
            ["type/story", "priority/medium"]);

        await sut.MarkStalledUpNextItemBlockedAsync("project-id", item, cancellationToken);

        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_one",
                "PVTF_status",
                "opt-blocked",
                cancellationToken);
        await _gitHubService.Received(1)
            .ApplyLabelsToTriageItemAsync(
                "owner",
                "repo",
                40,
                Arg.Is<IReadOnlyList<string>>(labels =>
                    labels.Contains("type/story")
                    && labels.Contains("priority/medium")
                    && labels.Contains(PmLabelHelpers.BlockedStatusLabel)),
                cancellationToken);
    }

    [Fact]
    public async Task MoveStalledUpNextItemToIceBoxAsync_ValidItem_UpdatesStatusClearsFocusOrderAndLabels()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = CreateSut();
        var item = new IterationPlanningStalledItemDto(
            "PVTI_one",
            PmWorkItemTypeDto.Issue,
            40,
            "Ice box story",
            "https://github.com/owner/repo/issues/40",
            "owner/repo",
            4,
            false,
            ["type/story", "priority/medium"]);

        await sut.MoveStalledUpNextItemToIceBoxAsync("project-id", item, cancellationToken);

        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_one",
                "PVTF_status",
                "opt-ice-box",
                cancellationToken);
        await _projectItemCatalogueService.Received(1)
            .ClearFocusOrderAsync("project-id", "PVTI_one", "PVTF_focus", cancellationToken);
        await _gitHubService.Received(1)
            .ApplyLabelsToTriageItemAsync(
                "owner",
                "repo",
                40,
                Arg.Is<IReadOnlyList<string>>(labels =>
                    labels.Contains("type/story")
                    && labels.Contains("priority/medium")
                    && labels.Contains(PmLabelHelpers.IceBoxStatusLabel)),
                cancellationToken);
    }

    [Fact]
    public async Task RemoveStalledUpNextItemAsync_ValidItem_MovesToTodoAndClearsFocusOrder()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = CreateSut();
        var item = new IterationPlanningStalledItemDto(
            "PVTI_one",
            PmWorkItemTypeDto.Issue,
            40,
            "Remove me",
            "https://github.com/owner/repo/issues/40",
            "owner/repo",
            5,
            false,
            ["type/story"]);

        await sut.RemoveStalledUpNextItemAsync("project-id", item, cancellationToken);

        await _gitHubService.Received(1)
            .UpdateProjectBoardItemStatusAsync(
                "project-id",
                "PVTI_one",
                "PVTF_status",
                "opt-todo",
                cancellationToken);
        await _projectItemCatalogueService.Received(1)
            .ClearFocusOrderAsync("project-id", "PVTI_one", "PVTF_focus", cancellationToken);
    }

    private IterationPlanningService CreateSut() =>
        new(_workItemCatalogueService, _projectItemCatalogueService, _gitHubService, TimeProvider.System);

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
            statusOptions.Add(new ProjectBoardStatusOptionDto("opt-blocked", "Blocked"));
            statusOptions.Add(new ProjectBoardStatusOptionDto("opt-ice-box", "Ice Box"));
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
        int number,
        DateTimeOffset? activityTimestamp = null) =>
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
            activityTimestamp ?? DateTimeOffset.UtcNow,
            UsedItemUpdatedAtFallback: false);
}
