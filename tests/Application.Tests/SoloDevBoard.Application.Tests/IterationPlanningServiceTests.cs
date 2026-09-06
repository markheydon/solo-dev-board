using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Domain.Entities.Milestones;
using IMilestoneRepository = SoloDevBoard.Application.Services.Migration.IMilestoneRepository;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="IterationPlanningService"/>.</summary>
public sealed class IterationPlanningServiceTests
{
    private readonly IPlanningWorkItemCatalogueService _workItemCatalogueService = Substitute.For<IPlanningWorkItemCatalogueService>();
    private readonly IProjectItemCatalogueService _projectItemCatalogueService = Substitute.For<IProjectItemCatalogueService>();
    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();
    private readonly IMilestoneRepository _milestoneRepository = Substitute.For<IMilestoneRepository>();

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
            PlanningWorkItemTypeDto.Issue,
            "owner/repo",
            50,
            labels,
            3,
            cancellationToken);

        Assert.True(result.AddedBoardCard);
        Assert.Equal("PVTI_new", result.ProjectItemId);
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
            PlanningWorkItemTypeDto.Issue,
            "owner/repo",
            51,
            labels,
            3,
            cancellationToken);

        Assert.False(result.AddedBoardCard);
        Assert.Equal("PVTI_existing", result.ProjectItemId);
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
        var labels = new[] { PlanningLabelHelpers.FeatureTypeLabel };

        var result = await sut.AddToUpNextAsync(
            "project-id",
            PlanningWorkItemTypeDto.Issue,
            "owner/repo",
            52,
            labels,
            3,
            cancellationToken);

        Assert.True(result.AddedBoardCard);
        Assert.Equal("PVTI_feature", result.ProjectItemId);
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
            PlanningWorkItemTypeDto.Issue,
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
            PlanningWorkItemTypeDto.Issue,
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
            PlanningWorkItemTypeDto.Issue,
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
            PlanningWorkItemTypeDto.Issue,
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
            .RemoveLabelFromTriageItemAsync(
                "owner",
                "repo",
                40,
                PlanningLabelHelpers.IceBoxStatusLabel,
                cancellationToken);
        await _gitHubService.Received(1)
            .AddLabelsToTriageItemAsync(
                "owner",
                "repo",
                40,
                Arg.Is<IReadOnlyList<string>>(labels =>
                    labels.Count == 1
                    && labels.Contains(PlanningLabelHelpers.BlockedStatusLabel)),
                cancellationToken);
    }

    [Fact]
    public async Task MarkStalledUpNextItemBlockedAsync_WhenCatalogueLabelsMissing_UsesAdditiveLabelChange()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = CreateSut();
        var item = new IterationPlanningStalledItemDto(
            "PVTI_one",
            PlanningWorkItemTypeDto.Issue,
            40,
            "Blocked story",
            "https://github.com/owner/repo/issues/40",
            "owner/repo",
            4,
            false,
            []);

        await sut.MarkStalledUpNextItemBlockedAsync("project-id", item, cancellationToken);

        await _gitHubService.Received(1)
            .RemoveLabelFromTriageItemAsync(
                "owner",
                "repo",
                40,
                PlanningLabelHelpers.IceBoxStatusLabel,
                cancellationToken);
        await _gitHubService.Received(1)
            .AddLabelsToTriageItemAsync(
                "owner",
                "repo",
                40,
                Arg.Is<IReadOnlyList<string>>(labels =>
                    labels.Count == 1
                    && labels.Contains(PlanningLabelHelpers.BlockedStatusLabel)),
                cancellationToken);
    }

    [Fact]
    public async Task MoveStalledUpNextItemToIceBoxAsync_WhenCatalogueLabelsMissing_UsesAdditiveLabelChange()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var catalogue = CreateCatalogue(existingItem: null);
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(catalogue);

        var sut = CreateSut();
        var item = new IterationPlanningStalledItemDto(
            "PVTI_one",
            PlanningWorkItemTypeDto.Issue,
            40,
            "Ice box story",
            "https://github.com/owner/repo/issues/40",
            "owner/repo",
            4,
            false,
            []);

        await sut.MoveStalledUpNextItemToIceBoxAsync("project-id", item, cancellationToken);

        await _gitHubService.Received(1)
            .RemoveLabelFromTriageItemAsync(
                "owner",
                "repo",
                40,
                PlanningLabelHelpers.BlockedStatusLabel,
                cancellationToken);
        await _gitHubService.Received(1)
            .AddLabelsToTriageItemAsync(
                "owner",
                "repo",
                40,
                Arg.Is<IReadOnlyList<string>>(labels =>
                    labels.Count == 1
                    && labels.Contains(PlanningLabelHelpers.IceBoxStatusLabel)),
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
            PlanningWorkItemTypeDto.Issue,
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
            .RemoveLabelFromTriageItemAsync(
                "owner",
                "repo",
                40,
                PlanningLabelHelpers.BlockedStatusLabel,
                cancellationToken);
        await _gitHubService.Received(1)
            .AddLabelsToTriageItemAsync(
                "owner",
                "repo",
                40,
                Arg.Is<IReadOnlyList<string>>(labels =>
                    labels.Count == 1
                    && labels.Contains(PlanningLabelHelpers.IceBoxStatusLabel)),
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
            PlanningWorkItemTypeDto.Issue,
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

    [Fact]
    public async Task ApplyBulkMilestoneAsync_ValidSelection_AssignsMilestoneToEachItem()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var selectedItems = new[]
        {
            CreateUpNextItem("owner/repo-a", 10),
            CreateUpNextItem("owner/repo-a", 11),
        };
        _milestoneRepository
            .GetMilestonesAsync("owner", "repo-a", cancellationToken)
            .Returns([CreateMilestone("v1.0.0", 5)]);

        var sut = CreateSut();

        var result = await sut.ApplyBulkMilestoneAsync(selectedItems, "v1.0.0", cancellationToken);

        Assert.Equal(2, result.AppliedCount);
        Assert.Empty(result.SkippedRepositories);
        Assert.Empty(result.Failures);
        await _gitHubService.Received(1)
            .AssignMilestoneToTriageItemAsync("owner", "repo-a", 10, 5, cancellationToken);
        await _gitHubService.Received(1)
            .AssignMilestoneToTriageItemAsync("owner", "repo-a", 11, 5, cancellationToken);
    }

    [Fact]
    public async Task ApplyBulkMilestoneAsync_WhenSecondAssignmentFails_ReturnsPartialResult()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var selectedItems = new[]
        {
            CreateUpNextItem("owner/repo-a", 10),
            CreateUpNextItem("owner/repo-a", 11),
        };
        _milestoneRepository
            .GetMilestonesAsync("owner", "repo-a", cancellationToken)
            .Returns([CreateMilestone("v1.0.0", 5)]);
        _gitHubService
            .AssignMilestoneToTriageItemAsync("owner", "repo-a", 10, 5, cancellationToken)
            .Returns(Task.CompletedTask);
        _gitHubService
            .AssignMilestoneToTriageItemAsync("owner", "repo-a", 11, 5, cancellationToken)
            .Returns(_ => throw new HttpRequestException("GitHub unavailable"));

        var sut = CreateSut();

        var result = await sut.ApplyBulkMilestoneAsync(selectedItems, "v1.0.0", cancellationToken);

        Assert.Equal(1, result.AppliedCount);
        Assert.Empty(result.SkippedRepositories);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("owner/repo-a", failure.RepositoryFullName);
        Assert.Equal(11, failure.Number);
        Assert.Contains("GitHub unavailable", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPlanningViewAsync_ForceReload_InvalidatesProjectBoardCatalogue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var boardCatalogue = CreateCatalogue(existingItem: null);
        _workItemCatalogueService
            .GetCatalogueAsync(cancellationToken)
            .Returns(new PlanningWorkItemCatalogueResultDto([], [], []));
        _projectItemCatalogueService
            .GetCatalogueAsync("project-id", cancellationToken)
            .Returns(boardCatalogue);

        var sut = CreateSut();

        await sut.GetPlanningViewAsync("project-id", 8, 3, forceReload: true, cancellationToken);

        _projectItemCatalogueService.Received(1).InvalidateCatalogue("project-id");
    }

    private IterationPlanningService CreateSut() =>
        new(_workItemCatalogueService, _projectItemCatalogueService, _gitHubService, _milestoneRepository, TimeProvider.System);

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

    private static IterationPlanningUpNextItemDto CreateUpNextItem(string repositoryFullName, int number) =>
        new(
            $"PVTI_{number}",
            PlanningWorkItemTypeDto.Issue,
            number,
            $"Title {number}",
            $"https://github.com/{repositoryFullName}/issues/{number}",
            repositoryFullName,
            1,
            ["type/story"]);

    private static Milestone CreateMilestone(string title, int number) =>
        new()
        {
            Title = title,
            Number = number,
            State = "open",
        };
}
