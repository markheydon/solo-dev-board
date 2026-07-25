namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Configuration options for GitHub API response caching.</summary>
public sealed class GitHubCacheOptions
{
    /// <summary>Configuration section name for GitHub response cache settings.</summary>
    public const string SectionName = "GitHub:Cache";

    /// <summary>Absolute cache lifetime in seconds for repository catalogue responses.</summary>
    public int RepositoriesTtlSeconds { get; set; } = 60;

    /// <summary>Absolute cache lifetime in seconds for label catalogue responses.</summary>
    public int LabelsTtlSeconds { get; set; } = 300;

    /// <summary>Absolute cache lifetime in seconds for milestone catalogue responses.</summary>
    public int MilestonesTtlSeconds { get; set; } = 300;
}
