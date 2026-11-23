namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Decides whether a stored in-progress run should be rewritten as interrupted on read.</summary>
public interface IOperatorSnapshotInProgressReadPolicy
{
    /// <summary>Returns false when a stored in-progress run must stay in progress (for example retry waiting).</summary>
    Task<bool> ShouldRewriteStaleInProgressAsync(string sanitisedLogin, CancellationToken cancellationToken = default);
}
