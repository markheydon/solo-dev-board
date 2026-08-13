namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Configuration options for GitHub API pagination limits.</summary>
public sealed class GitHubPaginationOptions
{
    /// <summary>Configuration section name for GitHub pagination settings.</summary>
    public const string SectionName = "GitHub:Pagination";

    /// <summary>Maximum number of pages to fetch for workflow run catalogue responses.</summary>
    public int WorkflowRunsMaxPages { get; set; } = 5;
}
