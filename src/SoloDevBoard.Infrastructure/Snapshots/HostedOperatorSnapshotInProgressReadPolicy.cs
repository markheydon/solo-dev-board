using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class HostedOperatorSnapshotInProgressReadPolicy(IDailyOccasionQueueStore queueStore) : IOperatorSnapshotInProgressReadPolicy
{
    private readonly IDailyOccasionQueueStore _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));

    /// <inheritdoc/>
    public async Task<bool> ShouldRewriteStaleInProgressAsync(string sanitisedLogin, CancellationToken cancellationToken = default)
    {
        var queue = await _queueStore.GetAsync(cancellationToken).ConfigureAwait(false);
        var entry = queue?.FindEntry(sanitisedLogin);
        return entry?.State != QueueEntryState.RetryWaiting;
    }
}
