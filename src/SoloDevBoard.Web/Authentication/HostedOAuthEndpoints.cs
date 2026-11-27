using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Web.Authentication;

/// <summary>Maps hosted GitHub OAuth routes for browser and worker grants.</summary>
internal static class HostedOAuthEndpoints
{
    internal static void MapHostedSignInRoutes(this WebApplication app)
    {
        app.MapGet("/auth/sign-in", BeginAuthorisation(HostedOAuthStatePurpose.Browser)).AllowAnonymous();
        app.MapGet("/auth/worker-sign-in", BeginAuthorisation(HostedOAuthStatePurpose.WorkerDuringSignIn)).AllowAnonymous();
        app.MapGet("/auth/browser-restore", BeginAuthorisation(HostedOAuthStatePurpose.BrowserRestore)).AllowAnonymous();
        app.MapGet("/auth/refresh-worker", BeginAuthorisation(HostedOAuthStatePurpose.WorkerRefresh, requiresAuth: true)).AllowAnonymous();
        app.MapGet("/auth/callback", HandleCallback).AllowAnonymous();
    }

    private static Delegate BeginAuthorisation(string purpose, bool requiresAuth = false) =>
        (HttpContext context, HostedGitHubAuthGateway authGateway, IOptions<GitHubAuthOptions> optionsAccessor) =>
        {
            if (requiresAuth && context.User?.Identity?.IsAuthenticated != true)
            {
                return Results.Redirect("/welcome");
            }

            var returnUrl = GetReturnUrlFromQuery(context.Request.Query);
            var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
            var options = optionsAccessor.Value;
            var callbackUri = BuildCallbackUri(context, options);
            var authoriseUrl = authGateway.BuildAuthoriseUrl(state, callbackUri);
            HostedOAuthStateCookie.Write(context, state, returnUrl, purpose);
            return Results.Redirect(authoriseUrl);
        };

    private static async Task<IResult> HandleCallback(
        HttpContext context,
        HostedGitHubAuthGateway authGateway,
        HostedCredentialGrantService credentialGrants,
        ILogger<Program> logger)
    {
        if (!HostedOAuthStateCookie.TryReadAndClear(context, out var state, out var returnUrl, out var purpose))
        {
            return Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInStateInvalid));
        }

        var returnedState = context.Request.Query["state"].ToString();
        if (!string.Equals(state, returnedState, StringComparison.Ordinal))
        {
            return Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInStateInvalid));
        }

        var signInError = context.Request.Query["error"].ToString();
        if (!string.IsNullOrWhiteSpace(signInError))
        {
            return purpose == HostedOAuthStatePurpose.Browser
                ? Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInDenied))
                : Results.Redirect(returnUrl);
        }

        var code = context.Request.Query["code"].ToString();
        if (string.IsNullOrWhiteSpace(code))
        {
            return purpose == HostedOAuthStatePurpose.Browser
                ? Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInIncomplete))
                : Results.Redirect(returnUrl);
        }

        HostedGitHubAuthSession session;
        try
        {
            session = await authGateway.ExchangeCodeForSessionAsync(code, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            logger.LogWarning(ex, "Hosted OAuth callback failed for purpose {Purpose}.", purpose);
            return purpose == HostedOAuthStatePurpose.Browser
                ? Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInFailed))
                : Results.Redirect(returnUrl);
        }

        return purpose switch
        {
            HostedOAuthStatePurpose.Browser => await CompleteBrowserLegAsync(context, authGateway, credentialGrants, session, returnUrl),
            HostedOAuthStatePurpose.WorkerDuringSignIn => await CompleteWorkerDuringSignInAsync(
                context,
                credentialGrants,
                session,
                returnUrl),
            HostedOAuthStatePurpose.BrowserRestore => await CompleteBrowserRestoreAsync(context, authGateway, session, returnUrl),
            HostedOAuthStatePurpose.WorkerRefresh => await CompleteWorkerRefreshAsync(context, credentialGrants, session, returnUrl),
            _ => Results.Redirect(returnUrl),
        };
    }

    private static async Task<IResult> CompleteBrowserLegAsync(
        HttpContext context,
        HostedGitHubAuthGateway authGateway,
        HostedCredentialGrantService credentialGrants,
        HostedGitHubAuthSession session,
        string returnUrl)
    {
        await SignInSessionAsync(context, authGateway, session).ConfigureAwait(false);
        await HostedSignInPersistence.PersistBrowserSignInAsync(session, credentialGrants, context.RequestAborted).ConfigureAwait(false);

        var workerUrl = QueryHelpers.AddQueryString("/auth/worker-sign-in", new Dictionary<string, string?> { ["returnUrl"] = returnUrl });
        return Results.Redirect(workerUrl);
    }

    private static async Task<IResult> CompleteWorkerDuringSignInAsync(
        HttpContext context,
        HostedCredentialGrantService credentialGrants,
        HostedGitHubAuthSession candidate,
        string returnUrl)
    {
        var accessToken = GetSessionAccessToken(context);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            var restoreUrl = QueryHelpers.AddQueryString("/auth/browser-restore", new Dictionary<string, string?> { ["returnUrl"] = returnUrl });
            return Results.Redirect(restoreUrl);
        }

        var store = await credentialGrants.TryStoreWorkerTokenAsync(
            candidate.OwnerLogin,
            candidate.RefreshToken,
            candidate.RefreshTokenExpiresAtUtc,
            accessToken,
            liftSuspendedTokenWait: true,
            context.RequestAborted).ConfigureAwait(false);

        if (store is HostedCredentialGrantService.WorkerTokenStoreResult.BrowserTokenRejected)
        {
            var restoreUrl = QueryHelpers.AddQueryString("/auth/browser-restore", new Dictionary<string, string?> { ["returnUrl"] = returnUrl });
            return Results.Redirect(restoreUrl);
        }

        return Results.Redirect(returnUrl);
    }

    private static async Task<IResult> CompleteBrowserRestoreAsync(
        HttpContext context,
        HostedGitHubAuthGateway authGateway,
        HostedGitHubAuthSession session,
        string returnUrl)
    {
        await SignInSessionAsync(context, authGateway, session).ConfigureAwait(false);
        return Results.Redirect(returnUrl);
    }

    private static async Task<IResult> CompleteWorkerRefreshAsync(
        HttpContext context,
        HostedCredentialGrantService credentialGrants,
        HostedGitHubAuthSession session,
        string returnUrl)
    {
        var accessToken = GetSessionAccessToken(context);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Results.Redirect(returnUrl);
        }

        _ = await credentialGrants.TryStoreWorkerTokenAsync(
            session.OwnerLogin,
            session.RefreshToken,
            session.RefreshTokenExpiresAtUtc,
            accessToken,
            liftSuspendedTokenWait: true,
            context.RequestAborted).ConfigureAwait(false);

        return Results.Redirect(returnUrl);
    }

    private static async Task SignInSessionAsync(HttpContext context, HostedGitHubAuthGateway authGateway, HostedGitHubAuthSession session)
    {
        var principal = authGateway.CreatePrincipal(session);
        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true,
        };

        if (session.TokenExpiresAtUtc is { } expiresAtUtc)
        {
            authenticationProperties.ExpiresUtc = expiresAtUtc;
        }

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties).ConfigureAwait(false);
    }

    private static string? GetSessionAccessToken(HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<GitHubAuthOptions>>().Value;
        return context.User.FindFirst(options.HostedAccessTokenClaimType)?.Value;
    }

    private static string GetReturnUrlFromQuery(IQueryCollection query)
    {
        var returnUrl = query["returnUrl"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return SafeReturnUrl(returnUrl);
        }

        return SafeReturnUrl(query["ReturnUrl"].FirstOrDefault());
    }

    private static string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/repositories";
        }

        if (!returnUrl.StartsWith("/", StringComparison.Ordinal) || returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/repositories";
        }

        return returnUrl;
    }

    private static string BuildCallbackUri(HttpContext context, GitHubAuthOptions options)
    {
        var callbackPath = string.IsNullOrWhiteSpace(options.HostedSignInCallbackPath)
            ? "/auth/callback"
            : options.HostedSignInCallbackPath;
        if (!callbackPath.StartsWith("/", StringComparison.Ordinal))
        {
            callbackPath = $"/{callbackPath}";
        }

        if (!string.IsNullOrWhiteSpace(options.HostedSignInCallbackBaseUri)
            && Uri.TryCreate(options.HostedSignInCallbackBaseUri, UriKind.Absolute, out var callbackBaseUri))
        {
            return new Uri(callbackBaseUri, callbackPath).ToString();
        }

        return $"{context.Request.Scheme}://{context.Request.Host}{callbackPath}";
    }
}
