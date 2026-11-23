using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Blob-based <see cref="IOperatorSnapshotStore"/> using conditional uploads.</summary>
public sealed class BlobOperatorSnapshotStore : IOperatorSnapshotStore
{
    private readonly BlobContainerClient _containerClient;
    private readonly IOperatorSnapshotLiveTaskRegistry _liveTasks;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BlobOperatorSnapshotStore> _logger;

    /// <summary>Initialises the blob store.</summary>
    public BlobOperatorSnapshotStore(
        BlobContainerClient containerClient,
        IOperatorSnapshotLiveTaskRegistry liveTasks,
        TimeProvider timeProvider,
        ILogger<BlobOperatorSnapshotStore> logger)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        _liveTasks = liveTasks ?? throw new ArgumentNullException(nameof(liveTasks));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<OperatorSnapshotGetResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin);
        if (sanitised is null)
        {
            return new OperatorSnapshotGetResult.UnsafeKey();
        }

        var blob = _containerClient.GetBlobClient($"{sanitised}.json");
        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return new OperatorSnapshotGetResult.NotFound();
        }

        var download = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        var document = OperatorSnapshotJson.Deserialise(download.Value.Content.ToString());
        if (document is null || document.SchemaVersion != OperatorSnapshotDocumentConstants.CurrentSchemaVersion)
        {
            return new OperatorSnapshotGetResult.UnsupportedSchema();
        }

        var etag = download.Value.Details.ETag.ToString();
        document = await MaybeRewriteInterruptedAsync(document, sanitised, etag, cancellationToken).ConfigureAwait(false);
        if (document.Run.Status == OperatorSnapshotRunStatus.Interrupted)
        {
            var props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            etag = props.Value.ETag.ToString();
        }

        var token = new OperatorSnapshotConcurrencyToken(document.Revision, etag);
        return new OperatorSnapshotGetResult.Found(document, token);
    }

    /// <inheritdoc/>
    public async Task<OperatorSnapshotSaveResult> SaveAsync(
        string operatorLogin,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken expectedToken,
        CancellationToken cancellationToken = default)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin);
        if (sanitised is null)
        {
            return new OperatorSnapshotSaveResult.UnsafeKey();
        }

        var blob = _containerClient.GetBlobClient($"{sanitised}.json");
        var exists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);

        if (exists && expectedToken.Revision == 0)
        {
            return new OperatorSnapshotSaveResult.ConcurrencyConflict();
        }

        if (!exists && expectedToken.Revision != 0 && expectedToken.BlobEtag is null)
        {
            // Creating with non-zero revision without etag is invalid.
        }

        long currentRevision = 0;
        ETag? etag = null;
        if (exists)
        {
            var props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            etag = props.Value.ETag;
            var existingJson = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            var existing = OperatorSnapshotJson.Deserialise(existingJson.Value.Content.ToString());
            if (existing is null)
            {
                return new OperatorSnapshotSaveResult.ConcurrencyConflict();
            }

            currentRevision = existing.Revision;
            if (currentRevision != expectedToken.Revision)
            {
                return new OperatorSnapshotSaveResult.ConcurrencyConflict();
            }
        }

        document.OperatorLogin = sanitised;
        document.Revision = currentRevision + 1;
        var payload = OperatorSnapshotJson.Serialise(document);

        try
        {
            var conditions = new BlobRequestConditions();
            if (exists)
            {
                conditions.IfMatch = expectedToken.BlobEtag is not null ? new ETag(expectedToken.BlobEtag) : etag;
            }
            else
            {
                conditions.IfNoneMatch = ETag.All;
            }

            await blob.UploadAsync(
                    BinaryData.FromString(payload),
                    new BlobUploadOptions { Conditions = conditions },
                    cancellationToken)
                .ConfigureAwait(false);
            return new OperatorSnapshotSaveResult.Succeeded();
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            return new OperatorSnapshotSaveResult.ConcurrencyConflict();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save operator snapshot blob.");
            return new OperatorSnapshotSaveResult.ConcurrencyConflict();
        }
    }

    private async Task<OperatorSnapshotDocument> MaybeRewriteInterruptedAsync(
        OperatorSnapshotDocument document,
        string sanitisedLogin,
        string etag,
        CancellationToken cancellationToken)
    {
        if (document.Run.Status != OperatorSnapshotRunStatus.InProgress || _liveTasks.HasLiveTask(sanitisedLogin))
        {
            return document;
        }

        var interrupted = new OperatorSnapshotDocument
        {
            SchemaVersion = document.SchemaVersion,
            OperatorLogin = document.OperatorLogin,
            Revision = document.Revision,
            LastSuccessfulCollectionAt = document.LastSuccessfulCollectionAt,
            Repositories = document.Repositories,
            LaterFacts = document.LaterFacts,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Interrupted,
                StartedAt = document.Run.StartedAt,
                FinishedAt = _timeProvider.GetUtcNow(),
                RepositoriesRecorded = document.Run.RepositoriesRecorded,
                Reason = OperatorSnapshotRunReason.SiteStopped,
                Detail = null,
            },
        };

        var save = await SaveAsync(
                sanitisedLogin,
                interrupted,
                new OperatorSnapshotConcurrencyToken(document.Revision, etag),
                cancellationToken)
            .ConfigureAwait(false);
        if (save is OperatorSnapshotSaveResult.Succeeded)
        {
            interrupted.Revision = document.Revision + 1;
            return interrupted;
        }

        return document;
    }
}
