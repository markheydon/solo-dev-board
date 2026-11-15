namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Provides the built-in recommended label taxonomies shared by Label Manager and Audit Dashboard.</summary>
public static class RecommendedLabelTaxonomyCatalog
{
    /// <summary>The identifier for the SoloDevBoard canonical taxonomy.</summary>
    public const string SoloDevBoardStrategyId = "solodevboard";

    /// <summary>The identifier for GitHub's default new-repository label set.</summary>
    public const string GitHubDefaultStrategyId = "github-default";

    /// <summary>Gets the SoloDevBoard canonical taxonomy labels.</summary>
    public static IReadOnlyList<LabelDto> SoloDevBoard { get; } =
    [
        new("type/epic", "6f42c1", "A named product theme spanning multiple features or a major increment - not a milestone bucket", string.Empty),
        new("type/feature", "0075ca", "A Feature - groups related stories within an epic", string.Empty),
        new("type/story", "1d76db", "A user-facing Story delivering a discrete piece of value", string.Empty),
        new("type/enabler", "e4e669", "An Enabler - technical prerequisite that unblocks stories", string.Empty),
        new("type/test", "bfd4f2", "A Test issue - test coverage deliverable (unit, component, integration)", string.Empty),
        new("type/bug", "d73a4a", "A bug or unexpected behaviour", string.Empty),
        new("type/chore", "fef2c0", "Maintenance, dependency updates, or technical debt", string.Empty),
        new("type/documentation", "0052cc", "Documentation additions or improvements", string.Empty),

        new("priority/critical", "b60205", "Blocking - must be resolved immediately", string.Empty),
        new("priority/high", "d93f0b", "Should be addressed in the current sprint or release", string.Empty),
        new("priority/medium", "fbca04", "Should be addressed soon but is not blocking", string.Empty),
        new("priority/low", "c2e0c6", "Nice to have; can be deferred", string.Empty),

        new("status/todo", "ffffff", "Ready to be worked on; not yet started", string.Empty),
        new("status/in-progress", "0e8a16", "Currently being worked on", string.Empty),
        new("status/blocked", "e11d48", "Cannot proceed; waiting on something external", string.Empty),
        new("status/ice-box", "8b949e", "Shelved for later; not in the active delivery queue", string.Empty),
        new("status/in-review", "1d76db", "Pull request open; awaiting code review", string.Empty),
        new("status/done", "cfd3d7", "Completed and closed", string.Empty),

        new("size/xs", "dde8c9", "Trivial - less than 1 hour (e.g. typo fix, config change)", string.Empty),
        new("size/s", "c5def5", "Small - less than half a day", string.Empty),
        new("size/m", "fef2c0", "Medium - half a day to one day", string.Empty),
        new("size/l", "f9d0c4", "Large - two to three days", string.Empty),
        new("size/xl", "d4c5f9", "Extra-large - more than three days; consider splitting", string.Empty),
    ];

    /// <summary>Gets GitHub's default new-repository label set.</summary>
    public static IReadOnlyList<LabelDto> GitHubDefault { get; } =
    [
        new("bug", "d73a4a", "Something is not working", string.Empty),
        new("documentation", "0075ca", "Improvements or additions to documentation", string.Empty),
        new("duplicate", "cfd3d7", "This issue or pull request already exists", string.Empty),
        new("enhancement", "a2eeef", "New feature or request", string.Empty),
        new("good first issue", "7057ff", "Good for newcomers", string.Empty),
        new("help wanted", "008672", "Extra attention is needed", string.Empty),
        new("invalid", "e4e669", "This does not appear to be valid", string.Empty),
        new("question", "d876e3", "Further information is requested", string.Empty),
        new("wontfix", "ffffff", "This will not be worked on", string.Empty),
    ];

    /// <summary>Gets the available built-in recommended strategies.</summary>
    public static IReadOnlyList<RecommendedLabelStrategyDto> Strategies { get; } =
    [
        new(SoloDevBoardStrategyId, "SoloDevBoard", "The SoloDevBoard canonical taxonomy covering type, priority, status, and size labels."),
        new(GitHubDefaultStrategyId, "GitHub default", "GitHub's default label set for new repositories."),
    ];

    /// <summary>Attempts to resolve the label set for a recommended strategy identifier.</summary>
    /// <param name="strategyId">The strategy identifier to resolve.</param>
    /// <param name="labels">When this method returns <see langword="true"/>, the matching label set.</param>
    /// <returns><see langword="true"/> when the strategy identifier is recognised; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetLabels(string strategyId, out IReadOnlyList<LabelDto> labels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);

        if (strategyId.Equals(SoloDevBoardStrategyId, StringComparison.OrdinalIgnoreCase))
        {
            labels = SoloDevBoard;
            return true;
        }

        if (strategyId.Equals(GitHubDefaultStrategyId, StringComparison.OrdinalIgnoreCase))
        {
            labels = GitHubDefault;
            return true;
        }

        labels = [];
        return false;
    }
}
