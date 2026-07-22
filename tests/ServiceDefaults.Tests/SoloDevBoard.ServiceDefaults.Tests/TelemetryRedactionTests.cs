using SoloDevBoard.ServiceDefaults.Telemetry;

namespace SoloDevBoard.ServiceDefaults.Tests;

public sealed class TelemetryRedactionTests
{
    [Fact]
    public void RedactHttpUrl_NoQuery_ReturnsOriginalUrl()
    {
        const string url = "https://example.com/auth/sign-in";

        var redacted = TelemetryRedaction.RedactHttpUrl(url);

        Assert.Equal(url, redacted);
    }

    [Fact]
    public void RedactHttpUrl_SensitiveQueryKeys_ReplacesValuesWithRedactionMarker()
    {
        const string url = "https://example.com/auth/callback?code=secret-code&state=opaque-state&returnUrl=%2F";

        var redacted = TelemetryRedaction.RedactHttpUrl(url);

        Assert.Contains("code=%5BRedacted%5D", redacted, StringComparison.Ordinal);
        Assert.Contains("state=%5BRedacted%5D", redacted, StringComparison.Ordinal);
        Assert.Contains("returnUrl=%2F", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-code", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("opaque-state", redacted, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RedactHttpUrl_NullOrWhitespace_ReturnsEmpty(string? url)
    {
        var redacted = TelemetryRedaction.RedactHttpUrl(url);

        Assert.Equal(string.Empty, redacted);
    }

    [Fact]
    public void RedactHttpUrl_RelativeUrl_ReturnsOriginalValue()
    {
        const string url = "/auth/callback?code=secret";

        var redacted = TelemetryRedaction.RedactHttpUrl(url);

        Assert.Equal(url, redacted);
    }
}
