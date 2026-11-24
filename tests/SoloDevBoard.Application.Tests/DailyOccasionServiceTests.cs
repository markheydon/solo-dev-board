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
    public async Task TryFillDailyOccasion_WhenOutsideFillWindow_Skipped()
    {
        var time = new FixedTimeProvider(OccasionDay.AddMinutes(-1));
        var sut = DailyOccasionServiceTestFactory.Create(
          time,
          new InMemoryOperatorSnapshotStore(),
          new InMemoryHostedSignInRecordStore(),
          new RecordingDailyQueuePublisher(),
          new InMemoryOccasionMarkerStore());

        var result = await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DailyOccasionFillResult.Skipped, result);
    }

    [Fact]
    public async Task TryFillDailyOccasion_WhenAfterFillWindow_Skipped()
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

        Assert.Equal(DailyOccasionFillResult.Skipped, result);
        Assert.Empty(queue.Messages);
    }

    [Fact]
    public async Task TryFillDailyOccasion_AtOccasionStart_QueuesEligibleLoginOnce()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var snapshots = new InMemoryOperatorSnapshotStore();
        var sut = DailyOccasionServiceTestFactory.Create(time, snapshots, signIns, queue, new InMemoryOccasionMarkerStore());

        var result = await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DailyOccasionFillResult.Filled, result);
        var message = Assert.Single(queue.Messages);
        Assert.Equal("octocat", message.OperatorLogin);
        Assert.Equal("2026-11-23", message.OccasionDate);
        Assert.Equal("initial", message.Kind);
        var document = snapshots.GetDocument("octocat");
        Assert.NotNull(document);
        Assert.Equal("2026-11-23", document.QueuedOccasion);
        Assert.False(document.PassExecuting);
    }

    [Fact]
    public async Task TryFillDailyOccasion_At0510_WhenMarkerAbsent_Fills()
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
    public async Task TryFillDailyOccasion_WhenMarkerAlreadyPresent_AlreadyFilled()
    {
        var markers = new InMemoryOccasionMarkerStore();
        await markers.TryCreateAsync(
          new OccasionMarker { OccasionDate = "2026-11-23", FilledAt = OccasionDay, Operators = ["octocat"] },
          TestContext.Current.CancellationToken);

        var time = new FixedTimeProvider(OccasionDay.AddMinutes(10));
        var sut = DailyOccasionServiceTestFactory.Create(
          time,
          new InMemoryOperatorSnapshotStore(),
          new InMemoryHostedSignInRecordStore(),
          new RecordingDailyQueuePublisher(),
          markers);

        Assert.Equal(DailyOccasionFillResult.AlreadyFilled, await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TryFillDailyOccasion_LeanPosture_SkippedWithoutQueue()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var sut = DailyOccasionServiceTestFactory.Create(
          time,
          new InMemoryOperatorSnapshotStore(),
          signIns,
          queue,
          new InMemoryOccasionMarkerStore(),
          hostedScheduleEnabled: false);

        Assert.Equal(DailyOccasionFillResult.Skipped, await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken));
        Assert.Empty(queue.Messages);
    }

    [Fact]
    public async Task TryFillDailyOccasion_WhenQueuedOccasionAlreadySet_SkipsOperator()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var snapshots = new InMemoryOperatorSnapshotStore();
        snapshots.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-23",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });
        var sut = DailyOccasionServiceTestFactory.Create(time, snapshots, signIns, queue, new InMemoryOccasionMarkerStore());

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
        var snapshots = new InMemoryOperatorSnapshotStore();
        snapshots.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            QueuedOccasion = "2026-11-23",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            snapshots,
            signIns,
            queue,
            new InMemoryOccasionMarkerStore());

        await sut.RepairMissingInitialMessagesAsync(TestContext.Current.CancellationToken);

        var message = Assert.Single(queue.Messages);
        Assert.Equal("octocat", message.OperatorLogin);
        Assert.Equal("2026-11-23", message.OccasionDate);
    }

    [Fact]
    public async Task RepairMissingInitialMessages_WhenQueuedOccasionUnset_DoesNotEnqueue()
    {
        var time = new FixedTimeProvider(OccasionDay);
        var queue = new RecordingDailyQueuePublisher();
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(EligibleSignIn("octocat"));
        var snapshots = new InMemoryOperatorSnapshotStore();
        snapshots.Seed("octocat", new OperatorSnapshotDocument
        {
            OperatorLogin = "octocat",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            snapshots,
            signIns,
            queue,
            new InMemoryOccasionMarkerStore());

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
        var snapshots = new InMemoryOperatorSnapshotStore();
        snapshots.Seed("skipped-a", new OperatorSnapshotDocument
        {
            OperatorLogin = "skipped-a",
            QueuedOccasion = "2026-11-23",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });
        snapshots.Seed("skipped-b", new OperatorSnapshotDocument
        {
            OperatorLogin = "skipped-b",
            QueuedOccasion = "2026-11-23",
            Run = new OperatorSnapshotRun { Status = OperatorSnapshotRunStatus.Failed },
        });
        var runner = new RecordingRunnerLease();
        var sut = DailyOccasionServiceTestFactory.Create(
            time,
            snapshots,
            signIns,
            queue,
            new InMemoryOccasionMarkerStore(),
            runner: runner);

        await sut.TryFillDailyOccasionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, runner.RenewCount);
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
