using System.Text.Json;
using System.Text.Json.Serialization;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Serialises account scan status blobs.</summary>
internal static class AccountScanStatusJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false,
    };

    internal static string Serialise(AccountScanStatusDocument document) => JsonSerializer.Serialize(document, Options);

    internal static AccountScanStatusDocument? Deserialise(string json) =>
        JsonSerializer.Deserialize<AccountScanStatusDocument>(json, Options);
}
