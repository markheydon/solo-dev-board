using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>Blob store for <see cref="HostedSignInRecord"/> in container <c>hosted-sign-ins</c>.</summary>
public sealed class BlobHostedSignInRecordStore(
    HostedSignInsBlobContainer container,
    ILogger<BlobHostedSignInRecordStore> logger) : IHostedSignInRecordStore
{
    private readonly BlobContainerClient _containerClient =
        container?.Client ?? throw new ArgumentNullException(nameof(container));
    private readonly ILogger<BlobHostedSignInRecordStore> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HostedSignInRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var records = new List<HostedSignInRecord>();
        await foreach (var blob in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (!blob.Name.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            var client = _containerClient.GetBlobClient(blob.Name);
            var download = await client.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            var record = HostedSignInRecordJson.Deserialise(download.Value.Content.ToString());
            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    /// <inheritdoc/>
    public async Task<HostedSignInRecordReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin);
        if (sanitised is null)
        {
            return new HostedSignInRecordReadResult.UnsafeKey();
        }

        var blob = _containerClient.GetBlobClient($"{sanitised}.json");
        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return new HostedSignInRecordReadResult.NotFound();
        }

        var download = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        var record = HostedSignInRecordJson.Deserialise(download.Value.Content.ToString());
        if (record is null)
        {
            return new HostedSignInRecordReadResult.NotFound();
        }

        return new HostedSignInRecordReadResult.Found(record, download.Value.Details.ETag.ToString());
    }

    /// <inheritdoc/>
    public async Task<HostedSignInRecordWriteResult> UpsertAsync(
        HostedSignInRecord record,
        string? expectedEtag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var sanitised = OperatorLoginSanitiser.Sanitise(record.OperatorLogin);
        if (sanitised is null)
        {
            return new HostedSignInRecordWriteResult.UnsafeKey();
        }

        record.OperatorLogin = sanitised;
        var blob = _containerClient.GetBlobClient($"{sanitised}.json");
        var exists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        var payload = HostedSignInRecordJson.Serialise(record);

        try
        {
            var conditions = new BlobRequestConditions();
            if (exists && !string.IsNullOrWhiteSpace(expectedEtag))
            {
                conditions.IfMatch = new ETag(expectedEtag);
            }
            else if (!exists)
            {
                conditions.IfNoneMatch = ETag.All;
            }

            await blob.UploadAsync(
                    BinaryData.FromString(payload),
                    new BlobUploadOptions { Conditions = conditions },
                    cancellationToken)
                .ConfigureAwait(false);

            var etag = (await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Value.ETag
                .ToString();
            return new HostedSignInRecordWriteResult.Succeeded(etag);
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            return new HostedSignInRecordWriteResult.ConcurrencyConflict();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write hosted sign-in record.");
            return new HostedSignInRecordWriteResult.ConcurrencyConflict();
        }
    }
}
