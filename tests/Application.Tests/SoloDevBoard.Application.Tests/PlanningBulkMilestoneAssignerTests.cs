using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Domain.Entities.Milestones;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PlanningBulkMilestoneAssigner"/>.</summary>
public sealed class PlanningBulkMilestoneAssignerTests
{
    [Fact]
    public void BuildSkipList_MissingMilestoneOnOneRepo_ReturnsThatRepository()
    {
        var selectedItems = new[]
        {
            CreateUpNextItem("owner/has-milestone", 10),
            CreateUpNextItem("owner/missing-milestone", 20),
        };

        var milestonesByRepository = new Dictionary<string, IReadOnlyList<Milestone>>(StringComparer.OrdinalIgnoreCase)
        {
            ["owner/has-milestone"] = [CreateMilestone("v1.1.0", 1)],
            ["owner/missing-milestone"] = [CreateMilestone("v1.0.0", 2)],
        };

        var result = PlanningBulkMilestoneAssigner.BuildSkipList(
            selectedItems,
            milestonesByRepository,
            "v1.1.0");

        Assert.Equal(["owner/missing-milestone"], result);
    }

    [Fact]
    public void BuildMilestoneOptions_UnionAcrossRepositories_ReturnsDistinctTitles()
    {
        var selectedItems = new[]
        {
            CreateUpNextItem("owner/repo-a", 10),
            CreateUpNextItem("owner/repo-b", 20),
        };

        var milestonesByRepository = new Dictionary<string, IReadOnlyList<Milestone>>(StringComparer.OrdinalIgnoreCase)
        {
            ["owner/repo-a"] = [CreateMilestone("v1.1.0", 1), CreateMilestone("v1.0.0", 2)],
            ["owner/repo-b"] = [CreateMilestone("v1.1.0", 3)],
        };

        var result = PlanningBulkMilestoneAssigner.BuildMilestoneOptions(selectedItems, milestonesByRepository);

        Assert.Equal(2, result.Count);
        Assert.Equal("v1.0.0", result[0].Title);
        Assert.Equal(["owner/repo-a"], result[0].RepositoryFullNames);
        Assert.Equal("v1.1.0", result[1].Title);
        Assert.Equal(["owner/repo-a", "owner/repo-b"], result[1].RepositoryFullNames);
    }

    [Fact]
    public void ResolveMilestoneNumber_TitleMatch_ReturnsNumber()
    {
        var milestones = new[] { CreateMilestone("v1.1.0", 7) };

        var result = PlanningBulkMilestoneAssigner.ResolveMilestoneNumber(milestones, "v1.1.0");

        Assert.Equal(7, result);
    }

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
