namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Defines the supported triage item types.</summary>
public enum TriageItemType
{
    /// <summary>Represents a GitHub issue.</summary>
    Issue = 0,

    /// <summary>Represents a GitHub pull request.</summary>
    PullRequest = 1,
}
