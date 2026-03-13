using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using MudBlazor.Services;
using SoloDevBoard.App.Components;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var hostedSignInEnabled = builder.Configuration.GetSection(GitHubAuthOptions.SectionName)
    .GetValue<bool>(nameof(GitHubAuthOptions.HostedSignInEnabled));

// Add MudBlazor services.
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

if (hostedSignInEnabled)
{
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(static options =>
        {
            options.LoginPath = "/auth/sign-in";
            options.LogoutPath = "/auth/sign-out";
            options.AccessDeniedPath = "/auth/access-denied";
        });

    builder.Services.AddAuthorization();
}

// Add our services.
// The Infrastructure project is referenced here solely as the DI composition root.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

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
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (hostedSignInEnabled)
{
    app.MapGet("/auth/sign-in", static () =>
        Results.Problem(
            title: "Hosted sign-in endpoint not configured",
            detail: "GitHub App user sign-in must be completed by the configured hosted authentication gateway before requests reach the Blazor application.",
            statusCode: StatusCodes.Status501NotImplemented));

    app.MapGet("/auth/sign-out", static async (HttpContext context) =>
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.Redirect("/");
    });
}

app.Run();
