using System.Text.Json;
using System.Text.Json.Serialization;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>Serialises hosted sign-in blobs.</summary>
internal static class HostedSignInRecordJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    internal static string Serialise(HostedSignInRecord record) => JsonSerializer.Serialize(record, Options);

    internal static HostedSignInRecord? Deserialise(string json) =>
        JsonSerializer.Deserialize<HostedSignInRecord>(json, Options);
}
