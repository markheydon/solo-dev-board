namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>
/// Configuration for local-only docs capture mode.
/// When enabled, repository and project board catalogues are restricted to public GitHub content
/// so documentation screenshots cannot leak private data. This is screenshot hygiene, not a security boundary.
/// </summary>
public sealed class DocsCaptureOptions
{
    /// <summary>Configuration section name for docs capture settings.</summary>
    public const string SectionName = "DocsCapture";

    /// <summary>
    /// Gets or sets a value indicating whether docs capture mode is enabled.
    /// Defaults to <see langword="false"/>. Enable only for local screenshot capture via user secrets
    /// or the <c>DocsCapture__Enabled</c> environment variable.
    /// </summary>
    public bool Enabled { get; set; }
}
