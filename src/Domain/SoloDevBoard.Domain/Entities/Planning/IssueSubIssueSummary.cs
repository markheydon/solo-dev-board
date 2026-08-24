namespace SoloDevBoard.Domain.Entities.Planning;

/// <summary>Tracked sub-issue counts for an open epic or feature issue.</summary>
public sealed record IssueSubIssueSummary
{
    /// <summary>Gets the repository-scoped issue number.</summary>
    public int Number { get; init; }

    /// <summary>Gets the total number of tracked sub-issues.</summary>
    public int TotalCount { get; init; }

    /// <summary>Gets the number of completed tracked sub-issues.</summary>
    public int CompletedCount { get; init; }
}
