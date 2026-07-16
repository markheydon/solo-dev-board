using System.Text.Json;

namespace SoloDevBoard.Infrastructure.Diagnostics;

/// <summary>Writes NDJSON debug entries for agent-assisted diagnostics. Remove after debugging.</summary>
internal static class AgentDebugLog
{
    private const string LogPath = @"D:\repos\markheydon\solo-dev-board\debug-df5609.log";
    private const string SessionId = "df5609";

    internal static void Write(string location, string message, object data, string hypothesisId, string runId = "pre-fix")
    {
        try
        {
            var entry = new
            {
                sessionId = SessionId,
                id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}"[..24],
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                location,
                message,
                data,
                runId,
                hypothesisId,
            };

            var line = JsonSerializer.Serialize(entry);
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
            // Debug logging must not affect application behaviour.
        }
    }
}
