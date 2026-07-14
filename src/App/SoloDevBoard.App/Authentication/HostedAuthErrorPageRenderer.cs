using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Authentication;

/// <summary>Renders the hosted authentication error page as static HTML.</summary>
internal static class HostedAuthErrorPageRenderer
{
    /// <summary>Renders the hosted authentication error page.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="reason">The hosted authentication failure reason code.</param>
    /// <param name="authOptions">GitHub authentication options.</param>
    /// <returns>Static HTML for the error page.</returns>
    public static string Render(HttpContext context, string? reason, GitHubAuthOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authOptions);

        var presentation = HostedAuthErrorPresentationMapper.Resolve(reason);
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        var ownerLogin = isAuthenticated
            ? context.User.FindFirst(authOptions.HostedOwnerLoginClaimType)?.Value ?? context.User.Identity?.Name
            : null;
        var showSignedInAccount = string.Equals(reason, HostedAuthErrorRoutes.AccessDenied, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(ownerLogin);

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en-GB\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\" />");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        builder.AppendLine($"  <title>{Encode(presentation.Title)}</title>");
        builder.AppendLine("  <link href=\"https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap\" rel=\"stylesheet\" />");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: Roboto, Helvetica, Arial, sans-serif; margin: 0; background: #f5f5f5; color: #212121; }");
        builder.AppendLine("  .app-bar { background: #594ae2; color: #fff; padding: 1rem 1.5rem; box-shadow: 0 2px 4px rgba(0,0,0,.2); }");
        builder.AppendLine("  .app-bar h1 { margin: 0; font-size: 1.5rem; font-weight: 500; }");
        builder.AppendLine("  main { max-width: 36rem; margin: 0 auto; padding: 2rem 1.5rem; }");
        builder.AppendLine("  .card { background: #fff; border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,.12); padding: 1rem; margin: 1rem 0; }");
        builder.AppendLine("  .actions { display: flex; gap: 0.75rem; flex-wrap: wrap; margin-top: 1.5rem; }");
        builder.AppendLine("  .button { display: inline-block; border: none; border-radius: 4px; padding: 0.6rem 1rem; font: inherit; text-decoration: none; cursor: pointer; }");
        builder.AppendLine("  .button-primary { background: #594ae2; color: #fff; }");
        builder.AppendLine("  .button-secondary { background: transparent; color: #594ae2; border: 1px solid #594ae2; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <header class=\"app-bar\"><h1>SoloDevBoard</h1></header>");
        builder.AppendLine("  <main>");
        builder.AppendLine($"    <h2>{Encode(presentation.Title)}</h2>");
        builder.AppendLine($"    <p>{Encode(presentation.Message)}</p>");

        if (showSignedInAccount)
        {
            builder.AppendLine("    <section class=\"card\">");
            builder.AppendLine("      <p><strong>Signed in as</strong></p>");
            builder.AppendLine($"      <p data-testid=\"auth-error-signed-in-login\">{Encode(ownerLogin)}</p>");
            builder.AppendLine("    </section>");
        }

        builder.AppendLine("    <div class=\"actions\">");
        builder.AppendLine("      <a class=\"button button-primary\" href=\"/auth/sign-in\" data-testid=\"auth-error-try-again\">Try again</a>");

        if (isAuthenticated)
        {
            builder.AppendLine("      <form method=\"post\" action=\"/auth/sign-out\">");
            builder.AppendLine("        <button class=\"button button-secondary\" type=\"submit\" data-testid=\"auth-error-sign-out\">Sign out</button>");
            builder.AppendLine("      </form>");
        }

        builder.AppendLine("    </div>");
        builder.AppendLine("  </main>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
