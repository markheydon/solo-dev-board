using Azure;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class DailyOccasionQueueBlobStore(
    IOperatorSnapshotBlobContainer container,
    ILogger<DailyOccasionQueueBlobStore> logger) : IDailyOccasionQueueStore
{
    private const string BlobName = "schedule/occasion.json";

    private readonly IOperatorSnapshotBlobContainer _container = container ?? throw new ArgumentNullException(nameof(container));
    private readonly ILogger<DailyOccasionQueueBlobStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<DailyOccasionQueue?> GetAsync(CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(BlobName);
        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var download = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
        var queue = ScheduleJson.DeserialiseOccasionQueue(download.Content);
        if (queue is null)
        {
            return null;
        }

        queue.BlobEtag = download.Etag;
        return queue;
    }

    /// <inheritdoc/>
    public async Task<bool> TryCreateAsync(DailyOccasionQueue queue, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(BlobName);
        var payload = ScheduleJson.SerialiseOccasionQueue(queue);
        try
        {
            await blob.UploadAsync(
                    payload,
                    new OperatorSnapshotBlobUploadConditions(null, IfNoneMatchAll: true),
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status is 409 or 412)
        {
            _logger.LogDebug("Occasion queue already created.");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateAsync(
        DailyOccasionQueue queue,
        string ifMatchEtag,
        CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(BlobName);
        var payload = ScheduleJson.SerialiseOccasionQueue(queue);
        try
        {
            await blob.UploadAsync(
                    payload,
                    new OperatorSnapshotBlobUploadConditions(ifMatchEtag, IfNoneMatchAll: false),
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status is 409 or 412)
        {
            _logger.LogDebug("Occasion queue update lost race.");
            return false;
        }
    }
}
