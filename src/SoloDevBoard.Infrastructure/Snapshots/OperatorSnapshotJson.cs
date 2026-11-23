using System.Text.Json;
using System.Text.Json.Serialization;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Serialises operator snapshot documents.</summary>
internal static class OperatorSnapshotJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    internal static string Serialise(OperatorSnapshotDocument document)
        => JsonSerializer.Serialize(document, Options);

    internal static OperatorSnapshotDocument? Deserialise(string json)
    {
        var document = JsonSerializer.Deserialize<OperatorSnapshotDocument>(json, Options);
        if (document is not null && document.Run.StartedBy is null)
        {
            document.Run.StartedBy = OperatorSnapshotRunStartedBy.Operator;
        }

        return document;
    }
}
