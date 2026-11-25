using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class DailyOccasionServiceTests
{
    private static readonly DateTimeOffset OccasionDay = new(2026, 11, 23, 5, 0, 0, TimeSpan.Zero);

    private static HostedSignInRecord EligibleSignIn(string login) => new()
    {
        OperatorLogin = login,
        CompletedAt = OccasionDay.AddDays(-1),
        OrganisationLogins = [],
        RefreshToken = "token",
        CredentialUnusable = false,
    };

    [Fact]
    public async Task TryFillDailyOccasion_Before0500Utc_FillsWhenCalled()
    {
        var time = new FixedTimeProvider(OccasionDay.AddMinutes(-1));
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var sut = DailyOccasionServiceTestFactory.Create(
          time,
          new InMemoryOperatorSnapshotStore(),
          signIns,
          queue,
          new InMemoryOccasionMarkerStore());

        var result = await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DailyOccasionFillResult.Filled, result);
        Assert.Single(queue.Messages);
    }

    [Fact]
    public async Task TryFillDailyOccasion_After0515Utc_StillFills()
    {
        var time = new FixedTimeProvider(OccasionDay.AddMinutes(16));
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var sut = DailyOccasionServiceTestFactory.Create(
          time,
          new InMemoryOperatorSnapshotStore(),
          signIns,
          queue,
          new InMemoryOccasionMarkerStore());

        var result = await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DailyOccasionFillResult.Filled, result);
        Assert.Single(queue.Messages);
    }

    [Fact]
    public async Task TryFillDailyOccasion_AtOccasionStart_QueuesEligibleLoginOnce()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var statusStore = new InMemoryAccountScanStatusStore();
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            new InMemoryOperatorSnapshotStore(),
            signIns,
            queue,
            new InMemoryOccasionMarkerStore(),
            accountScanStatus: statusStore);

        var result = await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DailyOccasionFillResult.Filled, result);
        var message = Assert.Single(queue.Messages);
        Assert.Equal("octocat", message.OperatorLogin);
        Assert.Equal("2026-11-23", message.OccasionDate);
        Assert.Equal("initial", message.Kind);
        var status = await statusStore.GetAsync("octocat", TestContext.Current.CancellationToken);
        Assert.IsType<AccountScanStatusReadResult.Found>(status);
        var found = (AccountScanStatusReadResult.Found)status;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusScheduled, found.Document.Status);
        Assert.True(found.Document.QueueMessagePending);
    }

    [Fact]
    public async Task TryFillDailyOccasion_CatchUpAt0510WhenMarkerAbsent_Fills()
    {
        var time = new FixedTimeProvider(OccasionDay.AddMinutes(10));
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("hubot"));
        var sut = DailyOccasionServiceTestFactory.Create(
          time,
          new InMemoryOperatorSnapshotStore(),
          signIns,
          queue,
          new InMemoryOccasionMarkerStore());

        Assert.Equal(DailyOccasionFillResult.Filled, await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken));
        Assert.Single(queue.Messages);
    }

    [Fact]
    public async Task TryFillDailyOccasion_WhenMarkerAlreadyPresent_QueuesAfterSuccess()
    {
        var markers = new InMemoryOccasionMarkerStore();
        await markers.TryCreateAsync(
          new OccasionMarker { OccasionDate = "2026-11-23", FilledAt = OccasionDay, Operators = ["octocat"] },
          TestContext.Current.CancellationToken);

        var time = new FixedTimeProvider(OccasionDay.AddHours(6));
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("hubot"));
        var snapshots = new InMemoryOperatorSnapshotStore();
        snapshots.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Trigger = OperatorSnapshotCollectionTrigger.Daily,
            Run = new OperatorSnapshotRun
            {
                Status = OperatorSnapshotRunStatus.Succeeded,
                FinishedAt = OccasionDay.AddHours(1),
            },
        });
        var sut = DailyOccasionServiceTestFactory.Create(time, snapshots, signIns, queue, markers);

        Assert.Equal(DailyOccasionFillResult.Filled, await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken));
        Assert.Single(queue.Messages);
        Assert.Equal("hubot", queue.Messages[0].OperatorLogin);
    }

    [Fact]
    public async Task TryFillDailyOccasion_LeanPosture_SkippedWithoutQueue()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var statusStore = new InMemoryAccountScanStatusStore();
        var sut = DailyOccasionServiceTestFactory.Create(
          time,
          new InMemoryOperatorSnapshotStore(),
          signIns,
          queue,
          new InMemoryOccasionMarkerStore(),
          hostedScheduleEnabled: false,
          accountScanStatus: statusStore);

        Assert.Equal(DailyOccasionFillResult.Skipped, await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken));
        Assert.Empty(queue.Messages);
        Assert.Equal(0, statusStore.WriteCount);
        Assert.Null(statusStore.Peek("octocat"));
    }

    [Fact]
    public async Task TryFillDailyOccasion_WhenQueuedOccasionAlreadySet_SkipsOperator()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(new AccountScanStatusDocument
        {
            OperatorLogin = "octocat",
            Status = AccountScanStatusDocumentConstants.StatusScheduled,
            QueueMessagePending = true,
            ScanGeneration = 1,
        });
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            new InMemoryOperatorSnapshotStore(),
            signIns,
            queue,
            new InMemoryOccasionMarkerStore(),
            accountScanStatus: statusStore);

        Assert.Equal(DailyOccasionFillResult.Filled, await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken));
        Assert.Empty(queue.Messages);
    }

    [Fact]
    public async Task RepairMissingInitialMessages_WhenNoMarker_EnqueuesForQueuedSnapshot()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(new AccountScanStatusDocument
        {
            OperatorLogin = "octocat",
            Status = AccountScanStatusDocumentConstants.StatusScheduled,
            QueueMessagePending = true,
            ScanGeneration = 1,
        });
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            new InMemoryOperatorSnapshotStore(),
            signIns,
            queue,
            new InMemoryOccasionMarkerStore(),
            accountScanStatus: statusStore);

        await sut.RepairMissingInitialMessagesAsync(TestContext.Current.CancellationToken);

        var message = Assert.Single(queue.Messages);
        Assert.Equal("octocat", message.OperatorLogin);
        Assert.Equal("2026-11-23", message.OccasionDate);
    }

    [Fact]
    public async Task RepairMissingInitialMessages_WhenStatusFinished_DoesNotEnqueue()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusFinished,
            generation: 1,
            lastResult: AccountScanStatusDocumentConstants.LastResultSuccess));
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            new InMemoryOperatorSnapshotStore(),
            signIns,
            queue,
            new InMemoryOccasionMarkerStore(),
            accountScanStatus: statusStore);

        await sut.RepairMissingInitialMessagesAsync(TestContext.Current.CancellationToken);

        Assert.Empty(queue.Messages);
    }

    [Fact]
    public async Task TryFillDailyOccasion_WhenMarkerUnreadable_AlreadyFilled()
    {
        var markers = new InMemoryOccasionMarkerStore();
        markers.MarkUnreadable("2026-11-23");
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            new InMemoryOperatorSnapshotStore(),
            signIns,
            queue,
            markers);

        Assert.Equal(DailyOccasionFillResult.AlreadyFilled, await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken));
        Assert.Empty(queue.Messages);
    }

    [Fact]
    public async Task TryFillDailyOccasion_RenewsRunnerLeaseOncePerSignInRecord()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("eligible"));
        signIns.Seed(EligibleSignIn("skipped-a"));
        signIns.Seed(EligibleSignIn("skipped-b"));
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "skipped-a", AccountScanStatusDocumentConstants.StatusScheduled, 1, queueMessagePending: true));
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "skipped-b", AccountScanStatusDocumentConstants.StatusScheduled, 1, queueMessagePending: true));
        var runner = new RecordingRunnerLease();
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            new InMemoryOperatorSnapshotStore(),
            signIns,
            queue,
            new InMemoryOccasionMarkerStore(),
            runner: runner,
            accountScanStatus: statusStore);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, runner.RenewCount);
    }

    [Fact]
    public async Task DailyOccasionService_OrphanScheduledWithNullLastResult_RestoresNeverRunBeforeEligibility()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "octocat", AccountScanStatusDocumentConstants.StatusScheduled, 2, queueMessagePending: false));
        var queue = new RecordingDailyQueuePublisher();
        var admission = new HostedAdmissionControlOptions { Enabled = true, AllowedUserLogins = "someone-else" };
        var sut = CreateSut(statusStore, queue, admission: admission);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        var stored = statusStore.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusNeverRun, stored.Status);
        Assert.Null(stored.LastResult);
        Assert.Equal(string.Empty, stored.ResultComment);
        Assert.Empty(queue.Messages);
    }

    [Fact]
    public async Task DailyOccasionService_OrphanScheduledWithLastResult_RestoresFinishedWithoutFailureComment()
    {
        var lastScan = OccasionDay.AddDays(-1);
        var document = AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusScheduled,
            2,
            queueMessagePending: false,
            lastResult: AccountScanStatusDocumentConstants.LastResultSuccess);
        document.LastScanAt = lastScan;
        document.ResultComment = AccountScanStatusDocumentConstants.CommentSuccess;
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(document);
        var queue = new RecordingDailyQueuePublisher();
        var admission = new HostedAdmissionControlOptions { Enabled = true, AllowedUserLogins = "someone-else" };
        var sut = CreateSut(statusStore, queue, admission: admission);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        var stored = statusStore.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, stored.Status);
        Assert.Equal(lastScan, stored.LastScanAt);
        Assert.Equal(AccountScanStatusDocumentConstants.LastResultSuccess, stored.LastResult);
        Assert.Equal(AccountScanStatusDocumentConstants.CommentSuccess, stored.ResultComment);
    }

    [Fact]
    public async Task DailyOccasionService_ReportedStalledAndEligible_QueuesAndSetsScheduled()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 4,
            queueMessagePending: true,
            livenessAt: OccasionDay.AddMinutes(-6),
            runningSince: OccasionDay.AddMinutes(-9)));
        var queue = new RecordingDailyQueuePublisher();
        var sut = CreateSut(statusStore, queue);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Single(queue.Messages);
        var stored = statusStore.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusScheduled, stored.Status);
        Assert.Equal(5, stored.ScanGeneration);
        Assert.True(stored.QueueMessagePending);
        Assert.Null(stored.LivenessAt);
        Assert.Null(stored.QueueVisibleAfter);
    }

    [Fact]
    public async Task DailyOccasionService_ToldRunning_DoesNotQueue()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusRunning,
            generation: 4,
            livenessAt: OccasionDay.AddMinutes(-5),
            runningSince: OccasionDay.AddMinutes(-9)));
        var queue = new RecordingDailyQueuePublisher();
        var sut = CreateSut(statusStore, queue);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Empty(queue.Messages);
        Assert.Equal(4, statusStore.Peek("octocat")!.ScanGeneration);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusRunning, statusStore.Peek("octocat")!.Status);
    }

    [Fact]
    public async Task DailyOccasionService_LostEtag_DoesNotEnqueueOrChangeSnapshot()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "octocat", AccountScanStatusDocumentConstants.StatusFinished, 1, lastResult: AccountScanStatusDocumentConstants.LastResultSuccess));
        statusStore.ConflictOnNextReplace = true;
        var snapshots = new InMemoryOperatorSnapshotStore();
        var queue = new RecordingDailyQueuePublisher();
        var sut = CreateSut(statusStore, queue, snapshots);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Empty(queue.Messages);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusFinished, statusStore.Peek("octocat")!.Status);
        Assert.Equal(1, statusStore.Peek("octocat")!.ScanGeneration);
        Assert.Null(snapshots.GetDocument("octocat"));
    }

    [Fact]
    public async Task DailyOccasionService_SignInRecordWithoutStatusBlob_InsertsNeverRunBeforeEligibility()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        var queue = new RecordingDailyQueuePublisher();
        var admission = new HostedAdmissionControlOptions { Enabled = true, AllowedUserLogins = "someone-else" };
        var sut = CreateSut(statusStore, queue, admission: admission);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        var stored = statusStore.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusNeverRun, stored.Status);
        Assert.Equal(0, stored.ScanGeneration);
        Assert.False(stored.QueueMessagePending);
        Assert.Empty(queue.Messages);
    }

    [Fact]
    public async Task DailyOccasionService_SecondFillAfterSuccess_QueuesIdleEligiblePersonAgain()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        var queue = new RecordingDailyQueuePublisher();
        var markers = new InMemoryOccasionMarkerStore();
        var time = new FixedTimeProvider(OccasionDay);
        var sut = CreateSut(statusStore, queue, markers: markers, time: time);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);
        var scheduled = statusStore.Peek("octocat")!;
        var finished = AccountScanStatusTestFactory.Document(
            "octocat",
            AccountScanStatusDocumentConstants.StatusFinished,
            scheduled.ScanGeneration,
            lastResult: AccountScanStatusDocumentConstants.LastResultSuccess);
        statusStore.Seed(finished);
        time.SetUtcNow(OccasionDay.AddHours(2));

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, queue.Messages.Count);
        Assert.Equal(scheduled.ScanGeneration + 1, statusStore.Peek("octocat")!.ScanGeneration);
    }

    [Fact]
    public async Task DailyOccasionService_CredentialUnusable_IsNotQueued()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        var queue = new RecordingDailyQueuePublisher();
        var sut = CreateSut(statusStore, queue, credentialUnusable: true);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Empty(queue.Messages);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusNeverRun, statusStore.Peek("octocat")!.Status);
    }

    [Fact]
    public async Task DailyOccasionService_EnqueueFails_LeavesQueueMessagePendingFalse()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        var queue = new RecordingDailyQueuePublisher { ThrowOnEnqueue = true };
        var sut = CreateSut(statusStore, queue);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        var stored = statusStore.Peek("octocat")!;
        Assert.Equal(AccountScanStatusDocumentConstants.StatusScheduled, stored.Status);
        Assert.False(stored.QueueMessagePending);
    }

    [Fact]
    public async Task DailyOccasionService_HostStartRepair_ReenqueuesMissingInitialOnlyWhenDue()
    {
        var statusStore = new InMemoryAccountScanStatusStore();
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        foreach (var login in new[] { "due", "future", "visible", "running", "notpending", "nullafter" })
        {
            signIns.Seed(EligibleSignIn(login));
        }

        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "due", AccountScanStatusDocumentConstants.StatusScheduled, 1, true, queueVisibleAfter: OccasionDay.AddMinutes(-1)));
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "future", AccountScanStatusDocumentConstants.StatusScheduled, 1, true, queueVisibleAfter: OccasionDay.AddMinutes(3)));
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "visible", AccountScanStatusDocumentConstants.StatusScheduled, 1, true));
        queue.VisibleLogins.Add("visible");
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "running", AccountScanStatusDocumentConstants.StatusRunning, 1, true, livenessAt: OccasionDay, runningSince: OccasionDay));
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "notpending", AccountScanStatusDocumentConstants.StatusScheduled, 1, false));
        statusStore.Seed(AccountScanStatusTestFactory.Document(
            "nullafter", AccountScanStatusDocumentConstants.StatusScheduled, 1, true));
        var sut = DailyOccasionServiceTestFactory.Create(
            new FixedTimeProvider(OccasionDay),
            new InMemoryOperatorSnapshotStore(),
            signIns,
            queue,
            new InMemoryOccasionMarkerStore(),
            accountScanStatus: statusStore);

        await sut.RepairMissingInitialMessagesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["due", "nullafter"], queue.Messages.Select(m => m.OperatorLogin).Order().ToArray());
        Assert.All(queue.Messages, m => Assert.Equal("initial", m.Kind));
    }

    private static DailyOccasionService CreateSut(
        InMemoryAccountScanStatusStore statusStore,
        RecordingDailyQueuePublisher queue,
        InMemoryOperatorSnapshotStore? snapshots = null,
        HostedAdmissionControlOptions? admission = null,
        InMemoryOccasionMarkerStore? markers = null,
        FixedTimeProvider? time = null,
        bool credentialUnusable = false)
    {
        var signIns = new InMemoryHostedSignInRecordStore();
        var record = EligibleSignIn("octocat");
        record.CredentialUnusable = credentialUnusable;
        signIns.Seed(record);
        return DailyOccasionServiceTestFactory.Create(
            time ?? new FixedTimeProvider(OccasionDay),
            snapshots ?? new InMemoryOperatorSnapshotStore(),
            signIns,
            queue,
            markers ?? new InMemoryOccasionMarkerStore(),
            admission: admission,
            accountScanStatus: statusStore);
    }

    [Fact]
    public async Task TryFillDailyOccasion_RunnerLeaseBusy_LostRace()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var sut = DailyOccasionServiceTestFactory.Create(
          time,
          new InMemoryOperatorSnapshotStore(),
          new InMemoryHostedSignInRecordStore(),
          new RecordingDailyQueuePublisher(),
          new InMemoryOccasionMarkerStore(),
          runner: new NeverAvailableRunnerLease());

        Assert.Equal(DailyOccasionFillResult.LostRace, await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken));
    }
}
