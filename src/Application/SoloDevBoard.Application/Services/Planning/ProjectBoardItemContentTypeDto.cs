namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Defines the supported project board item content types.</summary>
public enum ProjectBoardItemContentTypeDto
{
    /// <summary>Represents a GitHub issue.</summary>
    Issue = 0,

    /// <summary>Represents a GitHub pull request.</summary>
    PullRequest = 1,
}
