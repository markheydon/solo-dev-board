using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Authentication;

/// <summary>Persists browser and worker grants from the split hosted sign-in flow.</summary>
public sealed class HostedCredentialGrantService(
    IHostedSignInRecordStore signInRecords,
    AccountScanStatusTransitionService accountScanStatus,
    IHostedGitHubSessionAccessValidator accessValidator)
{
    private readonly IHostedSignInRecordStore _signInRecords =
        signInRecords ?? throw new ArgumentNullException(nameof(signInRecords));
    private readonly AccountScanStatusTransitionService _accountScanStatus =
        accountScanStatus ?? throw new ArgumentNullException(nameof(accountScanStatus));
    private readonly IHostedGitHubSessionAccessValidator _accessValidator =
        accessValidator ?? throw new ArgumentNullException(nameof(accessValidator));

    /// <summary>Result of trying to store a worker refresh token.</summary>
    public abstract record WorkerTokenStoreResult
    {
        /// <summary>Worker token stored.</summary>
        public sealed record Stored : WorkerTokenStoreResult;

        /// <summary>Browser access token was rejected; caller should restore the cookie only.</summary>
        public sealed record BrowserTokenRejected : WorkerTokenStoreResult;
    }

    /// <summary>
    /// Writes schema 2 sign-in facts after the browser leg. Does not write a worker token or clear
    /// <see cref="HostedSignInRecord.CredentialUnusable"/>.
    /// </summary>
    public async Task PersistBrowserSignInAsync(
        string operatorLogin,
        DateTimeOffset completedAt,
        IReadOnlyList<string> organisationLogins,
        CancellationToken cancellationToken = default)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin);
        if (sanitised is null)
        {
            return;
        }

        var existing = await _signInRecords.GetAsync(sanitised, cancellationToken).ConfigureAwait(false);
        var preserveCredentialWait = existing is HostedSignInRecordReadResult.Found found
            && found.Record.CredentialUnusable;

        var record = new HostedSignInRecord
        {
            SchemaVersion = 2,
            OperatorLogin = sanitised,
            CompletedAt = completedAt,
            OrganisationLogins = organisationLogins,
            CredentialUnusable = preserveCredentialWait,
        };

        if (existing is HostedSignInRecordReadResult.Found prior)
        {
            record.WorkerRefreshToken = prior.Record.WorkerRefreshToken;
            record.WorkerRefreshTokenExpiresAt = prior.Record.WorkerRefreshTokenExpiresAt;
        }

        var etag = existing is HostedSignInRecordReadResult.Found foundEtag ? foundEtag.Etag : null;
        await UpsertWithRetryAsync(record, etag, cancellationToken).ConfigureAwait(false);
        await _accountScanStatus.EnsureNeverRunIfAbsentAsync(sanitised, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stores a worker refresh token when the session access token is still accepted.
    /// </summary>
    public async Task<WorkerTokenStoreResult> TryStoreWorkerTokenAsync(
        string operatorLogin,
        string workerRefreshToken,
        DateTimeOffset? workerRefreshTokenExpiresAt,
        string sessionAccessToken,
        bool liftSuspendedTokenWait,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerRefreshToken);

        if (!await _accessValidator.IsAccessTokenAcceptedAsync(sessionAccessToken, cancellationToken).ConfigureAwait(false))
        {
            return new WorkerTokenStoreResult.BrowserTokenRejected();
        }

        var sanitised = OperatorLoginSanitiser.Sanitise(operatorLogin);
        if (sanitised is null)
        {
            return new WorkerTokenStoreResult.BrowserTokenRejected();
        }

        var existing = await _signInRecords.GetAsync(sanitised, cancellationToken).ConfigureAwait(false);
        if (existing is not HostedSignInRecordReadResult.Found found)
        {
            return new WorkerTokenStoreResult.BrowserTokenRejected();
        }

        var record = found.Record;
        record.SchemaVersion = 2;
        record.WorkerRefreshToken = workerRefreshToken;
        record.WorkerRefreshTokenExpiresAt = workerRefreshTokenExpiresAt;
        record.CredentialUnusable = false;

        await UpsertWithRetryAsync(record, found.Etag, cancellationToken).ConfigureAwait(false);

        if (liftSuspendedTokenWait)
        {
            await _accountScanStatus.LiftSuspendedTokenWaitAfterWorkerRefreshAsync(sanitised, cancellationToken)
                .ConfigureAwait(false);
        }

        return new WorkerTokenStoreResult.Stored();
    }

    private async Task UpsertWithRetryAsync(
        HostedSignInRecord record,
        string? expectedEtag,
        CancellationToken cancellationToken)
    {
        var upsert = await _signInRecords.UpsertAsync(record, expectedEtag, cancellationToken).ConfigureAwait(false);
        if (upsert is HostedSignInRecordWriteResult.ConcurrencyConflict)
        {
            var reread = await _signInRecords.GetAsync(record.OperatorLogin, cancellationToken).ConfigureAwait(false);
            if (reread is HostedSignInRecordReadResult.Found found)
            {
                record.WorkerRefreshToken ??= found.Record.WorkerRefreshToken;
                record.WorkerRefreshTokenExpiresAt ??= found.Record.WorkerRefreshTokenExpiresAt;
                record.CompletedAt = found.Record.CompletedAt;
                record.OrganisationLogins = found.Record.OrganisationLogins;
                if (found.Record.CredentialUnusable && record.CredentialUnusable)
                {
                    record.CredentialUnusable = found.Record.CredentialUnusable;
                }

                await _signInRecords.UpsertAsync(record, found.Etag, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
