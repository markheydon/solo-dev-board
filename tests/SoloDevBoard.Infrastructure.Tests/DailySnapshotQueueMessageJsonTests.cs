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
    public void Serialise_RetryMessage_IncludesNotBefore()
    {
        var message = new DailySnapshotQueueMessage
        {
            OccasionDate = "2026-11-23",
            OperatorLogin = "octocat",
            Kind = "retry",
            NotBefore = "2026-11-23T05:47:00.0000000Z",
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        Assert.Contains("notBefore", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialise_UnmetFillMessage_IncludesNotBeforeWithoutOperator()
    {
        var message = new DailySnapshotQueueMessage
        {
            Kind = "unmetFill",
            NotBefore = "2026-11-23T06:15:00.0000000Z",
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        Assert.Contains("unmetFill", json, StringComparison.Ordinal);
        Assert.Contains("notBefore", json, StringComparison.Ordinal);
        Assert.Contains("\"operatorLogin\":\"\"", json, StringComparison.Ordinal);
    }

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
