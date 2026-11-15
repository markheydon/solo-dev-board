namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Discriminator for PM work-item catalogue entries.</summary>
public enum PlanningWorkItemTypeDto
{
    /// <summary>A GitHub issue.</summary>
    Issue,

    /// <summary>A GitHub pull request.</summary>
    PullRequest,
}
