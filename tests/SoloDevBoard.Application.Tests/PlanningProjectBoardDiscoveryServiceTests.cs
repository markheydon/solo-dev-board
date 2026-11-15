using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Domain.Entities.Repositories;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PlanningProjectBoardDiscoveryService"/>.</summary>
public sealed class PlanningProjectBoardDiscoveryServiceTests
{
    private readonly IGitHubService _gitHubService = Substitute.For<IGitHubService>();

    [Fact]
    public async Task GetPlanningBoardOptionsForRepositoriesAsync_SuppliedRepositories_ReturnsDistinctSortedBoards()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService
            .GetProjectBoardsForRepositoryAsync("owner", "repo-a", cancellationToken)
            .Returns(CreateDiscovery("PVT_shared", "Shared Board", total: 2, inaccessible: 1));

        _gitHubService
            .GetProjectBoardsForRepositoryAsync("owner", "repo-b", cancellationToken)
            .Returns(CreateDiscovery(
                "PVT_shared",
                "Shared Board",
                total: 1,
                inaccessible: 0,
                secondBoardId: "PVT_unique",
                secondBoardTitle: "Unique Board"));

        var repositories = new[]
        {
            CreateRepositoryDto("owner", "repo-a"),
            CreateRepositoryDto("owner", "repo-b"),
        };

        var sut = new PlanningProjectBoardDiscoveryService(_gitHubService);

        var result = await sut.GetPlanningBoardOptionsForRepositoriesAsync(repositories, cancellationToken);

        Assert.Equal(2, result.Options.Count);
        Assert.Equal("PVT_shared", result.Options[0].Id);
        Assert.Equal("Shared Board", result.Options[0].Title);
        Assert.Equal("PVT_unique", result.Options[1].Id);
        Assert.Equal(3, result.TotalLinkedProjectCount);
        Assert.Equal(1, result.InaccessibleLinkedProjectCount);
        await _gitHubService.DidNotReceive().GetActiveRepositoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPlanningBoardOptionsAsync_MultipleRepositories_ReturnsDistinctSortedBoards()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns([
            CreateRepository("owner", "repo-a"),
            CreateRepository("owner", "repo-b"),
        ]);

        _gitHubService
            .GetProjectBoardsForRepositoryAsync("owner", "repo-a", cancellationToken)
            .Returns(CreateDiscovery("PVT_shared", "Shared Board", total: 2, inaccessible: 1));

        _gitHubService
            .GetProjectBoardsForRepositoryAsync("owner", "repo-b", cancellationToken)
            .Returns(CreateDiscovery(
                "PVT_shared",
                "Shared Board",
                total: 1,
                inaccessible: 0,
                secondBoardId: "PVT_unique",
                secondBoardTitle: "Unique Board"));

        var sut = new PlanningProjectBoardDiscoveryService(_gitHubService);

        var result = await sut.GetPlanningBoardOptionsAsync(cancellationToken);

        Assert.Equal(2, result.Options.Count);
        Assert.Equal("PVT_shared", result.Options[0].Id);
        Assert.Equal("Shared Board", result.Options[0].Title);
        Assert.Equal("PVT_unique", result.Options[1].Id);
        Assert.Equal(3, result.TotalLinkedProjectCount);
        Assert.Equal(1, result.InaccessibleLinkedProjectCount);
    }

    [Fact]
    public async Task GetPlanningBoardOptionsAsync_NoActiveRepositories_ReturnsEmptyOptions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _gitHubService.GetActiveRepositoriesAsync(cancellationToken).Returns(Array.Empty<Repository>());

        var sut = new PlanningProjectBoardDiscoveryService(_gitHubService);

        var result = await sut.GetPlanningBoardOptionsAsync(cancellationToken);

        Assert.Empty(result.Options);
        Assert.Equal(0, result.TotalLinkedProjectCount);
        Assert.Equal(0, result.InaccessibleLinkedProjectCount);
        await _gitHubService.DidNotReceive().GetProjectBoardsForRepositoryAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static Repository CreateRepository(string owner, string name) => new()
    {
        Id = 1,
        Name = name,
        FullName = $"{owner}/{name}",
        Description = string.Empty,
        Url = $"https://github.com/{owner}/{name}",
        IsPrivate = false,
        IsArchived = false,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static RepositoryDto CreateRepositoryDto(string owner, string name) =>
        new(1, name, $"{owner}/{name}", string.Empty, $"https://github.com/{owner}/{name}", false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false);

    private static RepositoryProjectBoardDiscoveryResult CreateDiscovery(
        string firstBoardId,
        string firstBoardTitle,
        int total,
        int inaccessible,
        string? secondBoardId = null,
        string? secondBoardTitle = null)
    {
        var boards = new List<TriageProjectBoard>
        {
            new()
            {
                Id = firstBoardId,
                Title = firstBoardTitle,
                OwnerLogin = "owner",
                StatusFieldId = "status-field",
                StatusOptions = [],
            },
        };

        if (secondBoardId is not null && secondBoardTitle is not null)
        {
            boards.Add(new TriageProjectBoard
            {
                Id = secondBoardId,
                Title = secondBoardTitle,
                OwnerLogin = "owner",
                StatusFieldId = "status-field",
                StatusOptions = [],
            });
        }

        return new RepositoryProjectBoardDiscoveryResult(boards, total, inaccessible);
    }
}
