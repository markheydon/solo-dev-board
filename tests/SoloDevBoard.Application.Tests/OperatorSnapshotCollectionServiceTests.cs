using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class OperatorSnapshotCollectionServiceTests
{
    [Fact]
    public async Task StartAsync_UnsafeOwnerLogin_ReturnsUnsafeOperatorLogin()
    {
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("../evil");

        var service = CreateService(userContext, new InMemoryOperatorSnapshotStore());

        var result = await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStartResult.UnsafeOperatorLogin, result);
    }

    [Fact]
    public async Task GetStatusAsync_NoDocument_ReturnsNeverRun()
    {
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");

        var service = CreateService(userContext, new InMemoryOperatorSnapshotStore());

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.NeverRun, status.Kind);
        Assert.Null(status.LastSuccessfulCollectionAt);
    }

    [Fact]
    public async Task GetStatusAsync_SucceededWithZeroRepositories_DiffersFromNeverRun()
    {
        var store = new InMemoryOperatorSnapshotStore();
        var finishedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z");
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Revision = 1,
            LastSuccessfulCollectionAt = finishedAt,
            Repositories = [],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Succeeded,
                StartedAt = finishedAt,
                FinishedAt = finishedAt,
                RepositoriesRecorded = 0,
            },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        var service = CreateService(userContext, store);

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.Succeeded, status.Kind);
        Assert.Equal(0, status.SnapshotRepositoryCount);
        Assert.NotNull(status.LastSuccessfulCollectionAt);
    }

    [Fact]
    public async Task GetStatusAsync_InterruptedRun_MapsInterruptedKind()
    {
        var store = new InMemoryOperatorSnapshotStore();
        var interruptedAt = DateTimeOffset.Parse("2026-06-01T13:00:00Z");
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Revision = 3,
            LastSuccessfulCollectionAt = DateTimeOffset.Parse("2026-06-01T10:00:00Z"),
            Repositories =
            [
                new OperatorSnapshotRepositoryEntry
                {
                    GitHubId = 1,
                    Owner = "octocat",
                    Name = "demo",
                    Visibility = "public",
                    HtmlUrl = "https://example.com",
                },
            ],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Interrupted,
                StartedAt = DateTimeOffset.Parse("2026-06-01T12:30:00Z"),
                FinishedAt = interruptedAt,
                Reason = OperatorSnapshotRunReason.SiteStopped,
            },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        var service = CreateService(userContext, store);

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.Interrupted, status.Kind);
        Assert.Equal(OperatorSnapshotRunReason.SiteStopped, status.Reason);
        Assert.Equal(1, status.SnapshotRepositoryCount);
    }

    [Fact]
    public async Task GetStatusAsync_FailedRun_KeepsLastSuccessfulSnapshotMetadata()
    {
        var store = new InMemoryOperatorSnapshotStore();
        var lastSuccess = DateTimeOffset.Parse("2026-05-01T08:00:00Z");
        store.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Revision = 4,
            LastSuccessfulCollectionAt = lastSuccess,
            Repositories =
            [
                new OperatorSnapshotRepositoryEntry
                {
                    GitHubId = 7,
                    Owner = "octocat",
                    Name = "kept",
                    Visibility = "public",
                    HtmlUrl = "https://example.com",
                },
            ],
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Failed,
                StartedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z"),
                FinishedAt = DateTimeOffset.Parse("2026-06-01T12:00:30Z"),
                Reason = OperatorSnapshotRunReason.RateLimit,
            },
        });

        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.OwnerLogin.Returns("octocat");
        var service = CreateService(userContext, store);

        var status = await service.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.Failed, status.Kind);
        Assert.Equal(lastSuccess, status.LastSuccessfulCollectionAt);
        Assert.Equal(1, status.SnapshotRepositoryCount);
        Assert.Equal(OperatorSnapshotRunReason.RateLimit, status.Reason);
    }

    private static OperatorSnapshotCollectionService CreateService(
        ICurrentUserContext userContext,
        IOperatorSnapshotStore store)
    {
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
