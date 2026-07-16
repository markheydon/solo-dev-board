using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.App.Components.Shell;
using SoloDevBoard.Application.Services.Common;
using SoloDevBoard.Infrastructure.Common;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

const string HostedSignInStateCookieName = "solo-dev-board.hosted-sign-in-state";

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var hostedSignInEnabled = builder.Configuration.GetSection(GitHubAuthOptions.SectionName)
    .GetValue<bool>(nameof(GitHubAuthOptions.HostedSignInEnabled));
var hostedOAuthFallbackEnabled = builder.Configuration.GetSection(GitHubAuthOptions.SectionName)
    .GetValue<bool>(nameof(GitHubAuthOptions.HostedOAuthAppFallbackEnabled));
var hostedCallbackBaseUri = builder.Configuration.GetSection(GitHubAuthOptions.SectionName)
    .GetValue<string>(nameof(GitHubAuthOptions.HostedSignInCallbackBaseUri));

if (TryGetConfiguredHttpsPort(hostedCallbackBaseUri, out var httpsPort))
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.HttpsPort = httpsPort;
    });
}

// Add MudBlazor services.
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IHostedAuthenticationRecoveryService, HostedAuthenticationRecoveryService>();

if (hostedSignInEnabled)
{
    builder.Services.AddCascadingAuthenticationState();
}

if (hostedSignInEnabled)
{
    builder.Services.AddHttpClient(HostedGitHubAuthGateway.HostedGitHubAuthClientName, static (serviceProvider, client) =>
    {
        var appVersionService = serviceProvider.GetRequiredService<IAppVersionService>();
        client.BaseAddress = new Uri("https://api.github.com");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(appVersionService.UserAgent);
    });
    builder.Services.AddScoped<HostedGitHubAuthGateway>();

    var hostedAuthOptions = builder.Configuration.GetSection(GitHubAuthOptions.SectionName).Get<GitHubAuthOptions>()
        ?? new GitHubAuthOptions();

    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/auth/sign-in";
            options.LogoutPath = "/auth/sign-out";
            options.Events = HostedCookieAuthenticationEvents.Create(hostedAuthOptions);
        });

    builder.Services.AddAuthorization();
}

// Add our services.
// The Infrastructure project is referenced here solely as the DI composition root.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

if (hostedSignInEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseHostedAdmissionControl();
}

app.UseAntiforgery();

app.MapStaticAssets();

if (hostedSignInEnabled)
{
    app.MapGet("/auth/error", static (HttpContext context, IOptions<GitHubAuthOptions> authOptionsAccessor) =>
    {
        var reason = context.Request.Query["reason"].ToString();
        var html = HostedAuthErrorPageRenderer.Render(context, reason, authOptionsAccessor.Value);
        return Results.Content(html, "text/html; charset=utf-8");
    });

    app.MapGet("/auth/sign-in", static (HttpContext context, HostedGitHubAuthGateway authGateway, IOptions<GitHubAuthOptions> optionsAccessor) =>
    {
        var returnUrl = GetReturnUrlFromQuery(context.Request.Query);
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(24));

        var options = optionsAccessor.Value;
        var callbackUri = BuildCallbackUri(context, options);
        var authoriseUrl = authGateway.BuildAuthoriseUrl(state, callbackUri);

        context.Response.Cookies.Append(
            HostedSignInStateCookieName,
            BuildStatePayload(state, returnUrl),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.None,
                Secure = true,
                MaxAge = TimeSpan.FromMinutes(10),
            });

        return Results.Redirect(authoriseUrl);
    });

    app.MapGet("/auth/callback", static async (
        HttpContext context,
        HostedGitHubAuthGateway authGateway,
        ILogger<Program> logger) =>
    {
        if (!TryReadAndClearStateCookie(context, out var state, out var returnUrl))
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
            return Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInDenied));
        }

        var code = context.Request.Query["code"].ToString();
        if (string.IsNullOrWhiteSpace(code))
        {
            return Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInIncomplete));
        }

        HostedGitHubAuthSession session;
        try
        {
            session = await authGateway.ExchangeCodeForSessionAsync(code, context.RequestAborted).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Hosted sign-in failed while establishing a session.");
            return Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInFailed));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Hosted sign-in failed because GitHub returned an unexpected HTTP response.");
            return Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInUnavailable));
        }

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
                authenticationProperties)
            .ConfigureAwait(false);

        return Results.Redirect(returnUrl);
    });

    if (hostedOAuthFallbackEnabled)
    {
        app.MapGet("/auth/sign-in/oauth-fallback", static () =>
            Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SignInMisconfigured)));
    }

    app.MapGet("/auth/sign-out", (Delegate)SignOutHostedSession);
    app.MapPost("/auth/sign-out", (Delegate)SignOutHostedSession).DisableAntiforgery();

    app.MapGet("/auth/session-expired", static async (HttpContext context) =>
    {
        var returnUrl = GetSafeReturnUrl(GetReturnUrlFromQuery(context.Request.Query));

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

        return Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SessionExpired, returnUrl));
    });
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task<IResult> SignOutHostedSession(HttpContext context)
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

    return Results.Redirect("/");
}

static string BuildCallbackUri(HttpContext context, GitHubAuthOptions options)
{
    ArgumentNullException.ThrowIfNull(options);

    var callbackPath = options.HostedSignInCallbackPath;
    if (string.IsNullOrWhiteSpace(callbackPath))
    {
        callbackPath = "/auth/callback";
    }

    if (!callbackPath.StartsWith("/", StringComparison.Ordinal))
    {
        callbackPath = $"/{callbackPath}";
    }

    if (!string.IsNullOrWhiteSpace(options.HostedSignInCallbackBaseUri))
    {
        if (!Uri.TryCreate(options.HostedSignInCallbackBaseUri, UriKind.Absolute, out var callbackBaseUri))
        {
            throw new InvalidOperationException("HostedSignInCallbackBaseUri must be a valid absolute URI when configured.");
        }

        return new Uri(callbackBaseUri, callbackPath).ToString();
    }

    return $"{context.Request.Scheme}://{context.Request.Host}{callbackPath}";
}

static string GetSafeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        return "/";
    }

    if (!returnUrl.StartsWith("/", StringComparison.Ordinal) || returnUrl.StartsWith("//", StringComparison.Ordinal))
    {
        return "/";
    }

    return returnUrl;
}

static string GetReturnUrlFromQuery(IQueryCollection query)
{
    var returnUrl = query["returnUrl"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(returnUrl))
    {
        return GetSafeReturnUrl(returnUrl);
    }

    return GetSafeReturnUrl(query["ReturnUrl"].FirstOrDefault());
}

static string BuildStatePayload(string state, string returnUrl)
{
    return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes($"{state}|{returnUrl}"));
}

static bool TryReadAndClearStateCookie(HttpContext context, out string state, out string returnUrl)
{
    state = string.Empty;
    returnUrl = "/";

    if (!context.Request.Cookies.TryGetValue(HostedSignInStateCookieName, out var payload) || string.IsNullOrWhiteSpace(payload))
    {
        return false;
    }

    DeleteHostedSignInStateCookie(context);

    string decoded;
    try
    {
        decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(payload));
    }
    catch (FormatException)
    {
        return false;
    }

    var separatorIndex = decoded.IndexOf('|');
    if (separatorIndex <= 0 || separatorIndex == decoded.Length - 1)
    {
        return false;
    }

    state = decoded[..separatorIndex];
    returnUrl = GetSafeReturnUrl(decoded[(separatorIndex + 1)..]);

    return true;
}

static void DeleteHostedSignInStateCookie(HttpContext context)
{
    context.Response.Cookies.Delete(
        HostedSignInStateCookieName,
        new CookieOptions
        {
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.None,
        });
}

static bool TryGetConfiguredHttpsPort(string? callbackBaseUri, out int httpsPort)
{
    httpsPort = 0;

    if (!Uri.TryCreate(callbackBaseUri, UriKind.Absolute, out var callbackUri))
    {
        return false;
    }

    if (!string.Equals(callbackUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    httpsPort = callbackUri.Port;
    return httpsPort > 0;
}
