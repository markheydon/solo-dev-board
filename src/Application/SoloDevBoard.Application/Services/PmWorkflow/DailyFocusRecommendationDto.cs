namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>A ranked Daily Focus recommendation at the Application→App boundary.</summary>
/// <param name="Rank">The 1-based rank in the top-three list.</param>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="Number">The repository-scoped issue or pull request number.</param>
/// <param name="Title">The item title.</param>
/// <param name="HtmlUrl">The browser URL for the item on GitHub.</param>
/// <param name="PriorityLabel">The <c>priority/</c> label name, or <see langword="null"/> when unlabelled.</param>
public sealed record DailyFocusRecommendationDto(
    int Rank,
    string RepositoryFullName,
    int Number,
    string Title,
    string HtmlUrl,
    string? PriorityLabel);
