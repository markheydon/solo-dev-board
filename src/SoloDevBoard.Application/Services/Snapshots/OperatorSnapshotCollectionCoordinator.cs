using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.Repositories;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Owns in-flight collection tasks and live progress per sanitised login.</summary>
public sealed class OperatorSnapshotCollectionCoordinator : IOperatorSnapshotLiveTaskRegistry
{
    private readonly ISnapshotCollectionCredentialScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly OperatorSnapshotApplicationStopping _applicationStopping;
    private readonly ILogger<OperatorSnapshotCollectionCoordinator> _logger;
    private readonly ConcurrentDictionary<string, LiveRun> _liveRuns = new(StringComparer.Ordinal);

    /// <summary>Initialises the coordinator.</summary>
    public OperatorSnapshotCollectionCoordinator(
        ISnapshotCollectionCredentialScopeFactory scopeFactory,
        TimeProvider timeProvider,
        OperatorSnapshotApplicationStopping applicationStopping,
        ILogger<OperatorSnapshotCollectionCoordinator> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _applicationStopping = applicationStopping ?? throw new ArgumentNullException(nameof(applicationStopping));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool HasLiveTask(string sanitisedLogin) => _liveRuns.ContainsKey(sanitisedLogin);

    /// <inheritdoc/>
    public bool TryGetLiveProgress(string sanitisedLogin, out DateTimeOffset startedAt, out int repositoriesRecorded)
    {
        if (_liveRuns.TryGetValue(sanitisedLogin, out var run))
        {
            startedAt = run.StartedAt;
            repositoriesRecorded = run.RepositoriesRecorded;
            return true;
        }

        startedAt = default;
        repositoriesRecorded = 0;
        return false;
    }

    /// <summary>Starts a walk when none is live for the sanitised login.</summary>
    public bool TryBegin(
        string sanitisedLogin,
        string ownerLogin,
        string accessToken,
        OperatorSnapshotConcurrencyToken startToken,
        Func<string, CancellationToken, Task<OperatorSnapshotGetResult>> readDocumentAsync,
        Func<string, OperatorSnapshotDocument, OperatorSnapshotConcurrencyToken, CancellationToken, Task<bool>> persistTerminalAsync)
    {
        if (!_liveRuns.TryAdd(sanitisedLogin, new LiveRun(_timeProvider.GetUtcNow(), 0)))
        {
            return false;
        }

        _ = RunWalkAsync(
            sanitisedLogin,
            ownerLogin,
            accessToken,
            startToken,
            readDocumentAsync,
            persistTerminalAsync);
        return true;
    }

    private async Task RunWalkAsync(
        string sanitisedLogin,
        string ownerLogin,
        string accessToken,
        OperatorSnapshotConcurrencyToken startToken,
        Func<string, CancellationToken, Task<OperatorSnapshotGetResult>> readDocumentAsync,
        Func<string, OperatorSnapshotDocument, OperatorSnapshotConcurrencyToken, CancellationToken, Task<bool>> persistTerminalAsync)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping.Token);
        var cancellationToken = linkedCts.Token;
        OperatorSnapshotConcurrencyToken token = startToken;

        try
        {
            using var scope = _scopeFactory.CreateScope(ownerLogin, accessToken);
            var github = scope.ServiceProvider.GetRequiredService<IGitHubService>();

            IReadOnlyList<Repository> repositories;
            try
            {
                var liveProgress = new Progress<int>(count => UpdateRecordedCount(sanitisedLogin, count));
                repositories = await github
                    .GetRepositoriesAsync(cancellationToken, forceReload: true, collectionProgress: liveProgress)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await WriteFailureAsync(
                        sanitisedLogin,
                        readDocumentAsync,
                        persistTerminalAsync,
                        cancellationToken,
                        ex)
                    .ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await WriteInterruptedAsync(sanitisedLogin, readDocumentAsync, persistTerminalAsync, cancellationToken).ConfigureAwait(false);
                return;
            }

            var entries = BuildEntries(repositories);
            UpdateRecordedCount(sanitisedLogin, entries.Count);

            var successAt = _timeProvider.GetUtcNow();
            var successDocument = new OperatorSnapshotDocument
            {
                SchemaVersion = OperatorSnapshotDocumentConstants.CurrentSchemaVersion,
                OperatorLogin = sanitisedLogin,
                Run = new OperatorSnapshotRun
                {
                    Status = OperatorSnapshotRunStatus.Succeeded,
                    StartedAt = GetStartedAt(sanitisedLogin),
                    FinishedAt = successAt,
                    RepositoriesRecorded = entries.Count,
                    Reason = null,
                    Detail = null,
                },
                LastSuccessfulCollectionAt = successAt,
                Repositories = entries,
            };

            await ApplyWithRetryAsync(
                    sanitisedLogin,
                    readDocumentAsync,
                    persistTerminalAsync,
                    cancellationToken,
                    (doc, currentToken) =>
                    {
                        successDocument.Revision = doc.Revision + 1;
                        successDocument.OperatorLogin = sanitisedLogin;
                        successDocument.LaterFacts = doc.LaterFacts;
                        successDocument.Run.StartedAt = doc.Run.Status == OperatorSnapshotRunStatus.InProgress
                            ? doc.Run.StartedAt
                            : GetStartedAt(sanitisedLogin);
                        successDocument.Run.StartedBy = doc.Run.StartedBy;
                        return (successDocument, currentToken);
                    })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operator snapshot walk failed unexpectedly for {Login}.", sanitisedLogin);
            await WriteFailureAsync(sanitisedLogin, readDocumentAsync, persistTerminalAsync, CancellationToken.None, ex).ConfigureAwait(false);
        }
        finally
        {
            _liveRuns.TryRemove(sanitisedLogin, out _);
            linkedCts.Dispose();
        }
    }

    private async Task WriteFailureAsync(
        string sanitisedLogin,
        Func<string, CancellationToken, Task<OperatorSnapshotGetResult>> readDocumentAsync,
        Func<string, OperatorSnapshotDocument, OperatorSnapshotConcurrencyToken, CancellationToken, Task<bool>> persistTerminalAsync,
        CancellationToken cancellationToken,
        Exception exception)
    {
        var (reason, detail) = OperatorSnapshotFailureMapper.Map(exception);
        var finishedAt = _timeProvider.GetUtcNow();

        await ApplyWithRetryAsync(
                sanitisedLogin,
                readDocumentAsync,
                persistTerminalAsync,
                cancellationToken,
                (doc, currentToken) =>
                {
                    doc.Run = new OperatorSnapshotRun
                    {
                        Status = OperatorSnapshotRunStatus.Failed,
                        StartedAt = doc.Run.StartedAt,
                        FinishedAt = finishedAt,
                        RepositoriesRecorded = doc.Run.RepositoriesRecorded,
                        Reason = reason,
                        Detail = detail,
                        StartedBy = doc.Run.StartedBy,
                    };
                    doc.Revision++;
                    return (doc, currentToken);
                })
            .ConfigureAwait(false);
    }

    private async Task WriteInterruptedAsync(
        string sanitisedLogin,
        Func<string, CancellationToken, Task<OperatorSnapshotGetResult>> readDocumentAsync,
        Func<string, OperatorSnapshotDocument, OperatorSnapshotConcurrencyToken, CancellationToken, Task<bool>> persistTerminalAsync,
        CancellationToken cancellationToken)
    {
        var finishedAt = _timeProvider.GetUtcNow();

        await ApplyWithRetryAsync(
                sanitisedLogin,
                readDocumentAsync,
                persistTerminalAsync,
                cancellationToken,
                (doc, currentToken) =>
                {
                    doc.Run = new OperatorSnapshotRun
                    {
                        Status = OperatorSnapshotRunStatus.Interrupted,
                        StartedAt = doc.Run.StartedAt,
                        FinishedAt = finishedAt,
                        RepositoriesRecorded = doc.Run.RepositoriesRecorded,
                        Reason = OperatorSnapshotRunReason.SiteStopped,
                        Detail = null,
                        StartedBy = doc.Run.StartedBy,
                    };
                    doc.Revision++;
                    return (doc, currentToken);
                })
            .ConfigureAwait(false);
    }

    private static List<OperatorSnapshotRepositoryEntry> BuildEntries(IReadOnlyList<Repository> repositories)
    {
        var seen = new HashSet<long>();
        var entries = new List<OperatorSnapshotRepositoryEntry>(repositories.Count);

        foreach (var repository in repositories)
        {
            var githubId = repository.GitHubId != 0 ? repository.GitHubId : repository.Id;
            if (!seen.Add(githubId))
            {
                continue;
            }

            var owner = string.IsNullOrWhiteSpace(repository.OwnerLogin)
                ? ExtractOwner(repository.FullName)
                : repository.OwnerLogin;

            entries.Add(new OperatorSnapshotRepositoryEntry
            {
                GitHubId = githubId,
                Owner = owner,
                Name = repository.Name,
                Visibility = repository.IsPrivate ? "private" : "public",
                Archived = repository.IsArchived,
                Fork = repository.IsFork,
                HtmlUrl = repository.Url,
            });
        }

        return entries;
    }

    private static string ExtractOwner(string fullName)
    {
        var slash = fullName.IndexOf('/', StringComparison.Ordinal);
        return slash > 0 ? fullName[..slash] : string.Empty;
    }

    private void UpdateRecordedCount(string sanitisedLogin, int count)
    {
        if (_liveRuns.TryGetValue(sanitisedLogin, out var run))
        {
            run.RepositoriesRecorded = count;
        }
    }

    private DateTimeOffset GetStartedAt(string sanitisedLogin)
        => _liveRuns.TryGetValue(sanitisedLogin, out var run) ? run.StartedAt : _timeProvider.GetUtcNow();

    private static async Task ApplyWithRetryAsync(
        string sanitisedLogin,
        Func<string, CancellationToken, Task<OperatorSnapshotGetResult>> readDocumentAsync,
        Func<string, OperatorSnapshotDocument, OperatorSnapshotConcurrencyToken, CancellationToken, Task<bool>> persistTerminalAsync,
        CancellationToken cancellationToken,
        Func<OperatorSnapshotDocument, OperatorSnapshotConcurrencyToken, (OperatorSnapshotDocument Document, OperatorSnapshotConcurrencyToken Token)> mutate)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var read = await readDocumentAsync(sanitisedLogin, cancellationToken).ConfigureAwait(false);
            if (read is not OperatorSnapshotGetResult.Found found)
            {
                return;
            }

            var (document, token) = mutate(found.Document, found.Token);
            if (await persistTerminalAsync(sanitisedLogin, document, token, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    private sealed class LiveRun(DateTimeOffset startedAt, int repositoriesRecorded)
    {
        public DateTimeOffset StartedAt { get; } = startedAt;

        public int RepositoriesRecorded { get; set; } = repositoriesRecorded;
    }
}
