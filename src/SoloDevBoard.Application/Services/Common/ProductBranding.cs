namespace SoloDevBoard.Application.Services.Common;

/// <summary>User-facing product identity. Code identifiers remain <c>SoloDevBoard</c>.</summary>
public static class ProductBranding
{
    /// <summary>Spoken and display name for chrome, docs, and end-user copy.</summary>
    public const string DisplayName = "Solo Dev Board";

    /// <summary>
    /// Relative path to the brand mark in the Blazor wwwroot (also used as favicon).
    /// The same SVG lives under <c>docs/assets/github-app/icon.svg</c> for the GitHub App listing.
    /// </summary>
    public const string MarkPath = "favicon.svg";
}
