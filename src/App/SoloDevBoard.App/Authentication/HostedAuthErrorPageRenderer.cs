using System.Net;
using System.Text;
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
    /// <returns>The rendered error page and HTTP status code.</returns>
    public static StaticErrorPageResult Render(HttpContext context, string? reason, GitHubAuthOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authOptions);

        var presentation = HostedAuthErrorPresentationMapper.Resolve(reason);
        var returnUrl = GetSafeReturnUrl(context.Request.Query["returnUrl"].FirstOrDefault());
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
        builder.AppendLine("  <link rel=\"icon\" type=\"image/svg+xml\" href=\"/favicon.svg\" />");
        builder.AppendLine("  <link rel=\"icon\" type=\"image/png\" href=\"/favicon.png\" />");
        StaticErrorPageStyles.AppendStyleBlock(builder);
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
        builder.AppendLine($"      <a class=\"button button-primary\" href=\"{Encode(BuildSignInUrl(reason, returnUrl))}\" data-testid=\"auth-error-try-again\">{Encode(GetPrimaryActionLabel(reason))}</a>");

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

        return new StaticErrorPageResult(builder.ToString(), presentation.StatusCode);
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string BuildSignInUrl(string? reason, string returnUrl)
    {
        if (string.Equals(reason, HostedAuthErrorRoutes.SessionExpired, StringComparison.Ordinal))
        {
            return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("/auth/sign-in", "returnUrl", returnUrl);
        }

        return "/auth/sign-in";
    }

    private static string GetPrimaryActionLabel(string? reason) =>
        string.Equals(reason, HostedAuthErrorRoutes.SessionExpired, StringComparison.Ordinal)
            ? "Sign in again"
            : "Try again";

    private static string GetSafeReturnUrl(string? returnUrl)
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
}
