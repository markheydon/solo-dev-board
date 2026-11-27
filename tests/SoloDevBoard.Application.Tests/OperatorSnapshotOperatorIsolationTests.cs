using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class OperatorSnapshotOperatorIsolationTests
{
    [Fact]
    public async Task GetStatusAsync_TwoOperators_ReturnIndependentStatuses()
    {
        var store = new InMemoryOperatorSnapshotStore();
        store.Seed("alice", new OperatorSnapshotDocument
        {
            OperatorLogin = "alice",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Succeeded, FinishedAt = DateTimeOffset.UtcNow },
            Repositories = [CreateEntry("alice", "a-repo")],
        });
        store.Seed("bob", new OperatorSnapshotDocument
        {
            OperatorLogin = "bob",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });

        var statuses = new InMemoryAccountScanStatusStore();
        statuses.Seed(AccountScanStatusTestFactory.Document(
            "bob",
            AccountScanStatusDocumentConstants.StatusScheduled,
            generation: 1,
            queueMessagePending: true));

        var aliceContext = Substitute.For<ICurrentUserContext>();
        aliceContext.OwnerLogin.Returns("alice");
        var bobContext = Substitute.For<ICurrentUserContext>();
        bobContext.OwnerLogin.Returns("bob");

        var aliceStatus = await CreateService(aliceContext, store, statuses).GetStatusAsync(TestContext.Current.CancellationToken);
        var bobStatus = await CreateService(bobContext, store, statuses).GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OperatorSnapshotStatusKind.Succeeded, aliceStatus.Kind);
        Assert.Equal(1, aliceStatus.SnapshotRepositoryCount);
        Assert.Equal(OperatorSnapshotStatusKind.Failed, bobStatus.Kind);
        Assert.Equal(ReportedAccountScanStatus.Scheduled, bobStatus.ReportedScanStatus);
        Assert.Equal(ReportedAccountScanStatus.NeverRun, aliceStatus.ReportedScanStatus);
    }

    private static OperatorSnapshotRepositoryEntry CreateEntry(string owner, string name) => new()
    {
        GitHubId = 1,
        Owner = owner,
        Name = name,
        Visibility = "public",
        HtmlUrl = $"https://example.com/{owner}/{name}",
    };

    private static OperatorSnapshotCollectionService CreateService(
        ICurrentUserContext userContext,
        IOperatorSnapshotStore store,
        InMemoryAccountScanStatusStore statuses)
        => new(
            userContext,
            store,
            new OperatorSnapshotCollectionCoordinator(
                Substitute.For<ISnapshotCollectionCredentialScopeFactory>(),
                TimeProvider.System,
                new OperatorSnapshotApplicationStopping(CancellationToken.None),
                NullLogger<OperatorSnapshotCollectionCoordinator>.Instance),
            Options.Create(new OperatorSnapshotDurabilityOptions { DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob }),
            TimeProvider.System,
            new OperatorSnapshotScheduleFactsService(
                new ScheduledCollectionEnvironment(
                    Options.Create(new GitHubAuthOptions { HostedSignInEnabled = true }),
                    Options.Create(new OperatorSnapshotDurabilityOptions { DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob })),
                new HostedCollectionEligibilityEvaluator(Options.Create(new HostedAdmissionControlOptions()))),
            new TestHostedSignInRecordStore(),
            new TestScheduledCollectionEnvironment(enabled: true),
            AccountScanStatusTestFactory.CreateTransitions(statuses, TimeProvider.System),
            NullLogger<OperatorSnapshotCollectionService>.Instance);

    private sealed class TestHostedSignInRecordStore : IHostedSignInRecordStore
    {
        public Task<IReadOnlyList<HostedSignInRecord>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HostedSignInRecord>>([]);

        public Task<HostedSignInRecordReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
            => Task.FromResult<HostedSignInRecordReadResult>(new HostedSignInRecordReadResult.NotFound());

        public Task<HostedSignInRecordWriteResult> UpsertAsync(
            HostedSignInRecord record,
            string? expectedEtag,
            CancellationToken cancellationToken = default)
            => Task.FromResult<HostedSignInRecordWriteResult>(new HostedSignInRecordWriteResult.Succeeded(string.Empty));
    }
}
