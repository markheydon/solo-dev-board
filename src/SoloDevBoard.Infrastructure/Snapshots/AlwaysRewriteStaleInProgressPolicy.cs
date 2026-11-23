using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Default policy: stale in-progress runs are interrupted on read.</summary>
public sealed class AlwaysRewriteStaleInProgressPolicy : IOperatorSnapshotInProgressReadPolicy
{
    /// <inheritdoc/>
    public Task<bool> ShouldRewriteStaleInProgressAsync(string sanitisedLogin, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
