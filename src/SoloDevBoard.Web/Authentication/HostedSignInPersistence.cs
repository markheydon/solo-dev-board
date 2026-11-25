using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Web.Authentication;

/// <summary>Stores the hosted sign-in record and the missing account scan status after a completed sign-in.</summary>
internal static class HostedSignInPersistence
{
    /// <summary>
    /// Upserts the sign-in record, then inserts a <c>neverRun</c> status blob when none exists.
    /// A blob that already exists is left unchanged, and nothing is queued or collected.
    /// </summary>
    internal static async Task PersistCompletedSignInAsync(
        HostedGitHubAuthSession session,
        IHostedSignInRecordStore signInRecords,
        AccountScanStatusTransitionService accountScanStatus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(signInRecords);
        ArgumentNullException.ThrowIfNull(accountScanStatus);

        var sanitised = OperatorLoginSanitiser.Sanitise(session.OwnerLogin);
        if (sanitised is null)
        {
            return;
        }

        var record = new HostedSignInRecord
        {
            SchemaVersion = 1,
            OperatorLogin = sanitised,
            CompletedAt = DateTimeOffset.UtcNow,
            OrganisationLogins = session.OrganisationLogins,
            RefreshToken = session.RefreshToken,
            RefreshTokenExpiresAt = session.RefreshTokenExpiresAtUtc,
            CredentialUnusable = false,
        };

        var existingSignIn = await signInRecords.GetAsync(sanitised, cancellationToken).ConfigureAwait(false);
        var expectedEtag = existingSignIn is HostedSignInRecordReadResult.Found existing
            ? existing.Etag
            : null;
        var upsert = await signInRecords.UpsertAsync(record, expectedEtag, cancellationToken).ConfigureAwait(false);
        if (upsert is HostedSignInRecordWriteResult.ConcurrencyConflict)
        {
            var reread = await signInRecords.GetAsync(sanitised, cancellationToken).ConfigureAwait(false);
            if (reread is HostedSignInRecordReadResult.Found found)
            {
                await signInRecords.UpsertAsync(record, found.Etag, cancellationToken).ConfigureAwait(false);
            }
        }

        await accountScanStatus.EnsureNeverRunIfAbsentAsync(sanitised, cancellationToken).ConfigureAwait(false);
    }
}
