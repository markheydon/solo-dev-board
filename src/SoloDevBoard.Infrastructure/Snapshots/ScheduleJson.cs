using System.Text.Json;
using System.Text.Json.Serialization;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>JSON helpers for schedule blobs.</summary>
internal static class ScheduleJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    internal static string SerialiseSignInRecord(OperatorSignInRecord record)
        => JsonSerializer.Serialize(record, Options);

    internal static OperatorSignInRecord? DeserialiseSignInRecord(string json)
        => JsonSerializer.Deserialize<OperatorSignInRecord>(json, Options);

    internal static string SerialiseOccasionQueue(DailyOccasionQueue queue)
        => JsonSerializer.Serialize(queue, Options);

    internal static DailyOccasionQueue? DeserialiseOccasionQueue(string json)
        => JsonSerializer.Deserialize<DailyOccasionQueue>(json, Options);
}
