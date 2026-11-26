namespace SoloDevBoard.Application.Authentication;

/// <summary>Durable hosted sign-in facts for scheduled collection when the operator is signed out.</summary>
public sealed class HostedSignInRecord
{
    /// <summary>Document schema version (current writes use <c>2</c>).</summary>
    public int SchemaVersion { get; set; } = 2;

    /// <summary>Sanitised operator login.</summary>
    public string OperatorLogin { get; set; } = string.Empty;

    /// <summary>When the latest hosted sign-in completed.</summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>Organisation logins saved from that sign-in.</summary>
    public IReadOnlyList<string> OrganisationLogins { get; set; } = [];

    /// <summary>Worker refresh token for overnight collection (secret; never logged or queued).</summary>
    public string? WorkerRefreshToken { get; set; }

    /// <summary>Worker refresh token expiry when GitHub provided one.</summary>
    public DateTimeOffset? WorkerRefreshTokenExpiresAt { get; set; }

    /// <summary>Schema 1 interactive grant; read-only legacy, never exchanged for worker use.</summary>
    public string? LegacyRefreshToken { get; set; }

    /// <summary>True after a daily pass failed with sign-in or token and no newer worker token is stored.</summary>
    public bool CredentialUnusable { get; set; }

    /// <summary>Worker token present for overnight collection.</summary>
    public bool HasWorkerRefreshToken => !string.IsNullOrWhiteSpace(WorkerRefreshToken);

    /// <summary>Refresh token the worker may exchange; schema 1 legacy values are never used.</summary>
    public string? WorkerRefreshTokenForExchange =>
        SchemaVersion >= 2 && !string.IsNullOrWhiteSpace(WorkerRefreshToken) ? WorkerRefreshToken : null;
}
