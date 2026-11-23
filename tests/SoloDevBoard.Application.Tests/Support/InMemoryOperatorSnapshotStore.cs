using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests.Support;

/// <summary>In-memory snapshot store for application-layer tests.</summary>
internal sealed class InMemoryOperatorSnapshotStore : IOperatorSnapshotStore
{
    private readonly Dictionary<string, (OperatorSnapshotDocument Document, long Revision)> _documents = new(StringComparer.Ordinal);

    public void Seed(string operatorLogin, OperatorSnapshotDocument document)
    {
        _documents[operatorLogin] = (Clone(document), document.Revision);
    }

    public Task<OperatorSnapshotGetResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
    {
        if (!_documents.TryGetValue(operatorLogin, out var entry))
        {
            return Task.FromResult<OperatorSnapshotGetResult>(new OperatorSnapshotGetResult.NotFound());
        }

        var token = new OperatorSnapshotConcurrencyToken(entry.Revision, null);
        return Task.FromResult<OperatorSnapshotGetResult>(new OperatorSnapshotGetResult.Found(Clone(entry.Document), token));
    }

    public Task<OperatorSnapshotSaveResult> SaveAsync(
        string operatorLogin,
        OperatorSnapshotDocument document,
        OperatorSnapshotConcurrencyToken expectedToken,
        CancellationToken cancellationToken = default)
    {
        if (_documents.TryGetValue(operatorLogin, out var existing)
            && expectedToken != OperatorSnapshotConcurrencyToken.None
            && existing.Revision != expectedToken.Revision)
        {
            return Task.FromResult<OperatorSnapshotSaveResult>(new OperatorSnapshotSaveResult.ConcurrencyConflict());
        }

        _documents[operatorLogin] = (Clone(document), document.Revision);
        return Task.FromResult<OperatorSnapshotSaveResult>(new OperatorSnapshotSaveResult.Succeeded());
    }

    public OperatorSnapshotDocument? GetDocument(string operatorLogin)
        => _documents.TryGetValue(operatorLogin, out var entry) ? Clone(entry.Document) : null;

    private static OperatorSnapshotDocument Clone(OperatorSnapshotDocument document)
        => new()
        {
            SchemaVersion = document.SchemaVersion,
            OperatorLogin = document.OperatorLogin,
            Revision = document.Revision,
            LastSuccessfulCollectionAt = document.LastSuccessfulCollectionAt,
            Repositories = document.Repositories.ToList(),
            LaterFacts = new Dictionary<string, object?>(document.LaterFacts),
            Run = new OperatorSnapshotRun
            {
                Status = document.Run.Status,
                StartedAt = document.Run.StartedAt,
                FinishedAt = document.Run.FinishedAt,
                RepositoriesRecorded = document.Run.RepositoriesRecorded,
                Reason = document.Run.Reason,
                Detail = document.Run.Detail,
            },
        };
}
