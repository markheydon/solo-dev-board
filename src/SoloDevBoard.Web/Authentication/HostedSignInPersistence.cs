using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Web.Authentication;

/// <summary>Stores the hosted sign-in record after the browser leg of sign-in.</summary>
internal static class HostedSignInPersistence
{
    /// <summary>
    /// Upserts schema 2 sign-in facts without a worker token, then inserts a <c>neverRun</c> status blob when none exists.
    /// </summary>
    internal static Task PersistBrowserSignInAsync(
        HostedGitHubAuthSession session,
        HostedCredentialGrantService credentialGrants,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(credentialGrants);

        var sanitised = OperatorLoginSanitiser.Sanitise(session.OwnerLogin);
        if (sanitised is null)
        {
            return Task.CompletedTask;
        }

        return credentialGrants.PersistBrowserSignInAsync(
            sanitised,
            DateTimeOffset.UtcNow,
            session.OrganisationLogins,
            cancellationToken);
    }
}
