using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests;

public sealed class OperatorSnapshotCollectionServiceTests
{
    [Fact]
    public async Task StartAsync_UnsafeOwnerLogin_ReturnsUnsafeOperatorLogin()
    {
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("../evil");

        var service = CreateService(userContext);

        var result = await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStartResult.UnsafeOperatorLogin, result);
    }

    private static OperatorSnapshotCollectionService CreateService(ICurrentUserContext userContext)
    {
        var store = Substitute.For<IOperatorSnapshotStore>();
        var coordinator = new OperatorSnapshotCollectionCoordinator(
            Substitute.For<ISnapshotCollectionCredentialScopeFactory>(),
            TimeProvider.System,
            new OperatorSnapshotApplicationStopping(CancellationToken.None),
            Substitute.For<Microsoft.Extensions.Logging.ILogger<OperatorSnapshotCollectionCoordinator>>());

        return new OperatorSnapshotCollectionService(
            userContext,
            store,
            coordinator,
            Options.Create(new OperatorSnapshotDurabilityOptions { DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine }),
            TimeProvider.System);
    }
}
