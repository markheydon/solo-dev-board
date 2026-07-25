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
        builder.AppendLine("  <link href=\"https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap\" rel=\"stylesheet\" />");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: Roboto, Helvetica, Arial, sans-serif; margin: 0; background: #f5f5f5; color: #212121; }");
        builder.AppendLine("  .app-bar { background: #594ae2; color: #fff; padding: 1rem 1.5rem; box-shadow: 0 2px 4px rgba(0,0,0,.2); }");
        builder.AppendLine("  .app-bar h1 { margin: 0; font-size: 1.5rem; font-weight: 500; }");
        builder.AppendLine("  main { max-width: 36rem; margin: 0 auto; padding: 2rem 1.5rem; }");
        builder.AppendLine("  .actions { display: flex; gap: 0.75rem; flex-wrap: wrap; margin-top: 1.5rem; }");
        builder.AppendLine("  .button { display: inline-block; border: none; border-radius: 4px; padding: 0.6rem 1rem; font: inherit; text-decoration: none; cursor: pointer; }");
        builder.AppendLine("  .button-primary { background: #594ae2; color: #fff; }");
        builder.AppendLine("  </style>");
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
