using System.Net;
using System.Text;

namespace SoloDevBoard.App.Authentication;

/// <summary>Renders the PAT connectivity error page as static HTML.</summary>
internal static class PatConnectivityErrorPageRenderer
{
    /// <summary>Renders the PAT connectivity error page.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="reason">The PAT connectivity failure reason code.</param>
    /// <returns>The rendered error page and HTTP status code.</returns>
    public static StaticErrorPageResult Render(HttpContext context, string? reason)
    {
        ArgumentNullException.ThrowIfNull(context);

        var presentation = PatConnectivityErrorPresentationMapper.Resolve(reason);
        var returnUrl = GetSafeReturnUrl(context.Request.Query["returnUrl"].FirstOrDefault());

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
        builder.AppendLine("    <div class=\"actions\">");
        builder.AppendLine($"      <a class=\"button button-primary\" href=\"{Encode(returnUrl)}\" data-testid=\"pat-connectivity-return-home\">Return to home</a>");
        builder.AppendLine("    </div>");
        builder.AppendLine("  </main>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return new StaticErrorPageResult(builder.ToString(), presentation.StatusCode);
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

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
