using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Application-layer orchestration for one-at-a-time triage sessions.</summary>
public sealed class TriageService : ITriageService
{
    private readonly IGitHubService _gitHubService;

    /// <summary>Initialises a new instance of the <see cref="TriageService"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used to retrieve and apply triage actions.</param>
    public TriageService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
    }

    /// <inheritdoc/>
    public async Task<TriageSessionDto> StartSessionAsync(string owner, string repo, bool includePullRequests = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var issues = await _gitHubService.GetIssuesAsync(owner, repo, cancellationToken).ConfigureAwait(false);
        var queue = new List<TriageItem>(issues.Count + 16);
        queue.AddRange(issues
            .Where(issue => issue.Labels.Count == 0)
            .Select(issue => ToDomainIssueItem(owner, repo, issue)));

        if (includePullRequests)
        {
            var pullRequests = await _gitHubService.GetPullRequestsAsync(owner, repo, cancellationToken).ConfigureAwait(false);
            queue.AddRange(pullRequests.Select(pullRequest => ToDomainPullRequestItem(owner, repo, pullRequest)));
        }

        var orderedQueue = queue
            .OrderBy(item => item.UpdatedAt)
            .ToArray();

        var session = new TriageSession
        {
            SessionId = Guid.NewGuid(),
            OwnerLogin = owner,
            RepositoryName = repo,
            IncludePullRequests = includePullRequests,
            Queue = orderedQueue,
            CurrentIndex = 0,
            SkippedItems = [],
            ActionHistory = [],
            Progress = BuildProgress(orderedQueue.Length, 0, 0),
            Summary = BuildSummary(orderedQueue.Length, 0, 0, []),
            StartedAt = DateTimeOffset.UtcNow,
        };

        return ToDto(session);
    }

    /// <inheritdoc/>
    public Task<TriageSessionDto> AdvanceSessionAsync(TriageSessionDto session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(session);

        var domainSession = ToDomain(session);
        var nextIndex = domainSession.Queue.Count == 0
            ? 0
            : Math.Min(domainSession.CurrentIndex + 1, domainSession.Queue.Count);

        return Task.FromResult(UpdateSessionState(domainSession with { CurrentIndex = nextIndex }));
    }

    /// <inheritdoc/>
    public Task<TriageSessionDto> SkipCurrentItemAsync(TriageSessionDto session, string reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(session);

        var domainSession = ToDomain(session);
        if (domainSession.CurrentIndex >= domainSession.Queue.Count)
        {
            return Task.FromResult(ToDto(domainSession));
        }

        var currentItem = domainSession.Queue[domainSession.CurrentIndex];
        var updatedSkippedItems = domainSession.SkippedItems
            .Concat([currentItem])
            .DistinctBy(item => (item.ItemType, item.Number, item.RepositoryFullName))
            .ToArray();

        var detail = string.IsNullOrWhiteSpace(reason)
            ? "Skipped for later review."
            : $"Skipped for later review. Reason: {reason.Trim()}";

        var action = new TriageAction
        {
            ActionType = TriageActionType.Skipped,
            ItemType = currentItem.ItemType,
            ItemNumber = currentItem.Number,
            RepositoryFullName = currentItem.RepositoryFullName,
            Detail = detail,
            OccurredAt = DateTimeOffset.UtcNow,
        };

        var updatedActionHistory = domainSession.ActionHistory
            .Concat([action])
            .ToArray();

        // Remove the skipped item from the active queue so revisit can append it at the end.
        var updatedQueue = domainSession.Queue
            .Where((_, index) => index != domainSession.CurrentIndex)
            .ToArray();

        var nextIndex = Math.Min(domainSession.CurrentIndex, updatedQueue.Length);

        return Task.FromResult(UpdateSessionState(domainSession with
        {
            Queue = updatedQueue,
            CurrentIndex = nextIndex,
            SkippedItems = updatedSkippedItems,
            ActionHistory = updatedActionHistory,
        }));
    }

    /// <inheritdoc/>
    public Task<TriageSessionDto> RevisitSkippedItemsAsync(TriageSessionDto session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(session);

        var domainSession = ToDomain(session);
        if (domainSession.SkippedItems.Count == 0)
        {
            return Task.FromResult(ToDto(domainSession));
        }

        var skippedKeys = domainSession.SkippedItems
            .Select(item => (item.ItemType, item.Number, item.RepositoryFullName))
            .ToHashSet();

        // Ensure skipped items are moved to the end even if they still exist in the queue.
        var retainedQueue = domainSession.Queue
            .Where(item => !skippedKeys.Contains((item.ItemType, item.Number, item.RepositoryFullName)))
            .ToArray();

        var updatedQueue = retainedQueue
            .Concat(domainSession.SkippedItems)
            .ToArray();

        return Task.FromResult(UpdateSessionState(domainSession with
        {
            Queue = updatedQueue,
            SkippedItems = [],
        }));
    }

    /// <inheritdoc/>
    public async Task<TriageSessionDto> ApplyLabelToCurrentItemAsync(TriageSessionDto session, string labelName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);

        var domainSession = ToDomain(session);
        if (domainSession.CurrentIndex >= domainSession.Queue.Count)
        {
            return ToDto(domainSession);
        }

        var currentItem = domainSession.Queue[domainSession.CurrentIndex];
        if (!TryParseRepositoryScope(currentItem.RepositoryFullName, out var owner, out var repo))
        {
            throw new ArgumentException(
                $"Repository scope '{currentItem.RepositoryFullName}' must be in owner/repository format.",
                nameof(session));
        }

        var trimmedLabelName = labelName.Trim();
        var updatedLabels = currentItem.Labels
            .Select(label => label.Name)
            .Concat([trimmedLabelName])
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await _gitHubService
            .ApplyLabelsToTriageItemAsync(owner, repo, currentItem.Number, updatedLabels, cancellationToken)
            .ConfigureAwait(false);

        var updatedCurrentItem = currentItem with
        {
            Labels = updatedLabels.Select(name => new SoloDevBoard.Domain.Entities.Labels.Label { Name = name }).ToArray(),
        };

        var updatedQueue = domainSession.Queue
            .Select((item, index) => index == domainSession.CurrentIndex ? updatedCurrentItem : item)
            .ToArray();

        var action = new TriageAction
        {
            ActionType = TriageActionType.LabelApplied,
            ItemType = currentItem.ItemType,
            ItemNumber = currentItem.Number,
            RepositoryFullName = currentItem.RepositoryFullName,
            Detail = $"Applied label '{trimmedLabelName}'.",
            OccurredAt = DateTimeOffset.UtcNow,
        };

        var updatedActionHistory = domainSession.ActionHistory
            .Concat([action])
            .ToArray();

        return UpdateSessionState(domainSession with
        {
            Queue = updatedQueue,
            ActionHistory = updatedActionHistory,
        });
    }

    /// <inheritdoc/>
    public TriageSessionSummaryDto BuildSessionSummary(TriageSessionDto session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return UpdateSessionState(ToDomain(session)).Summary;
    }

    private static bool TryParseRepositoryScope(string repositoryFullName, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        if (string.IsNullOrWhiteSpace(repositoryFullName))
        {
            return false;
        }

        var segments = repositoryFullName.Split('/', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2)
        {
            return false;
        }

        owner = segments[0];
        repo = segments[1];
        return true;
    }

    private static TriageSessionDto UpdateSessionState(TriageSession session)
    {
        var progress = BuildProgress(session.Queue.Count, session.CurrentIndex, session.SkippedItems.Count);
        var summary = BuildSummary(session.Queue.Count, session.CurrentIndex, session.SkippedItems.Count, session.ActionHistory);

        var updated = session with
        {
            Progress = progress,
            Summary = summary,
        };

        return ToDto(updated);
    }

    private static TriageSessionProgress BuildProgress(int totalItems, int currentIndex, int skippedItems)
    {
        var processed = Math.Min(Math.Max(currentIndex, 0), totalItems);
        var remaining = Math.Max(totalItems - processed, 0);

        return new TriageSessionProgress
        {
            TotalItems = totalItems,
            ProcessedItems = processed,
            RemainingItems = remaining,
            SkippedItems = skippedItems,
        };
    }

    private static TriageSessionSummary BuildSummary(int totalItems, int currentIndex, int skippedItems, IReadOnlyList<TriageAction> actionHistory)
    {
        var processed = Math.Min(Math.Max(currentIndex, 0), totalItems);
        var remaining = Math.Max(totalItems - processed, 0);

        return new TriageSessionSummary
        {
            TotalItems = totalItems,
            ProcessedItems = processed,
            RemainingItems = remaining,
            SkippedItems = skippedItems,
            LabelsAppliedCount = actionHistory.Count(action => action.ActionType == TriageActionType.LabelApplied),
            MilestonesAssignedCount = actionHistory.Count(action => action.ActionType == TriageActionType.MilestoneAssigned),
            ProjectAssignmentsCount = actionHistory.Count(action => action.ActionType == TriageActionType.ProjectBoardAssigned),
            DuplicateClosuresCount = actionHistory.Count(action => action.ActionType == TriageActionType.ClosedAsDuplicate),
        };
    }

    private static TriageItem ToDomainIssueItem(string owner, string repo, Issue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);

        return new TriageItem
        {
            ItemType = TriageItemType.Issue,
            Id = issue.Id,
            Number = issue.Number,
            RepositoryFullName = $"{owner}/{repo}",
            Title = issue.Title,
            HtmlUrl = issue.HtmlUrl,
            Body = issue.Body,
            State = issue.State,
            AuthorLogin = issue.AuthorLogin,
            Labels = issue.Labels,
            Milestone = issue.Milestone,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt,
        };
    }

    private static TriageItem ToDomainPullRequestItem(string owner, string repo, PullRequest pullRequest)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);

        return new TriageItem
        {
            ItemType = TriageItemType.PullRequest,
            Id = pullRequest.Id,
            Number = pullRequest.Number,
            RepositoryFullName = $"{owner}/{repo}",
            Title = pullRequest.Title,
            HtmlUrl = pullRequest.HtmlUrl,
            Body = pullRequest.Body,
            State = pullRequest.State,
            AuthorLogin = pullRequest.AuthorLogin,
            Labels = [],
            Milestone = null,
            CreatedAt = pullRequest.CreatedAt,
            UpdatedAt = pullRequest.UpdatedAt,
        };
    }

    private static TriageSessionDto ToDto(TriageSession session)
    {
        return new TriageSessionDto(
            session.SessionId,
            session.OwnerLogin,
            session.RepositoryName,
            session.IncludePullRequests,
            session.Queue.Select(ToDto).ToArray(),
            session.CurrentIndex,
            session.SkippedItems.Select(ToDto).ToArray(),
            session.ActionHistory.Select(ToDto).ToArray(),
            ToDto(session.Progress),
            ToDto(session.Summary),
            session.StartedAt);
    }

    private static TriageSession ToDomain(TriageSessionDto session)
    {
        return new TriageSession
        {
            SessionId = session.SessionId,
            OwnerLogin = session.OwnerLogin,
            RepositoryName = session.RepositoryName,
            IncludePullRequests = session.IncludePullRequests,
            Queue = session.Queue.Select(ToDomain).ToArray(),
            CurrentIndex = session.CurrentIndex,
            SkippedItems = session.SkippedItems.Select(ToDomain).ToArray(),
            ActionHistory = session.ActionHistory.Select(ToDomain).ToArray(),
            Progress = ToDomain(session.Progress),
            Summary = ToDomain(session.Summary),
            StartedAt = session.StartedAt,
        };
    }

    private static TriageItemDto ToDto(TriageItem item)
    {
        return new TriageItemDto(
            item.ItemType == TriageItemType.PullRequest ? TriageItemTypeDto.PullRequest : TriageItemTypeDto.Issue,
            item.Id,
            item.Number,
            item.RepositoryFullName,
            item.Title,
            item.HtmlUrl,
            item.Body,
            item.State,
            item.AuthorLogin,
            item.Labels.Select(label => label.Name).ToArray(),
            item.Milestone?.Number,
            item.Milestone?.Title ?? string.Empty,
            item.CreatedAt,
            item.UpdatedAt);
    }

    private static TriageItem ToDomain(TriageItemDto item)
    {
        return new TriageItem
        {
            ItemType = item.ItemType == TriageItemTypeDto.PullRequest ? TriageItemType.PullRequest : TriageItemType.Issue,
            Id = item.Id,
            Number = item.Number,
            RepositoryFullName = item.RepositoryFullName,
            Title = item.Title,
            HtmlUrl = item.HtmlUrl,
            Body = item.Body,
            State = item.State,
            AuthorLogin = item.AuthorLogin,
            Labels = item.Labels.Select(labelName => new SoloDevBoard.Domain.Entities.Labels.Label { Name = labelName }).ToArray(),
            Milestone = item.MilestoneNumber is null
                ? null
                : new SoloDevBoard.Domain.Entities.Milestones.Milestone { Number = item.MilestoneNumber.Value, Title = item.MilestoneTitle },
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
    }

    private static TriageActionDto ToDto(TriageAction action)
    {
        return new TriageActionDto(
            ToDto(action.ActionType),
            action.ItemType == TriageItemType.PullRequest ? TriageItemTypeDto.PullRequest : TriageItemTypeDto.Issue,
            action.ItemNumber,
            action.RepositoryFullName,
            action.Detail,
            action.OccurredAt);
    }

    private static TriageAction ToDomain(TriageActionDto action)
    {
        return new TriageAction
        {
            ActionType = ToDomain(action.ActionType),
            ItemType = action.ItemType == TriageItemTypeDto.PullRequest ? TriageItemType.PullRequest : TriageItemType.Issue,
            ItemNumber = action.ItemNumber,
            RepositoryFullName = action.RepositoryFullName,
            Detail = action.Detail,
            OccurredAt = action.OccurredAt,
        };
    }

    private static TriageSessionProgressDto ToDto(TriageSessionProgress progress)
        => new(progress.TotalItems, progress.ProcessedItems, progress.RemainingItems, progress.SkippedItems);

    private static TriageSessionSummaryDto ToDto(TriageSessionSummary summary)
        => new(
            summary.TotalItems,
            summary.ProcessedItems,
            summary.RemainingItems,
            summary.SkippedItems,
            summary.LabelsAppliedCount,
            summary.MilestonesAssignedCount,
            summary.ProjectAssignmentsCount,
            summary.DuplicateClosuresCount);

    private static TriageSessionProgress ToDomain(TriageSessionProgressDto progress)
    {
        return new TriageSessionProgress
        {
            TotalItems = progress.TotalItems,
            ProcessedItems = progress.ProcessedItems,
            RemainingItems = progress.RemainingItems,
            SkippedItems = progress.SkippedItems,
        };
    }

    private static TriageSessionSummary ToDomain(TriageSessionSummaryDto summary)
    {
        return new TriageSessionSummary
        {
            TotalItems = summary.TotalItems,
            ProcessedItems = summary.ProcessedItems,
            RemainingItems = summary.RemainingItems,
            SkippedItems = summary.SkippedItems,
            LabelsAppliedCount = summary.LabelsAppliedCount,
            MilestonesAssignedCount = summary.MilestonesAssignedCount,
            ProjectAssignmentsCount = summary.ProjectAssignmentsCount,
            DuplicateClosuresCount = summary.DuplicateClosuresCount,
        };
    }

    private static TriageActionTypeDto ToDto(TriageActionType actionType)
        => actionType switch
        {
            TriageActionType.LabelApplied => TriageActionTypeDto.LabelApplied,
            TriageActionType.MilestoneAssigned => TriageActionTypeDto.MilestoneAssigned,
            TriageActionType.ProjectBoardAssigned => TriageActionTypeDto.ProjectBoardAssigned,
            TriageActionType.ClosedAsDuplicate => TriageActionTypeDto.ClosedAsDuplicate,
            _ => TriageActionTypeDto.Skipped,
        };

    private static TriageActionType ToDomain(TriageActionTypeDto actionType)
        => actionType switch
        {
            TriageActionTypeDto.LabelApplied => TriageActionType.LabelApplied,
            TriageActionTypeDto.MilestoneAssigned => TriageActionType.MilestoneAssigned,
            TriageActionTypeDto.ProjectBoardAssigned => TriageActionType.ProjectBoardAssigned,
            TriageActionTypeDto.ClosedAsDuplicate => TriageActionType.ClosedAsDuplicate,
            _ => TriageActionType.Skipped,
        };
}
