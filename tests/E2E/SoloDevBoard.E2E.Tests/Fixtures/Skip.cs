namespace SoloDevBoard.E2E.Tests.Fixtures;

/// <summary>
/// Runtime skip helpers for conditional E2E test execution.
/// </summary>
public static class Skip
{
    /// <summary>
    /// Skips the current test when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">When true, the test is skipped.</param>
    /// <param name="reason">The skip reason reported to the test runner.</param>
    public static void If(bool condition, string reason) => Assert.SkipWhen(condition, reason);
}
