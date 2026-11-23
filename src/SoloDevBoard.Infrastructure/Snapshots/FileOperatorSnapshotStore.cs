using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>File-based <see cref="IOperatorSnapshotStore"/> with temp-then-replace writes.</summary>
public sealed class FileOperatorSnapshotStore : IOperatorSnapshotStore
{
    private readonly OperatorSnapshotFileStoreOptions _options;
    private readonly IOperatorSnapshotLiveTaskRegistry _liveTasks;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FileOperatorSnapshotStore> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _pathLocks = new(StringComparer.Ordinal);

    /// <summary>Initialises the file store.</summary>
    public FileOperatorSnapshotStore(
        IOptions<OperatorSnapshotFileStoreOptions> options,
        IOperatorSnapshotLiveTaskRegistry liveTasks,
        TimeProvider timeProvider,
        ILogger<FileOperatorSnapshotStore> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _liveTasks = liveTasks ?? throw new ArgumentNullException(nameof(liveTasks));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<OperatorSnapshotGetResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePath(operatorLogin, out var path, out var sanitised))
        {
            return new OperatorSnapshotGetResult.UnsafeKey();
        }

        if (!File.Exists(path))
        {
            return new OperatorSnapshotGetResult.NotFound();
        }

        OperatorSnapshotDocument document;
        var fileLock = GetLock(path);
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var parsed = OperatorSnapshotJson.Deserialise(json);
            if (parsed is null || parsed.SchemaVersion != OperatorSnapshotDocumentConstants.CurrentSchemaVersion)
            {
                return new OperatorSnapshotGetResult.UnsupportedSchema();
            }

            document = parsed;
        }
        finally
        {
            fileLock.Release();
        }

        document = await MaybeRewriteInterruptedAsync(document, sanitised, cancellationToken).ConfigureAwait(false);
        var token = new OperatorSnapshotConcurrencyToken(document.Revision, null);
        return new OperatorSnapshotGetResult.Found(document, token);
    }

    /// <inheritdoc/>
    public async Task<OperatorSnapshotSaveResult> SaveAsync(
        string operatorLogin,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken expectedToken,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolvePath(operatorLogin, out var path, out var sanitised))
        {
            return new OperatorSnapshotSaveResult.UnsafeKey();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var fileLock = GetLock(path);
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            long currentRevision = 0;
            if (File.Exists(path))
            {
                var existingJson = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                var existing = OperatorSnapshotJson.Deserialise(existingJson);
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
            else if (expectedToken.Revision != 0)
            {
                return new OperatorSnapshotSaveResult.ConcurrencyConflict();
            }

            document.OperatorLogin = sanitised;
            document.Revision = currentRevision + 1;
            var tempPath = path + ".tmp";
            var payload = OperatorSnapshotJson.Serialise(document);
            await File.WriteAllTextAsync(tempPath, payload, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
            return new OperatorSnapshotSaveResult.Succeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save operator snapshot file.");
            return new OperatorSnapshotSaveResult.ConcurrencyConflict();
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task<OperatorSnapshotDocument> MaybeRewriteInterruptedAsync(
        OperatorSnapshotDocument document,
        string sanitisedLogin,
        CancellationToken cancellationToken)
    {
        if (document.Run.Status != OperatorSnapshotRunStatus.InProgress || _liveTasks.HasLiveTask(sanitisedLogin))
        {
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

        var save = await SaveAsync(sanitisedLogin, interrupted, new OperatorSnapshotConcurrencyToken(document.Revision, null), cancellationToken)
            .ConfigureAwait(false);
        if (save is OperatorSnapshotSaveResult.Succeeded)
        {
            interrupted.Revision = document.Revision + 1;
            return interrupted;
        }

        return document;
    }

    private bool TryResolvePath(string operatorLogin, out string path, out string sanitised)
    {
        sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin) ?? string.Empty;
        path = string.Empty;
        if (sanitised.Length == 0)
        {
            return false;
        }

        var directory = Path.GetFullPath(_options.SnapshotDirectory);
        var candidate = Path.GetFullPath(Path.Combine(directory, $"{sanitised}.json"));
        if (!candidate.StartsWith(directory, StringComparison.Ordinal))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    private SemaphoreSlim GetLock(string path) => _pathLocks.GetOrAdd(path, static _ => new SemaphoreSlim(1, 1));
}
