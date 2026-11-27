using NSubstitute;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Application.Tests.Support;

namespace SoloDevBoard.Application.Tests;

public sealed class HostedCredentialGrantServiceTests
{
    private static readonly DateTimeOffset CompletedAt = new(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
    private const string Login = "octocat";

    [Fact]
    public async Task HostedCredentialGrantService_Browser_StoresWindowWithoutClearingWait()
    {
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = Login,
            CompletedAt = CompletedAt.AddDays(-5),
            SchemaVersion = 2,
            CredentialUnusable = true,
            WorkerRefreshToken = "existing-worker",
        });
        var statuses = new InMemoryAccountScanStatusStore();
        var sut = CreateSut(signIns, statuses);

        await sut.PersistBrowserSignInAsync(
            Login,
            CompletedAt,
            ["my-org"],
            TestContext.Current.CancellationToken);

        var stored = signIns.SignInRecords().Single();
        Assert.Equal(CompletedAt, stored.CompletedAt);
        Assert.True(stored.CredentialUnusable);
        Assert.Equal("existing-worker", stored.WorkerRefreshToken);
        Assert.Equal(2, stored.SchemaVersion);
    }

    [Fact]
    public async Task HostedCredentialGrantService_WorkerDuringSignIn_AcceptedBrowserToken_StoresWorkerToken()
    {
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = Login,
            CompletedAt = CompletedAt,
            SchemaVersion = 2,
            CredentialUnusable = true,
        });
        var validator = Substitute.For<IHostedGitHubSessionAccessValidator>();
        validator.IsAccessTokenAcceptedAsync("session-access", Arg.Any<CancellationToken>())
            .Returns(true);
        var statuses = new InMemoryAccountScanStatusStore();
        statuses.Seed(AccountScanStatusDocumentConstants.CreateNeverRun(Login));
        var sut = CreateSut(signIns, statuses, validator);

        var result = await sut.TryStoreWorkerTokenAsync(
            Login,
            "worker-refresh",
            DateTimeOffset.UtcNow.AddDays(1),
            "session-access",
            liftSuspendedTokenWait: false,
            TestContext.Current.CancellationToken);

        Assert.IsType<HostedCredentialGrantService.WorkerTokenStoreResult.Stored>(result);
        var stored = signIns.SignInRecords().Single();
        Assert.Equal("worker-refresh", stored.WorkerRefreshToken);
        Assert.False(stored.CredentialUnusable);
        Assert.Equal(CompletedAt, stored.CompletedAt);
    }

    [Fact]
    public async Task HostedCredentialGrantService_WorkerDuringSignIn_RejectedBrowserToken_DiscardsCandidateAndRestoresCookieOnly()
    {
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = Login,
            CompletedAt = CompletedAt,
            SchemaVersion = 2,
            WorkerRefreshToken = "keep-me",
        });
        var validator = Substitute.For<IHostedGitHubSessionAccessValidator>();
        validator.IsAccessTokenAcceptedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var sut = CreateSut(signIns, new InMemoryAccountScanStatusStore(), validator);

        var result = await sut.TryStoreWorkerTokenAsync(
            Login,
            "candidate-worker",
            null,
            "rejected-session",
            liftSuspendedTokenWait: false,
            TestContext.Current.CancellationToken);

        Assert.IsType<HostedCredentialGrantService.WorkerTokenStoreResult.BrowserTokenRejected>(result);
        Assert.Equal("keep-me", signIns.SignInRecords().Single().WorkerRefreshToken);
    }

    [Fact]
    public async Task HostedCredentialGrantService_WorkerRefresh_StoresWorkerTokenWithoutMovingWindow()
    {
        var signIns = new InMemoryHostedSignInRecordStore();
        signIns.Seed(new HostedSignInRecord
        {
            OperatorLogin = Login,
            CompletedAt = CompletedAt,
            OrganisationLogins = ["alpha"],
            SchemaVersion = 2,
        });
        var validator = Substitute.For<IHostedGitHubSessionAccessValidator>();
        validator.IsAccessTokenAcceptedAsync("session-access", Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut(signIns, new InMemoryAccountScanStatusStore(), validator);

        var result = await sut.TryStoreWorkerTokenAsync(
            Login,
            "refreshed-worker",
            DateTimeOffset.UtcNow.AddDays(30),
            "session-access",
            liftSuspendedTokenWait: true,
            TestContext.Current.CancellationToken);

        Assert.IsType<HostedCredentialGrantService.WorkerTokenStoreResult.Stored>(result);
        var stored = signIns.SignInRecords().Single();
        Assert.Equal(CompletedAt, stored.CompletedAt);
        Assert.Equal(["alpha"], stored.OrganisationLogins);
        Assert.Equal("refreshed-worker", stored.WorkerRefreshToken);
    }

    [Fact]
    public async Task HostedCredentialGrantService_Cancel_LeavesSignInAndRecordsNoScanFailure()
    {
        var signIns = new InMemoryHostedSignInRecordStore();
        var statuses = new InMemoryAccountScanStatusStore();
        var sut = CreateSut(signIns, statuses);

        await sut.PersistBrowserSignInAsync(
            Login,
            CompletedAt,
            ["my-org"],
            TestContext.Current.CancellationToken);

        var status = statuses.Peek(Login);
        Assert.NotNull(status);
        Assert.Equal(AccountScanStatusDocumentConstants.StatusNeverRun, status.Status);
        Assert.Null(status.LastResult);
        var stored = signIns.SignInRecords().Single();
        Assert.Equal(CompletedAt, stored.CompletedAt);
        Assert.Null(stored.WorkerRefreshToken);
    }

    private static HostedCredentialGrantService CreateSut(
        InMemoryHostedSignInRecordStore signIns,
        InMemoryAccountScanStatusStore statuses,
        IHostedGitHubSessionAccessValidator? validator = null)
    {
        validator ??= Substitute.For<IHostedGitHubSessionAccessValidator>();
        var transitions = AccountScanStatusTestFactory.CreateTransitions(statuses, TimeProvider.System);
        return new HostedCredentialGrantService(signIns, transitions, validator);
    }
}

file static class HostedSignInRecordStoreExtensions
{
    public static IReadOnlyList<HostedSignInRecord> SignInRecords(this InMemoryHostedSignInRecordStore store)
        => store.ListAsync().GetAwaiter().GetResult();
}
