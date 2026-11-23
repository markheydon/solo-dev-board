using System.Net;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests;

public sealed class OperatorSnapshotFailureMapperTests
{
    [Fact]
    public void Map_Http429_ReturnsRateLimit()
    {
        var (reason, detail) = OperatorSnapshotFailureMapper.Map(
            new HttpRequestException("rate limit exceeded", inner: null, statusCode: HttpStatusCode.TooManyRequests));

        Assert.Equal(OperatorSnapshotRunReason.RateLimit, reason);
        Assert.Null(detail);
    }

    [Fact]
    public void Map_Http401_ReturnsSignInOrToken()
    {
        var (reason, _) = OperatorSnapshotFailureMapper.Map(
            new HttpRequestException("Unauthorized", inner: null, statusCode: HttpStatusCode.Unauthorized));

        Assert.Equal(OperatorSnapshotRunReason.SignInOrToken, reason);
    }
}
