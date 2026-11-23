using System.Text.Json.Serialization;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Hosted sign-in record used for daily eligibility (not a public DTO).</summary>
public sealed class OperatorSignInRecord
{
    /// <summary>Sanitised operator login.</summary>
    public string OperatorLogin { get; set; } = string.Empty;

    /// <summary>When the last completed hosted sign-in finished (UTC).</summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>Organisation logins saved from that sign-in.</summary>
    public List<string> OrganisationLogins { get; set; } = [];

    /// <summary>Data-protection ciphertext for the refresh token.</summary>
    public string ProtectedRefreshToken { get; set; } = string.Empty;

    /// <summary>UTC expiry when GitHub supplied one.</summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>When true, daily collection is held until the next completed sign-in.</summary>
    public bool ScheduleHold { get; set; }

    /// <summary>Concurrency token from the blob store.</summary>
    [JsonIgnore]
    public string? BlobEtag { get; set; }
}
