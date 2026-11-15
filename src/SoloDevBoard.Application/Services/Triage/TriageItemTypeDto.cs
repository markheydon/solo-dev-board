namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Defines the supported triage item types for Application-layer DTOs.</summary>
public enum TriageItemTypeDto
{
    /// <summary>Represents a GitHub issue.</summary>
    Issue = 0,

    /// <summary>Represents a GitHub pull request.</summary>
    PullRequest = 1,
}
