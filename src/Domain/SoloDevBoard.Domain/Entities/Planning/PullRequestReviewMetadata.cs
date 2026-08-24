namespace SoloDevBoard.Domain.Entities.Planning;

/// <summary>Review-pending metadata for an open pull request.</summary>
public sealed record PullRequestReviewMetadata
{
    /// <summary>Gets the repository-scoped pull request number.</summary>
    public int Number { get; init; }

    /// <summary>Gets a value indicating whether the pull request has a pending review signal.</summary>
    public bool HasReviewPending { get; init; }
}
