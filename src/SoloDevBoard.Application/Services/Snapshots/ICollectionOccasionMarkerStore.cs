namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Occasion marker blobs under <c>collection-schedule/occasions/</c>.</summary>
public interface ICollectionOccasionMarkerStore
{
    /// <summary>Returns the marker for a UTC date when present.</summary>
    Task<OccasionMarkerReadResult> GetAsync(string occasionDate, CancellationToken cancellationToken = default);

    /// <summary>Creates the marker when absent.</summary>
    Task<OccasionMarkerWriteResult> TryCreateAsync(OccasionMarker marker, CancellationToken cancellationToken = default);
}

/// <summary>Occasion marker document.</summary>
public sealed class OccasionMarker
{
    /// <summary>UTC date (<c>yyyy-MM-dd</c>).</summary>
    public string OccasionDate { get; set; } = string.Empty;

    /// <summary>When the fill completed.</summary>
    public DateTimeOffset FilledAt { get; set; }

    /// <summary>Logins enqueued once for this occasion.</summary>
    public List<string> Operators { get; set; } = [];
}

/// <summary>Marker read outcomes.</summary>
public abstract record OccasionMarkerReadResult
{
    /// <summary>Marker exists.</summary>
    public sealed record Found(OccasionMarker Marker) : OccasionMarkerReadResult;

    /// <summary>Marker absent.</summary>
    public sealed record NotFound : OccasionMarkerReadResult;

    /// <summary>Marker blob exists but could not be read.</summary>
    public sealed record Unreadable : OccasionMarkerReadResult;
}

/// <summary>Marker create outcomes.</summary>
public abstract record OccasionMarkerWriteResult
{
    /// <summary>Created this marker.</summary>
    public sealed record Created : OccasionMarkerWriteResult;

    /// <summary>Another copy created it first.</summary>
    public sealed record AlreadyExists : OccasionMarkerWriteResult;
}
