using Azure;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Blob-based <see cref="IOperatorSnapshotStore"/> using conditional uploads.</summary>
public sealed class BlobOperatorSnapshotStore : IOperatorSnapshotStore
{
    private readonly IOperatorSnapshotBlobContainer _container;
    private readonly IOperatorSnapshotLiveTaskRegistry _liveTasks;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BlobOperatorSnapshotStore> _logger;

    /// <summary>Initialises the blob store.</summary>
    public BlobOperatorSnapshotStore(
        IOperatorSnapshotBlobContainer container,
        IOperatorSnapshotLiveTaskRegistry liveTasks,
        TimeProvider timeProvider,
        ILogger<BlobOperatorSnapshotStore> logger)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
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

        var blob = _container.GetBlobClient($"{sanitised}.json");
        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return new OperatorSnapshotGetResult.NotFound();
        }

        var download = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
        var document = OperatorSnapshotJson.Deserialise(download.Content);
        if (document is null || document.SchemaVersion != OperatorSnapshotDocumentConstants.CurrentSchemaVersion)
        {
            return new OperatorSnapshotGetResult.UnsupportedSchema();
        }

        var etag = download.Etag;
        document = await MaybeRewriteInterruptedAsync(document, sanitised, etag, cancellationToken).ConfigureAwait(false);
        if (document.Run.Status == OperatorSnapshotRunStatus.Interrupted)
        {
            etag = await blob.GetEtagAsync(cancellationToken).ConfigureAwait(false);
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

        var blob = _container.GetBlobClient($"{sanitised}.json");
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
        string? etag = null;
        if (exists)
        {
            etag = await blob.GetEtagAsync(cancellationToken).ConfigureAwait(false);
            var existingJson = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
            var existing = OperatorSnapshotJson.Deserialise(existingJson.Content);
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
            var conditions = exists
                ? new OperatorSnapshotBlobUploadConditions(
                    expectedToken.BlobEtag ?? etag,
                    IfNoneMatchAll: false)
                : new OperatorSnapshotBlobUploadConditions(null, IfNoneMatchAll: true);

            await blob.UploadAsync(payload, conditions, cancellationToken).ConfigureAwait(false);
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

        var isDailyPassExecuting = document.Trigger == OperatorSnapshotCollectionTrigger.Daily
            && (document.PassExecuting
                || (document.PassLeaseUntil is { } leaseUntil && leaseUntil > _timeProvider.GetUtcNow()));

        if (isDailyPassExecuting)
        {
            if (document.PassExecuting
                && document.PassLeaseUntil is { } expired
                && expired <= _timeProvider.GetUtcNow())
            {
                return await RewriteDailyPassInterruptedAsync(document, sanitisedLogin, etag, cancellationToken)
                    .ConfigureAwait(false);
            }

            return document;
        }

        if (document.Trigger == OperatorSnapshotCollectionTrigger.Daily && !document.PassExecuting)
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
            Trigger = document.Trigger,
            QueuedOccasion = document.QueuedOccasion,
            PassExecuting = false,
            PassLeaseUntil = null,
            AttemptsUsed = document.AttemptsUsed,
            RetainedRun = document.RetainedRun,
            PendingRetryNotBeforeUtc = document.PendingRetryNotBeforeUtc,
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

    private async Task<OperatorSnapshotDocument> RewriteDailyPassInterruptedAsync(
        OperatorSnapshotDocument document,
        string sanitisedLogin,
        string etag,
        CancellationToken cancellationToken)
    {
        var interrupted = new OperatorSnapshotDocument
        {
            SchemaVersion = document.SchemaVersion,
            OperatorLogin = document.OperatorLogin,
            Revision = document.Revision,
            LastSuccessfulCollectionAt = document.LastSuccessfulCollectionAt,
            Repositories = document.Repositories,
            LaterFacts = document.LaterFacts,
            Trigger = document.Trigger,
            QueuedOccasion = null,
            PassExecuting = false,
            PassLeaseUntil = null,
            AttemptsUsed = document.AttemptsUsed,
            RetainedRun = null,
            PendingRetryNotBeforeUtc = null,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Interrupted,
                StartedAt = document.Run.StartedAt,
                FinishedAt = _timeProvider.GetUtcNow(),
                RepositoriesRecorded = document.Run.RepositoriesRecorded,
                Reason = OperatorSnapshotRunReason.ScheduledCollectionStopped,
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
