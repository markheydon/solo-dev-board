namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Discriminator for PM work-item catalogue entries.</summary>
public enum PmWorkItemTypeDto
{
    /// <summary>A GitHub issue.</summary>
    Issue,

    /// <summary>A GitHub pull request.</summary>
    PullRequest,
}
