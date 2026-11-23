namespace SoloDevBoard.Application.Authentication;

/// <summary>Durable hosted sign-in facts for scheduled collection when the operator is signed out.</summary>
public sealed class HostedSignInRecord
{
    /// <summary>Document schema version.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Sanitised operator login.</summary>
    public string OperatorLogin { get; set; } = string.Empty;

    /// <summary>When the latest hosted sign-in completed.</summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>Organisation logins saved from that sign-in.</summary>
    public IReadOnlyList<string> OrganisationLogins { get; set; } = [];

    /// <summary>GitHub refresh token (secret; never logged or queued).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Refresh token expiry when GitHub provided one.</summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>True after a daily pass failed with sign-in or token.</summary>
    public bool CredentialUnusable { get; set; }
}
