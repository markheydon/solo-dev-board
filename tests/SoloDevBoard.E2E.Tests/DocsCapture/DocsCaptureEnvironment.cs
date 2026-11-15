namespace SoloDevBoard.E2E.Tests.DocsCapture;

/// <summary>
/// Environment gate for manual documentation screenshot capture tests.
/// </summary>
public static class DocsCaptureEnvironment
{
    /// <summary>
    /// Whether documentation screenshot capture tests are enabled via <c>DOCS_CAPTURE_ENABLED=1</c>.
    /// </summary>
    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("DOCS_CAPTURE_ENABLED"), "1", StringComparison.Ordinal);
}
