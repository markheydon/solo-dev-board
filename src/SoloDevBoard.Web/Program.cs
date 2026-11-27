using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Services.Planning;
using SoloDevBoard.Web.Authentication;
using SoloDevBoard.Web.Components.Features.ActionsTemplates;
using SoloDevBoard.Web.Components.Features.Planning;
using SoloDevBoard.Web.Components.Shell;
using SoloDevBoard.Web.Planning;
using SoloDevBoard.Web.Theming;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var hostedSignInEnabled = builder.Configuration.GetValue<bool>(
    $"{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.HostedSignInEnabled)}");
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
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.ShowTransitionDuration = 0;
    config.SnackbarConfiguration.HideTransitionDuration = 0;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IGitHubAuthenticationRecoveryService, GitHubAuthenticationRecoveryService>();
builder.Services.AddScoped<IGitHubAuthenticationSummaryService, GitHubAuthenticationSummaryService>();
builder.Services.AddScoped<IThemePreferenceStorage, ThemePreferenceJsStorage>();
builder.Services.AddScoped<IThemePreferenceService, ThemePreferenceService>();
builder.Services.AddScoped<IPlanningSettingsStorage, PlanningSettingsJsStorage>();
builder.Services.AddScoped<IActionsTemplateSourceStorage, ActionsTemplateSourceJsStorage>();
builder.Services.AddScoped<PlanningChromeCoordinator>();

if (hostedSignInEnabled)
{
    builder.Services.AddCascadingAuthenticationState();
}

if (hostedSignInEnabled)
{
    builder.Services.AddHostedGitHubAuthHttpClients();
    builder.Services.AddScoped<HostedGitHubAuthGateway>();
    builder.Services.AddSingleton<IHostedGitHubSessionAccessValidator, SoloDevBoard.Infrastructure.Identity.HostedGitHubSessionAccessValidator>();
    builder.Services.AddSingleton<HostedCredentialGrantService>();

    var hostedAuthOptions = builder.Configuration.GetSection(GitHubAuthOptions.SectionName).Get<GitHubAuthOptions>()
        ?? new GitHubAuthOptions();

    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/welcome";
            options.LogoutPath = "/auth/sign-out";
            options.Events = HostedCookieAuthenticationEvents.Create(hostedAuthOptions);
        });

    // Do not set FallbackPolicy: it challenges static assets (/_framework, /_content) before
    // Blazor can start. Page protection uses AuthorizeRouteView; HTTP protection uses admission middleware.
    builder.Services.AddAuthorization();
}

var useOperatorSnapshotBlob = !string.IsNullOrWhiteSpace(builder.Configuration["ConnectionStrings:operator-snapshots"]);
if (useOperatorSnapshotBlob)
{
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.OperatorSnapshots);
    builder.Services.AddOperatorSnapshotKeyedBlobStore(AspireHostedStorageReferenceNames.OperatorSnapshots);
}

if (hostedSignInEnabled && useOperatorSnapshotBlob)
{
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.HostedSignIns);
    builder.AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.AccountScanStatus);
    builder.Services.RegisterHostedSignInBlobStore();
    builder.Services.RegisterAccountScanStatusBlobStore();
}

builder.AddOperatorSnapshotHost(useOperatorSnapshotBlob);

// Add our services via the shared composition root.
builder.Services.AddSoloDevBoard(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapHealthChecks("/health/github", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("github"),
});

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
    SoloDevBoard.Composition.SoloDevBoardApplicationBuilderExtensions.UseSoloDevBoardInfrastructure(app);
}

app.UseAntiforgery();

app.MapStaticAssets();

if (hostedSignInEnabled)
{
    app.MapGet("/auth/error", static (HttpContext context, IOptions<GitHubAuthOptions> authOptionsAccessor) =>
    {
        var reason = context.Request.Query["reason"].ToString();
        var page = HostedAuthErrorPageRenderer.Render(context, reason, authOptionsAccessor.Value);
        return Results.Content(page.Html, "text/html; charset=utf-8", statusCode: page.StatusCode);
    }).AllowAnonymous();

    app.MapHostedSignInRoutes();

    app.MapGet("/auth/sign-out", (Delegate)SignOutHostedSession).AllowAnonymous();
    app.MapPost("/auth/sign-out", (Delegate)SignOutHostedSession).DisableAntiforgery().AllowAnonymous();

    app.MapGet("/auth/session-expired", static async (HttpContext context) =>
    {
        var returnUrl = GetSafeReturnUrl(GetReturnUrlFromQuery(context.Request.Query));

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

        return Results.Redirect(HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.SessionExpired, returnUrl));
    }).AllowAnonymous();
}

app.MapGet("/auth/connectivity-error", static (HttpContext context) =>
{
    var reason = context.Request.Query["reason"].ToString();
    var page = PatConnectivityErrorPageRenderer.Render(context, reason);
    return Results.Content(page.Html, "text/html; charset=utf-8", statusCode: page.StatusCode);
}).AllowAnonymous();

// Hosted sign-in uses AuthorizeRouteView and admission middleware for page protection.
// Do not call RequireAuthorization() here: it challenges Blazor infrastructure endpoints
// (for example POST /_blazor/negotiate) before the circuit starts, leaving /welcome blank.
var razorComponents = app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (hostedSignInEnabled)
{
    razorComponents.AllowAnonymous();
}

app.Run();

static async Task<IResult> SignOutHostedSession(HttpContext context)
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

    return Results.Redirect("/welcome");
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
