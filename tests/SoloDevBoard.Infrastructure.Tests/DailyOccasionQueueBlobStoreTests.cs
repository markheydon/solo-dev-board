using Microsoft.Extensions.Logging.Abstractions;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Infrastructure.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class DailyOccasionQueueBlobStoreTests
{
    [Fact]
    public async Task TryCreateAsync_WhenAbsent_CreatesOnce()
    {
        var container = new FakeOperatorSnapshotBlobContainer();
        var store = new DailyOccasionQueueBlobStore(container, NullLogger<DailyOccasionQueueBlobStore>.Instance);
        var queue = new DailyOccasionQueue
        {
            OccasionDate = new DateOnly(2026, 6, 1),
            FilledAt = DateTimeOffset.UtcNow,
        };

        Assert.True(await store.TryCreateAsync(queue, TestContext.Current.CancellationToken));
        Assert.False(await store.TryCreateAsync(queue, TestContext.Current.CancellationToken));
    }
}
