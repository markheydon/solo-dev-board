using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests.Support;

internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
}

internal sealed class RecordingDailyQueuePublisher : IDailySnapshotQueuePublisher
{
    public List<DailySnapshotQueueMessage> Messages { get; } = [];

    public Task EnqueueAsync(DailySnapshotQueueMessage message, CancellationToken cancellationToken = default)
    {
        Messages.Add(new DailySnapshotQueueMessage
        {
            OccasionDate = message.OccasionDate,
            OperatorLogin = message.OperatorLogin,
            Kind = message.Kind,
            NotBefore = message.NotBefore,
        });
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryOccasionMarkerStore : ICollectionOccasionMarkerStore
{
    private readonly Dictionary<string, OccasionMarker> _markers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _etags = new(StringComparer.Ordinal);
    private readonly HashSet<string> _unreadable = new(StringComparer.Ordinal);
    private int _etagCounter;

    public void MarkUnreadable(string occasionDate) => _unreadable.Add(occasionDate);

    public Task<OccasionMarkerReadResult> GetAsync(string occasionDate, CancellationToken cancellationToken = default)
    {
        if (_unreadable.Contains(occasionDate))
        {
            return Task.FromResult<OccasionMarkerReadResult>(new OccasionMarkerReadResult.Unreadable());
        }

        return Task.FromResult<OccasionMarkerReadResult>(
            _markers.TryGetValue(occasionDate, out var marker)
                ? new OccasionMarkerReadResult.Found(marker, _etags[occasionDate])
                : new OccasionMarkerReadResult.NotFound());
    }

    public Task<OccasionMarkerWriteResult> TryCreateAsync(OccasionMarker marker, CancellationToken cancellationToken = default)
    {
        if (_markers.ContainsKey(marker.OccasionDate))
        {
            return Task.FromResult<OccasionMarkerWriteResult>(new OccasionMarkerWriteResult.AlreadyExists());
        }

        _markers[marker.OccasionDate] = new OccasionMarker
        {
            OccasionDate = marker.OccasionDate,
            FilledAt = marker.FilledAt,
            Operators = [.. marker.Operators],
        };
        _etags[marker.OccasionDate] = $"etag-{_etagCounter++}";
        return Task.FromResult<OccasionMarkerWriteResult>(new OccasionMarkerWriteResult.Created());
    }

    public Task<OccasionMarkerWriteResult> TryReplaceAsync(
        OccasionMarker marker,
        string expectedEtag,
        CancellationToken cancellationToken = default)
    {
        if (!_markers.TryGetValue(marker.OccasionDate, out _)
            || !_etags.TryGetValue(marker.OccasionDate, out var etag)
            || !string.Equals(etag, expectedEtag, StringComparison.Ordinal))
        {
            return Task.FromResult<OccasionMarkerWriteResult>(new OccasionMarkerWriteResult.ConcurrencyConflict());
        }

        _markers[marker.OccasionDate] = new OccasionMarker
        {
            OccasionDate = marker.OccasionDate,
            FilledAt = marker.FilledAt,
            Operators = [.. marker.Operators],
        };
        _etags[marker.OccasionDate] = $"etag-{_etagCounter++}";
        return Task.FromResult<OccasionMarkerWriteResult>(new OccasionMarkerWriteResult.Replaced());
    }
}

internal sealed class InMemoryHostedSignInRecordStore : IHostedSignInRecordStore
{
    private readonly List<HostedSignInRecord> _records = [];

    public void Seed(params HostedSignInRecord[] records) => _records.AddRange(records);

    public Task<IReadOnlyList<HostedSignInRecord>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<HostedSignInRecord>>(_records.ToList());

    public Task<HostedSignInRecordReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
    {
        var match = _records.FirstOrDefault(r => string.Equals(r.OperatorLogin, operatorLogin, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<HostedSignInRecordReadResult>(
            match is null
                ? new HostedSignInRecordReadResult.NotFound()
                : new HostedSignInRecordReadResult.Found(match, "etag"));
    }

    public Task<HostedSignInRecordWriteResult> UpsertAsync(
        HostedSignInRecord record,
        string? expectedEtag,
        CancellationToken cancellationToken = default)
        => Task.FromResult<HostedSignInRecordWriteResult>(new HostedSignInRecordWriteResult.Succeeded("etag"));
}

internal sealed class AlwaysAvailableRunnerLease : IDailyCollectionRunnerLease
{
    public Task<IDailyCollectionRunnerLeaseHandle?> TryAcquireAsync(TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.FromResult<IDailyCollectionRunnerLeaseHandle?>(new NoOpRunnerLeaseHandle());
}

internal sealed class NeverAvailableRunnerLease : IDailyCollectionRunnerLease
{
    public Task<IDailyCollectionRunnerLeaseHandle?> TryAcquireAsync(TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.FromResult<IDailyCollectionRunnerLeaseHandle?>(null);
}

internal sealed class NoOpRunnerLeaseHandle : IDailyCollectionRunnerLeaseHandle
{
    public Task TryRenewAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class RecordingRunnerLease : IDailyCollectionRunnerLease
{
    public int RenewCount { get; private set; }

    public Task<IDailyCollectionRunnerLeaseHandle?> TryAcquireAsync(TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.FromResult<IDailyCollectionRunnerLeaseHandle?>(new RecordingRunnerLeaseHandle(this));

    internal void RecordRenew() => RenewCount++;
}

internal sealed class RecordingRunnerLeaseHandle(RecordingRunnerLease lease) : IDailyCollectionRunnerLeaseHandle
{
    public Task TryRenewAsync(CancellationToken cancellationToken = default)
    {
        lease.RecordRenew();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal static class DailyOccasionServiceTestFactory
{
    public static DailyOccasionService Create(
        FixedTimeProvider time,
        InMemoryOperatorSnapshotStore snapshots,
        InMemoryHostedSignInRecordStore signIns,
        RecordingDailyQueuePublisher queue,
        InMemoryOccasionMarkerStore markers,
        IDailyCollectionRunnerLease? runner = null,
        bool hostedScheduleEnabled = true,
        HostedAdmissionControlOptions? admission = null)
    {
        var environment = new TestScheduledCollectionEnvironment(hostedScheduleEnabled);
        return new DailyOccasionService(
            environment,
            time,
            markers,
            runner ?? new AlwaysAvailableRunnerLease(),
            queue,
            signIns,
            snapshots,
            new HostedCollectionEligibilityEvaluator(Options.Create(admission ?? new HostedAdmissionControlOptions { Enabled = false })),
            NullLogger<DailyOccasionService>.Instance);
    }
}

internal sealed class TestScheduledCollectionEnvironment(bool enabled) : IScheduledCollectionEnvironment
{
    public bool IsHostedScheduleEnabled => enabled;
}
