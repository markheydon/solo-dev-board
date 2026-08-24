using System.Text.RegularExpressions;

namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Pure detection of Daily Focus pull requests stalled awaiting review.</summary>
public static partial class DailyFocusStalledReviewDetector
{
    /// <summary>Canonical Status option name for work waiting on review.</summary>
    public const string InReviewStatusName = "In Review";

    /// <summary>
    /// Returns whether the selected board exposes an In Review Status, or a clearly equivalent name
    /// such as <c>Waiting on review</c> or <c>Code Review</c>.
    /// </summary>
    /// <param name="statusOptions">Status options discovered on the selected board.</param>
    /// <param name="items">Items currently on the selected board, used when option metadata is empty.</param>
    /// <returns><see langword="true"/> when an In Review equivalent Status is present.</returns>
    public static bool BoardHasInReviewStatus(
        IReadOnlyList<ProjectBoardStatusOptionDto> statusOptions,
        IReadOnlyList<ProjectBoardItemDto> items)
    {
        ArgumentNullException.ThrowIfNull(statusOptions);
        ArgumentNullException.ThrowIfNull(items);

        if (statusOptions.Any(static option => IsInReviewEquivalent(option.Name)))
        {
            return true;
        }

        return items.Any(static item => IsInReviewEquivalent(item.Status?.Name));
    }

    /// <summary>
    /// Detects stalled pull requests from time spent in an In Review (or equivalent) Status column.
    /// </summary>
    /// <param name="items">Items currently on the selected board.</param>
    /// <param name="utcNow">The current UTC instant used for age calculation.</param>
    /// <param name="stallDays">Inclusive stall threshold in days.</param>
    /// <param name="excludedRepositories">Repositories omitted from the result, in <c>owner/name</c> form.</param>
    /// <returns>Stalled pull requests, oldest first.</returns>
    public static IReadOnlyList<DailyFocusStalledReviewPullRequestDto> DetectFromBoardColumn(
        IReadOnlyList<ProjectBoardItemDto> items,
        DateTimeOffset utcNow,
        int stallDays,
        IReadOnlyList<string> excludedRepositories)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(excludedRepositories);

        var excluded = new HashSet<string>(excludedRepositories, StringComparer.OrdinalIgnoreCase);
        var resolvedStallDays = ResolveStallDays(stallDays);

        return items
            .Where(item =>
                item.Content.ContentType == ProjectBoardItemContentTypeDto.PullRequest
                && IsInReviewEquivalent(item.Status?.Name)
                && !IsExcluded(item.Content.RepositoryOwner, item.Content.RepositoryName, excluded)
                && IsStalled(item.ActivityTimestamp, utcNow, resolvedStallDays))
            .Select(item => MapBoardItem(item, utcNow))
            .OrderByDescending(static row => row.AgeDays)
            .ThenBy(static row => row.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static row => row.Number)
            .ToArray();
    }

    /// <summary>
    /// Detects stalled pull requests from open, non-draft catalogue items with a pending review.
    /// </summary>
    /// <param name="workItems">Open issues and pull requests from the PM work-item catalogue.</param>
    /// <param name="utcNow">The current UTC instant used for age calculation.</param>
    /// <param name="stallDays">Inclusive stall threshold in days.</param>
    /// <param name="excludedRepositories">Repositories omitted from the result, in <c>owner/name</c> form.</param>
    /// <returns>Stalled pull requests, oldest first.</returns>
    public static IReadOnlyList<DailyFocusStalledReviewPullRequestDto> DetectFromPendingReviewCatalogue(
        IReadOnlyList<PlanningWorkItemDto> workItems,
        DateTimeOffset utcNow,
        int stallDays,
        IReadOnlyList<string> excludedRepositories)
    {
        ArgumentNullException.ThrowIfNull(workItems);
        ArgumentNullException.ThrowIfNull(excludedRepositories);

        var excluded = new HashSet<string>(excludedRepositories, StringComparer.OrdinalIgnoreCase);
        var resolvedStallDays = ResolveStallDays(stallDays);

        return workItems
            .Where(item =>
                item.ItemType == PlanningWorkItemTypeDto.PullRequest
                && item.IsDraft == false
                && item.HasReviewPending == true
                && !excluded.Contains(item.RepositoryFullName)
                && IsStalled(item.CreatedAt, utcNow, resolvedStallDays))
            .Select(item => MapWorkItem(item, utcNow))
            .OrderByDescending(static row => row.AgeDays)
            .ThenBy(static row => row.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static row => row.Number)
            .ToArray();
    }

    /// <summary>Returns whether a Status display name is In Review or a clearly equivalent label.</summary>
    /// <param name="statusName">The Status option display name, or <see langword="null"/> when unset.</param>
    /// <returns><see langword="true"/> when the name denotes a review column.</returns>
    public static bool IsInReviewEquivalent(string? statusName)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return false;
        }

        if (statusName.Equals(InReviewStatusName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return StatusWordPattern()
            .Matches(statusName)
            .Any(static match => match.Value.Equals("review", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Returns whether the elapsed time meets the inclusive stall threshold.</summary>
    /// <param name="timestamp">The stall clock start (Status-changed time or created time).</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="stallDays">Inclusive stall threshold in days.</param>
    /// <returns><see langword="true"/> when the age is at least <paramref name="stallDays"/>.</returns>
    public static bool IsStalled(DateTimeOffset timestamp, DateTimeOffset utcNow, int stallDays)
    {
        var resolvedStallDays = ResolveStallDays(stallDays);
        return utcNow - timestamp >= TimeSpan.FromDays(resolvedStallDays);
    }

    private static DailyFocusStalledReviewPullRequestDto MapBoardItem(
        ProjectBoardItemDto item,
        DateTimeOffset utcNow)
    {
        var repositoryFullName = $"{item.Content.RepositoryOwner}/{item.Content.RepositoryName}";
        return new DailyFocusStalledReviewPullRequestDto(
            repositoryFullName,
            item.Content.Number,
            AgeDays(item.ActivityTimestamp, utcNow),
            item.Content.Url,
            item.Content.Title);
    }

    private static DailyFocusStalledReviewPullRequestDto MapWorkItem(
        PlanningWorkItemDto item,
        DateTimeOffset utcNow)
        => new(
            item.RepositoryFullName,
            item.Number,
            AgeDays(item.CreatedAt, utcNow),
            item.HtmlUrl,
            item.Title);

    private static int AgeDays(DateTimeOffset timestamp, DateTimeOffset utcNow)
    {
        var elapsed = utcNow - timestamp;
        return elapsed < TimeSpan.Zero ? 0 : elapsed.Days;
    }

    private static int ResolveStallDays(int stallDays)
        => stallDays > 0 ? stallDays : PlanningSettingsDefaults.StallDays;

    private static bool IsExcluded(
        string repositoryOwner,
        string repositoryName,
        HashSet<string> excluded)
        => excluded.Contains($"{repositoryOwner}/{repositoryName}");

    [GeneratedRegex(@"\p{L}+", RegexOptions.CultureInvariant)]
    private static partial Regex StatusWordPattern();
}
