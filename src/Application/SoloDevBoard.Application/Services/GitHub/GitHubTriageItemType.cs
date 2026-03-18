namespace SoloDevBoard.Application.Services.GitHub;

/// <summary>Defines triage item types used by GitHub write operations.</summary>
public enum GitHubTriageItemType
{
    /// <summary>Represents a GitHub issue.</summary>
    Issue = 0,

    /// <summary>Represents a GitHub pull request.</summary>
    PullRequest = 1,
}
