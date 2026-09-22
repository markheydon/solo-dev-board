using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace SoloDevBoard.E2E.Tests.Fixtures;

/// <summary>
/// Base Playwright page test that starts the SoloDevBoard application and configures browser context defaults.
/// </summary>
public abstract class SoloDevBoardPageTest : PageTest
{
    /// <inheritdoc />
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = SoloDevBoardWebApplicationFixture.BaseUrl,
        ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
    };

    /// <inheritdoc />
    public override async ValueTask InitializeAsync()
    {
        // Linux desktop sessions often export BROWSER=xdg-open, which Playwright misreads as a browser name.
        Environment.SetEnvironmentVariable("BROWSER", null);

        await SoloDevBoardWebApplicationFixture.EnsureStartedAsync();
        await base.InitializeAsync();
    }
}
