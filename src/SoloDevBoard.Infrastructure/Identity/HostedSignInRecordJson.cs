using System.Text.Json;
using System.Text.Json.Serialization;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>Serialises hosted sign-in blobs (schema 2 writes; schema 1 read without worker copy).</summary>
internal static class HostedSignInRecordJson
{
    private const int CurrentSchemaVersion = 2;

    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    internal static string Serialise(HostedSignInRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        record.SchemaVersion = CurrentSchemaVersion;
        var payload = new HostedSignInRecordWriteDto
        {
            SchemaVersion = CurrentSchemaVersion,
            OperatorLogin = record.OperatorLogin,
            CompletedAt = record.CompletedAt,
            OrganisationLogins = record.OrganisationLogins.ToList(),
            WorkerRefreshToken = record.WorkerRefreshToken,
            WorkerRefreshTokenExpiresAt = record.WorkerRefreshTokenExpiresAt,
            CredentialUnusable = record.CredentialUnusable,
        };

        return JsonSerializer.Serialize(payload, Options);
    }

    internal static HostedSignInRecord? Deserialise(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("schemaVersion", out var schemaProp))
        {
            return null;
        }

        var schemaVersion = schemaProp.GetInt32();
        var record = new HostedSignInRecord
        {
            SchemaVersion = schemaVersion,
            OperatorLogin = root.GetProperty("operatorLogin").GetString() ?? string.Empty,
            CompletedAt = root.GetProperty("completedAt").GetDateTimeOffset(),
            OrganisationLogins = ReadOrganisationLogins(root),
            CredentialUnusable = root.TryGetProperty("credentialUnusable", out var cu) && cu.GetBoolean(),
        };

        if (schemaVersion >= CurrentSchemaVersion)
        {
            record.WorkerRefreshToken = root.TryGetProperty("workerRefreshToken", out var wrt)
                ? wrt.GetString()
                : null;
            record.WorkerRefreshTokenExpiresAt = root.TryGetProperty("workerRefreshTokenExpiresAt", out var exp)
                && exp.ValueKind != JsonValueKind.Null
                ? exp.GetDateTimeOffset()
                : null;
        }
        else if (root.TryGetProperty("refreshToken", out var legacy) && legacy.ValueKind == JsonValueKind.String)
        {
            // Schema 1 stored the browser grant; do not copy or exchange it as the worker token.
            record.LegacyRefreshToken = legacy.GetString();
        }

        return record;
    }

    private static IReadOnlyList<string> ReadOrganisationLogins(JsonElement root)
    {
        if (!root.TryGetProperty("organisationLogins", out var orgs) || orgs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return orgs.EnumerateArray()
            .Select(static e => e.GetString())
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Select(static s => s!)
            .ToArray();
    }

    private sealed class HostedSignInRecordWriteDto
    {
        public int SchemaVersion { get; set; }

        public string OperatorLogin { get; set; } = string.Empty;

        public DateTimeOffset CompletedAt { get; set; }

        public List<string> OrganisationLogins { get; set; } = [];

        public string? WorkerRefreshToken { get; set; }

        public DateTimeOffset? WorkerRefreshTokenExpiresAt { get; set; }

        public bool CredentialUnusable { get; set; }
    }
}
