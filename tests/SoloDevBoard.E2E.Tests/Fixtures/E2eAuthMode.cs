namespace SoloDevBoard.E2E.Tests.Fixtures;

/// <summary>
/// Active authentication mode for Playwright end-to-end tests.
/// </summary>
public enum E2eAuthModeValue
{
    /// <summary>PAT placeholder authentication.</summary>
    Pat,

    /// <summary>Hosted sign-in gate with placeholder GitHub App credentials.</summary>
    Hosted,
}

/// <summary>
/// Reads the configured E2E authentication mode from the environment.
/// </summary>
public static class E2eAuthMode
{
    /// <summary>
    /// Returns the active E2E authentication mode (defaults to PAT).
    /// </summary>
    public static E2eAuthModeValue Current
    {
        get
        {
            var mode = Environment.GetEnvironmentVariable("E2E_AUTH_MODE")?.ToLowerInvariant();
            return mode == "hosted" ? E2eAuthModeValue.Hosted : E2eAuthModeValue.Pat;
        }
    }

    /// <summary>
    /// Returns true when the suite is running in hosted sign-in mode.
    /// </summary>
    public static bool IsHosted => Current == E2eAuthModeValue.Hosted;

    /// <summary>
    /// Returns true when the suite is running in PAT mode.
    /// </summary>
    public static bool IsPat => Current == E2eAuthModeValue.Pat;
}
