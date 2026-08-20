namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Builds stable join keys for matching catalogue work items to planning-board cards.</summary>
public static class PmWorkItemJoinKey
{
    /// <summary>Builds a join key for a catalogue work item.</summary>
    /// <param name="item">The catalogue work item.</param>
    /// <returns>A key that distinguishes issues from pull requests that share a number.</returns>
    public static string For(PmWorkItemDto item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return For(item.ItemType == PmWorkItemTypeDto.PullRequest, item.RepositoryFullName, item.Number);
    }

    /// <summary>Builds a join key for a planning-board item.</summary>
    /// <param name="item">The planning-board item.</param>
    /// <returns>A key that distinguishes issues from pull requests that share a number.</returns>
    public static string For(ProjectBoardItemDto item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var repositoryFullName = $"{item.Content.RepositoryOwner}/{item.Content.RepositoryName}";
        var isPullRequest = item.Content.ContentType == ProjectBoardItemContentTypeDto.PullRequest;
        return For(isPullRequest, repositoryFullName, item.Content.Number);
    }

    /// <summary>Builds a join key from the item kind, repository, and number.</summary>
    /// <param name="isPullRequest"><see langword="true" /> when the item is a pull request; otherwise, <see langword="false" />.</param>
    /// <param name="repositoryFullName">The repository in <c>owner/name</c> form.</param>
    /// <param name="number">The repository-scoped issue or pull request number.</param>
    /// <returns>A key in the form <c>issue:owner/name#n</c> or <c>pr:owner/name#n</c>.</returns>
    public static string For(bool isPullRequest, string repositoryFullName, int number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryFullName);
        var kind = isPullRequest ? "pr" : "issue";
        return $"{kind}:{repositoryFullName}#{number}";
    }
}
