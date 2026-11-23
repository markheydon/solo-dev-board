using System.Text.Json.Serialization;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Occasion queue document for one UTC date.</summary>
public sealed class DailyOccasionQueue
{
    /// <summary>UTC date of 05:00 for this queue.</summary>
    public DateOnly OccasionDate { get; set; }

    /// <summary>When this document was created (UTC).</summary>
    public DateTimeOffset FilledAt { get; set; }

    /// <summary>Ordered queue entries.</summary>
    public List<DailyOccasionQueueEntry> Entries { get; set; } = [];

    /// <summary>Login holding the active daily pass lease, if any.</summary>
    public string? ActiveLogin { get; set; }

    /// <summary>When the active lease expires (UTC).</summary>
    public DateTimeOffset? LeaseUntil { get; set; }

    /// <summary>Concurrency token from the blob store.</summary>
    [JsonIgnore]
    public string? BlobEtag { get; set; }

    /// <summary>Returns whether work remains on this document.</summary>
    public bool HasWork()
        => Entries.Count > 0
           || !string.IsNullOrWhiteSpace(ActiveLogin);

    /// <summary>Finds an entry for the login, if present.</summary>
    public DailyOccasionQueueEntry? FindEntry(string sanitisedLogin)
        => Entries.FirstOrDefault(entry => string.Equals(entry.Login, sanitisedLogin, StringComparison.Ordinal));
}
