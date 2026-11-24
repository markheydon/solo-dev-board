using System.Text.Json;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class DailySnapshotQueueMessageJsonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Serialise_DailySnapshotQueueMessage_ExcludesRefreshToken()
    {
        var message = new DailySnapshotQueueMessage
        {
            OccasionDate = "2026-11-23",
            OperatorLogin = "octocat",
            Kind = "initial",
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        Assert.Contains("occasionDate", json, StringComparison.Ordinal);
        Assert.Contains("operatorLogin", json, StringComparison.Ordinal);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
    }
}
