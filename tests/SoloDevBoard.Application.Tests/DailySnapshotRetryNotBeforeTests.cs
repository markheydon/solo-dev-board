using System.Net;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests;

public sealed class DailySnapshotRetryNotBeforeTests
{
    [Fact]
    public void Compute_RateLimitWithoutGitHubDelay_UsesTwoMinuteMinimum()
    {
        var failedAt = new DateTimeOffset(2026, 11, 23, 5, 1, 0, TimeSpan.Zero);
        var notBefore = DailySnapshotRetryNotBefore.Compute(
            failedAt,
            OperatorSnapshotRunReason.RateLimit,
            attemptsUsed: 1,
            failureException: new HttpRequestException("429", null, HttpStatusCode.TooManyRequests));

        Assert.Equal(failedAt.AddMinutes(2), notBefore);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 6)]
    [InlineData(3, 12)]
    public void Compute_UnexpectedError_UsesStaggeredMinimumMinutes(int attemptsUsed, int expectedMinutes)
    {
        var failedAt = new DateTimeOffset(2026, 11, 23, 5, 1, 0, TimeSpan.Zero);
        var notBefore = DailySnapshotRetryNotBefore.Compute(
            failedAt,
            OperatorSnapshotRunReason.Unexpected,
            attemptsUsed,
            failureException: new InvalidOperationException("boom"));

        Assert.Equal(failedAt.AddMinutes(expectedMinutes), notBefore);
    }

    [Fact]
    public void Compute_RateLimitWithGitHubData_UsesSuggestedDelay()
    {
        var failedAt = new DateTimeOffset(2026, 11, 23, 5, 1, 0, TimeSpan.Zero);
        var exception = new HttpRequestException("429", null, HttpStatusCode.TooManyRequests);
        exception.Data[DailySnapshotRetryNotBefore.GitHubRetryDelayDataKey] = TimeSpan.FromSeconds(90);

        var notBefore = DailySnapshotRetryNotBefore.Compute(
            failedAt,
            OperatorSnapshotRunReason.RateLimit,
            attemptsUsed: 1,
            exception);

        Assert.Equal(failedAt.AddSeconds(90), notBefore);
    }
}
