using Microsoft.AspNetCore.Authentication.Cookies;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Authentication;

/// <summary>Configures hosted cookie authentication validation events.</summary>
internal static class HostedCookieAuthenticationEvents
{
    /// <summary>Creates cookie authentication events for hosted sign-in sessions.</summary>
    /// <param name="authOptions">GitHub authentication options.</param>
    /// <returns>Cookie authentication events for hosted deployments.</returns>
    public static CookieAuthenticationEvents Create(GitHubAuthOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions);

        return new CookieAuthenticationEvents
        {
            OnValidatePrincipal = context => ValidatePrincipalAsync(context, authOptions),
        };
    }

    private static Task ValidatePrincipalAsync(
        CookieValidatePrincipalContext context,
        GitHubAuthOptions authOptions)
    {
        if (HostedTokenExpiryValidator.IsExpired(context.Principal, authOptions))
        {
            context.RejectPrincipal();
        }

        return Task.CompletedTask;
    }
}
