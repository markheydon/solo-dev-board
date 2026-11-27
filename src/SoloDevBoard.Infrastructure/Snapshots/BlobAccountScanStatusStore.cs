using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc />
public sealed class BlobAccountScanStatusStore(
    AccountScanStatusBlobContainer container,
    ILogger<BlobAccountScanStatusStore> logger) : IAccountScanStatusStore
{
    private readonly BlobContainerClient _containerClient =
        container?.Client ?? throw new ArgumentNullException(nameof(container));
    private readonly ILogger<BlobAccountScanStatusStore> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<AccountScanStatusReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin);
        if (sanitised is null)
        {
            return new AccountScanStatusReadResult.UnsafeKey();
        }

        var blob = _containerClient.GetBlobClient($"{sanitised}.json");
        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return new AccountScanStatusReadResult.NotFound();
        }

        var download = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        var document = AccountScanStatusJson.Deserialise(download.Value.Content.ToString());
        if (document is null)
        {
            return new AccountScanStatusReadResult.NotFound();
        }

        return new AccountScanStatusReadResult.Found(document, download.Value.Details.ETag.ToString());
    }

    /// <inheritdoc/>
    public async Task<AccountScanStatusWriteResult> TryCreateAsync(
        AccountScanStatusDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var sanitised = OperatorLoginSanitiser.Sanitise(document.OperatorLogin);
        if (sanitised is null)
        {
            return new AccountScanStatusWriteResult.UnsafeKey();
        }

        document.OperatorLogin = sanitised;
        var blob = _containerClient.GetBlobClient($"{sanitised}.json");
        if (await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return new AccountScanStatusWriteResult.AlreadyExists();
        }

        try
        {
            var payload = AccountScanStatusJson.Serialise(document);
            await blob.UploadAsync(
                    BinaryData.FromString(payload),
                    new BlobUploadOptions
                    {
                        Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
                        HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" },
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            var etag = (await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Value.ETag
                .ToString();
            return new AccountScanStatusWriteResult.Succeeded(etag);
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            return new AccountScanStatusWriteResult.AlreadyExists();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account scan status blob.");
            return new AccountScanStatusWriteResult.ConcurrencyConflict();
        }
    }

    /// <inheritdoc/>
    public async Task<AccountScanStatusWriteResult> TryReplaceAsync(
        AccountScanStatusDocument document,
        string expectedEtag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var sanitised = OperatorLoginSanitiser.Sanitise(document.OperatorLogin);
        if (sanitised is null)
        {
            return new AccountScanStatusWriteResult.UnsafeKey();
        }

        document.OperatorLogin = sanitised;
        var blob = _containerClient.GetBlobClient($"{sanitised}.json");
        try
        {
            var payload = AccountScanStatusJson.Serialise(document);
            await blob.UploadAsync(
                    BinaryData.FromString(payload),
                    new BlobUploadOptions
                    {
                        Conditions = new BlobRequestConditions { IfMatch = new ETag(expectedEtag) },
                        HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" },
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            var etag = (await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Value.ETag
                .ToString();
            return new AccountScanStatusWriteResult.Succeeded(etag);
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            return new AccountScanStatusWriteResult.ConcurrencyConflict();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace account scan status blob.");
            return new AccountScanStatusWriteResult.ConcurrencyConflict();
        }
    }
}
