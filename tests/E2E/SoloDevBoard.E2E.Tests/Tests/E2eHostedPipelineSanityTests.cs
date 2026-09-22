namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Ensures the hosted Playwright CI matrix filter always discovers at least one test.
/// </summary>
[Trait("Category", "E2E")]
[Trait("AuthMode", "Hosted")]
public sealed class E2eHostedPipelineSanityTests
{
    /// <summary>
    /// Verifies the hosted matrix filter matches at least one test in this assembly.
    /// </summary>
    [Fact]
    public void HostedMatrixFilter_AlwaysMatchesAtLeastOneTest() => Assert.True(true);
}
