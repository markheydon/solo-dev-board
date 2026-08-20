namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>A pull request waiting on review for at least the configured stall threshold.</summary>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="Number">The repository-scoped pull request number.</param>
/// <param name="AgeDays">Whole days since the stall clock started (Status time or pull request created time).</param>
/// <param name="HtmlUrl">The GitHub URL for the pull request.</param>
/// <param name="Title">The pull request title.</param>
public sealed record DailyFocusStalledReviewPullRequestDto(
    string RepositoryFullName,
    int Number,
    int AgeDays,
    string HtmlUrl,
    string Title);
