using System.Net;
using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Maps collection exceptions to stored run reasons.</summary>
public static class OperatorSnapshotFailureMapper
{
    /// <summary>Maps <paramref name="exception"/> to a reason and optional detail line.</summary>
    public static (OperatorSnapshotRunReason Reason, string? Detail) Map(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return (OperatorSnapshotRunReason.SiteStopped, null);
        }

        if (exception is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            return (OperatorSnapshotRunReason.SignInOrToken, null);
        }

        if (exception is HttpRequestException httpEx)
        {
            var statusCode = (int?)httpEx.StatusCode;
            if (statusCode == (int)HttpStatusCode.TooManyRequests)
            {
                return (OperatorSnapshotRunReason.RateLimit, null);
            }

            if (statusCode is (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden)
            {
                if (statusCode == (int)HttpStatusCode.Forbidden &&
                    httpEx.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                {
                    return (OperatorSnapshotRunReason.RateLimit, null);
                }

                return (OperatorSnapshotRunReason.SignInOrToken, null);
            }

            return (OperatorSnapshotRunReason.Unexpected, BuildDetail(exception, statusCode));
        }

        return (OperatorSnapshotRunReason.Unexpected, BuildDetail(exception, null));
    }

    private static string? BuildDetail(Exception exception, int? statusCode)
    {
        var typeName = exception.GetType().Name;
        return statusCode is { } code ? $"{typeName} HTTP {code}" : typeName;
    }
}
