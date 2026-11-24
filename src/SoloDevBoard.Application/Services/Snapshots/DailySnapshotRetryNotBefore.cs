using System.Globalization;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Computes queue <c>notBefore</c> for daily further tries.</summary>
public static class DailySnapshotRetryNotBefore
{
    /// <summary>Key on <see cref="Exception.Data"/> for a GitHub-suggested retry delay.</summary>
    public const string GitHubRetryDelayDataKey = "SoloDevBoard.GitHubRetryDelay";

    /// <summary>
    /// Computes the earliest UTC instant a further try may start, from the failed attempt only.
    /// </summary>
    public static DateTimeOffset Compute(
        DateTimeOffset failedAtUtc,
        OperatorSnapshotRunReason reason,
        int attemptsUsed,
        Exception? failureException)
    {
        return reason switch
        {
            OperatorSnapshotRunReason.RateLimit => failedAtUtc + (TryGetGitHubRetryDelay(failureException) ?? TimeSpan.FromMinutes(2)),
            OperatorSnapshotRunReason.Unexpected => failedAtUtc + GetUnexpectedMinimumWait(attemptsUsed),
            _ => failedAtUtc,
        };
    }

    /// <summary>Serialises <paramref name="notBeforeUtc"/> for the queue message body.</summary>
    public static string ToIso8601Utc(DateTimeOffset notBeforeUtc)
        => notBeforeUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    /// <summary>Parses a queue message <c>notBefore</c> value.</summary>
    public static bool TryParseIso8601Utc(string? value, out DateTimeOffset notBeforeUtc)
    {
        notBeforeUtc = default;
        return !string.IsNullOrWhiteSpace(value)
            && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out notBeforeUtc);
    }

    private static TimeSpan GetUnexpectedMinimumWait(int attemptsUsed) =>
        attemptsUsed switch
        {
            1 => TimeSpan.FromMinutes(3),
            2 => TimeSpan.FromMinutes(6),
            _ => TimeSpan.FromMinutes(12),
        };

    private static TimeSpan? TryGetGitHubRetryDelay(Exception? failureException)
    {
        for (var ex = failureException; ex is not null; ex = ex.InnerException)
        {
            if (ex.Data.Contains(GitHubRetryDelayDataKey) && ex.Data[GitHubRetryDelayDataKey] is TimeSpan delay && delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return null;
    }
}
