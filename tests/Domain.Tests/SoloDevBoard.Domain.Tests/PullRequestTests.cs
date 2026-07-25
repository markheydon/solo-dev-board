using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Domain.Tests;

public sealed class PullRequestTests
{
    [Fact]
    public void PullRequest_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 1, 11, 14, 30, 0, TimeSpan.Zero);
        var milestone = new Milestone { Id = 2, Number = 1, Title = "v1.0.0" };
        var labels = new List<Label>
        {
            new() { Name = "enhancement", Colour = "a2eeef" },
        };

        // Act
        var pullRequest = new PullRequest
        {
            Id = 500,
            Number = 42,
            Title = "Add triage support",
            HtmlUrl = "https://github.com/owner/repo/pull/42",
            Body = "Implements triage queue handling.",
            State = "open",
            AuthorLogin = "contributor",
            HeadBranch = "feature/triage",
            BaseBranch = "main",
            IsDraft = true,
            Labels = labels,
            Milestone = milestone,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };

        // Assert
        Assert.Equal(500, pullRequest.Id);
        Assert.Equal(42, pullRequest.Number);
        Assert.Equal("Add triage support", pullRequest.Title);
        Assert.Equal("https://github.com/owner/repo/pull/42", pullRequest.HtmlUrl);
        Assert.Equal("Implements triage queue handling.", pullRequest.Body);
        Assert.Equal("open", pullRequest.State);
        Assert.Equal("contributor", pullRequest.AuthorLogin);
        Assert.Equal("feature/triage", pullRequest.HeadBranch);
        Assert.Equal("main", pullRequest.BaseBranch);
        Assert.True(pullRequest.IsDraft);
        Assert.Single(pullRequest.Labels);
        Assert.Equal(milestone, pullRequest.Milestone);
        Assert.Equal(createdAt, pullRequest.CreatedAt);
        Assert.Equal(updatedAt, pullRequest.UpdatedAt);
    }
}
