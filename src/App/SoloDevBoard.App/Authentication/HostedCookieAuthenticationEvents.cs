using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Authentication;

/// <summary>Configures hosted cookie authentication validation events.</summary>
internal static class HostedCookieAuthenticationEvents
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);
    private const string SessionExpiredItemKey = "solo-dev-board.hosted-session-expired";

    /// <summary>Creates cookie authentication events for hosted sign-in sessions.</summary>
    /// <param name="authOptions">GitHub authentication options.</param>
    /// <returns>Cookie authentication events for hosted deployments.</returns>
    public static CookieAuthenticationEvents Create(GitHubAuthOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions);

        return new CookieAuthenticationEvents
        {
            OnValidatePrincipal = context => ValidatePrincipalAsync(context, authOptions),
            OnRedirectToLogin = context => RedirectToLoginAsync(context),
        };
    }

    private static async Task ValidatePrincipalAsync(
        CookieValidatePrincipalContext context,
        GitHubAuthOptions authOptions)
    {
        if (context.Principal is not { } principal)
        {
            return;
        }

        var accessTokenExpiresAtUtc = GetAccessTokenExpiresAtUtc(principal, authOptions);
        if (accessTokenExpiresAtUtc is null)
        {
            return;
        }

        if (accessTokenExpiresAtUtc > DateTimeOffset.UtcNow + RefreshSkew)
        {
            return;
        }

        var refreshToken = principal.FindFirst(authOptions.HostedRefreshTokenClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            MarkSessionExpired(context);
            return;
        }

        var refreshTokenExpiresAtUtc = GetRefreshTokenExpiresAtUtc(principal, authOptions);
        if (refreshTokenExpiresAtUtc is { } refreshExpiresAt && refreshExpiresAt <= DateTimeOffset.UtcNow)
        {
            MarkSessionExpired(context);
            return;
        }

        if (context.HttpContext.RequestServices.GetService<HostedGitHubAuthGateway>() is not { } gateway)
        {
            MarkSessionExpired(context);
            return;
        }

        var admissionOptionsAccessor = context.HttpContext.RequestServices.GetService<IOptions<HostedAdmissionControlOptions>>();
        if (admissionOptionsAccessor is null)
        {
            MarkSessionExpired(context);
            return;
        }

        HostedGitHubAuthSession refreshedSession;
        try
        {
            var currentSession = CreateSessionFromPrincipal(principal, authOptions, admissionOptionsAccessor.Value);
            refreshedSession = await gateway.RefreshSessionAsync(currentSession, context.HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception)
        {
            MarkSessionExpired(context);
            return;
        }

        var refreshedPrincipal = gateway.CreatePrincipal(refreshedSession);
        context.ReplacePrincipal(refreshedPrincipal);
        context.ShouldRenew = true;
    }

    private static Task RedirectToLoginAsync(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.HttpContext.Items.TryGetValue(SessionExpiredItemKey, out var value) && value is true)
        {
            var returnUrl = ExtractReturnUrl(context.RedirectUri);
            context.RedirectUri = HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SessionExpired, returnUrl);
        }

        if (IsApiRequest(context))
        {
            context.Response.Headers.Location = context.RedirectUri;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }

    private static bool IsApiRequest(RedirectContext<CookieAuthenticationOptions> context)
    {
        var request = context.Request;

        if (string.Equals(request.Query[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal)
            || string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.Ordinal))
        {
            return true;
        }

        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint is null)
        {
            return false;
        }

        var disableRedirect = endpoint.Metadata.GetMetadata<IDisableCookieRedirectMetadata>() is not null;
        var allowRedirect = endpoint.Metadata.GetMetadata<IAllowCookieRedirectMetadata>() is not null;

        return disableRedirect && !allowRedirect;
    }

    private static void MarkSessionExpired(CookieValidatePrincipalContext context)
    {
        context.HttpContext.Items[SessionExpiredItemKey] = true;
        context.RejectPrincipal();
    }

    private static HostedGitHubAuthSession CreateSessionFromPrincipal(
        ClaimsPrincipal principal,
        GitHubAuthOptions authOptions,
        HostedAdmissionControlOptions admissionOptions)
    {
        var ownerLogin = principal.FindFirst(authOptions.HostedOwnerLoginClaimType)?.Value ?? string.Empty;
        var accessToken = principal.FindFirst(authOptions.HostedAccessTokenClaimType)?.Value ?? string.Empty;
        var installationId = ParseInstallationId(principal.FindFirst(authOptions.HostedInstallationIdClaimType)?.Value);
        var tokenExpiresAtUtc = GetAccessTokenExpiresAtUtc(principal, authOptions);
        var refreshToken = principal.FindFirst(authOptions.HostedRefreshTokenClaimType)?.Value ?? string.Empty;
        var refreshTokenExpiresAtUtc = GetRefreshTokenExpiresAtUtc(principal, authOptions);
        var organisationLogins = ReadOrganisationLogins(principal, admissionOptions);

        return new HostedGitHubAuthSession(
            ownerLogin,
            accessToken,
            installationId,
            tokenExpiresAtUtc,
            organisationLogins,
            refreshToken,
            refreshTokenExpiresAtUtc);
    }

    private static long? ParseInstallationId(string? installationIdClaim)
    {
        if (long.TryParse(installationIdClaim, CultureInfo.InvariantCulture, out var installationId))
        {
            return installationId;
        }

        return null;
    }

    private static string[] ReadOrganisationLogins(ClaimsPrincipal principal, HostedAdmissionControlOptions admissionOptions)
    {
        if (string.IsNullOrWhiteSpace(admissionOptions.HostedOrganisationLoginsClaimType))
        {
            return [];
        }

        return principal
            .FindAll(admissionOptions.HostedOrganisationLoginsClaimType)
            .Select(static claim => claim.Value)
            .Where(static login => !string.IsNullOrWhiteSpace(login))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DateTimeOffset? GetAccessTokenExpiresAtUtc(ClaimsPrincipal principal, GitHubAuthOptions authOptions)
    {
        if (string.IsNullOrWhiteSpace(authOptions.HostedTokenExpiresAtClaimType))
        {
            return null;
        }

        var claim = principal.FindFirst(authOptions.HostedTokenExpiresAtClaimType)?.Value;
        return ParseExpiresAtClaim(claim);
    }

    private static DateTimeOffset? GetRefreshTokenExpiresAtUtc(ClaimsPrincipal principal, GitHubAuthOptions authOptions)
    {
        if (string.IsNullOrWhiteSpace(authOptions.HostedRefreshTokenExpiresAtClaimType))
        {
            return null;
        }

        var claim = principal.FindFirst(authOptions.HostedRefreshTokenExpiresAtClaimType)?.Value;
        return ParseExpiresAtClaim(claim);
    }

    private static DateTimeOffset? ParseExpiresAtClaim(string? claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
                claimValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var expiresAtUtc))
        {
            return expiresAtUtc;
        }

        return null;
    }

    private static string? ExtractReturnUrl(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return null;
        }

        var queryIndex = redirectUri.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(redirectUri[(queryIndex + 1)..]);
        return query.TryGetValue("ReturnUrl", out var returnUrl) ? returnUrl.ToString() : null;
    }
}
