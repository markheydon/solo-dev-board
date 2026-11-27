using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace SoloDevBoard.Web.Authentication;

/// <summary>State cookie for hosted GitHub OAuth purposes.</summary>
internal static class HostedOAuthStateCookie
{
    internal const string CookieName = "solo-dev-board.hosted-sign-in-state";

    internal static void Write(HttpContext context, string state, string returnUrl, string purpose)
    {
        var payload = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes($"{state}|{returnUrl}|{purpose}"));
        context.Response.Cookies.Append(
            CookieName,
            payload,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.None,
                Secure = true,
                MaxAge = TimeSpan.FromMinutes(10),
            });
    }

    internal static bool TryReadAndClear(HttpContext context, out string state, out string returnUrl, out string purpose)
    {
        state = string.Empty;
        returnUrl = "/";
        purpose = SoloDevBoard.Application.Authentication.HostedOAuthStatePurpose.Browser;

        if (!context.Request.Cookies.TryGetValue(CookieName, out var payload) || string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        Delete(context);

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(payload));
        }
        catch (FormatException)
        {
            return false;
        }

        var parts = decoded.Split('|', 3);
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return false;
        }

        state = parts[0];
        returnUrl = GetSafeReturnUrl(parts[1]);
        purpose = parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2])
            ? parts[2]
            : SoloDevBoard.Application.Authentication.HostedOAuthStatePurpose.Browser;
        return true;
    }

    internal static void Delete(HttpContext context) =>
        context.Response.Cookies.Delete(
            CookieName,
            new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.None,
            });

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
