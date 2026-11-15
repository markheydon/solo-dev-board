using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Domain.Tests;

public sealed class TriageEntityTests
{
    [Fact]
    public void TriageSession_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        var queueItem = new TriageItem
        {
            ItemType = TriageItemType.Issue,
            Id = 1,
            Number = 10,
            RepositoryFullName = "owner/repo",
            Title = "Triage me",
        };
        var skippedItem = queueItem with { Number = 11, Title = "Skipped item" };
        var action = new TriageAction
        {
            Id = 1,
            ActionType = TriageActionType.Skipped,
            ItemType = TriageItemType.Issue,
            ItemNumber = 11,
            RepositoryFullName = "owner/repo",
            Detail = "Needs more context",
            OccurredAt = startedAt,
        };
        var progress = new TriageSessionProgress
        {
            Id = 1,
            TotalItems = 2,
            ProcessedItems = 1,
            RemainingItems = 1,
            SkippedItems = 1,
        };
        var summary = new TriageSessionSummary
        {
            Id = 1,
            TotalItems = 2,
            ProcessedItems = 1,
            RemainingItems = 1,
            SkippedItems = 1,
            LabelsAppliedCount = 1,
            MilestonesAssignedCount = 0,
            ProjectAssignmentsCount = 0,
            DuplicateClosuresCount = 0,
            LabelActionDetails = ["bug"],
            MilestoneActionDetails = [],
            ProjectActionDetails = [],
            DuplicateActionDetails = [],
            SkippedActionDetails = ["Needs more context"],
            SkippedItemDetails = ["#11 Skipped item"],
        };

        // Act
        var session = new TriageSession
        {
            Id = 1,
            SessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            OwnerLogin = "owner",
            RepositoryName = "repo",
            IncludePullRequests = true,
            Queue = [queueItem],
            CurrentIndex = 0,
            SkippedItems = [skippedItem],
            ActionHistory = [action],
            Progress = progress,
            Summary = summary,
            StartedAt = startedAt,
        };

        // Assert
        Assert.Equal(1, session.Id);
        Assert.Equal(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), session.SessionId);
        Assert.Equal("owner", session.OwnerLogin);
        Assert.Equal("repo", session.RepositoryName);
        Assert.True(session.IncludePullRequests);
        Assert.Single(session.Queue);
        Assert.Equal(0, session.CurrentIndex);
        Assert.Single(session.SkippedItems);
        Assert.Single(session.ActionHistory);
        Assert.Equal(progress, session.Progress);
        Assert.Equal(summary, session.Summary);
        Assert.Equal(startedAt, session.StartedAt);
    }

    [Fact]
    public void TriageItem_WithMilestoneAndLabels_ShouldReturnCorrectValues()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 2, 2, 11, 0, 0, TimeSpan.Zero);
        var milestone = new Milestone { Id = 3, Number = 2, Title = "Backlog" };
        var labels = new List<Label> { new() { Name = "triage", Colour = "ededed" } };

        // Act
        var item = new TriageItem
        {
            ItemType = TriageItemType.PullRequest,
            Id = 99,
            Number = 7,
            RepositoryFullName = "owner/repo",
            Title = "Review requested",
            HtmlUrl = "https://github.com/owner/repo/pull/7",
            Body = "Please review.",
            State = "open",
            AuthorLogin = "reviewer",
            Labels = labels,
            Milestone = milestone,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };

        // Assert
        Assert.Equal(TriageItemType.PullRequest, item.ItemType);
        Assert.Equal(99, item.Id);
        Assert.Equal(7, item.Number);
        Assert.Equal("owner/repo", item.RepositoryFullName);
        Assert.Equal("Review requested", item.Title);
        Assert.Equal("https://github.com/owner/repo/pull/7", item.HtmlUrl);
        Assert.Equal("Please review.", item.Body);
        Assert.Equal("open", item.State);
        Assert.Equal("reviewer", item.AuthorLogin);
        Assert.Single(item.Labels);
        Assert.Equal(milestone, item.Milestone);
        Assert.Equal(createdAt, item.CreatedAt);
        Assert.Equal(updatedAt, item.UpdatedAt);
    }

    [Fact]
    public void TriageProjectBoard_WithStatusOptions_ShouldReturnCorrectValues()
    {
        // Arrange & Act
        var board = new TriageProjectBoard
        {
            Id = "PVT_789",
            Title = "Inbox",
            OwnerLogin = "owner",
            StatusFieldId = "FIELD_1",
            StatusOptions =
            [
                new TriageProjectBoardStatusOption { Id = "OPT_1", Name = "Todo" },
                new TriageProjectBoardStatusOption { Id = "OPT_2", Name = "Done" },
            ],
        };

        // Assert
        Assert.Equal("PVT_789", board.Id);
        Assert.Equal("Inbox", board.Title);
        Assert.Equal("owner", board.OwnerLogin);
        Assert.Equal("FIELD_1", board.StatusFieldId);
        Assert.Equal(2, board.StatusOptions.Count);
        Assert.Equal("Todo", board.StatusOptions[0].Name);
        Assert.Equal("OPT_2", board.StatusOptions[1].Id);
    }

    [Fact]
    public void TriageActionType_Values_ShouldContainExpectedMembers()
    {
        // Assert
        Assert.Equal(
            [
                nameof(TriageActionType.LabelApplied),
                nameof(TriageActionType.MilestoneAssigned),
                nameof(TriageActionType.ProjectBoardAssigned),
                nameof(TriageActionType.ClosedAsDuplicate),
                nameof(TriageActionType.Skipped),
            ],
            Enum.GetNames<TriageActionType>());
    }

    [Fact]
    public void TriageItemType_Values_ShouldContainExpectedMembers()
    {
        // Assert
        Assert.Equal(
            [
                nameof(TriageItemType.Issue),
                nameof(TriageItemType.PullRequest),
            ],
            Enum.GetNames<TriageItemType>());
    }
}
