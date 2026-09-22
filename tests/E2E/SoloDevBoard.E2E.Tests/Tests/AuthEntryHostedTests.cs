using System.Text.Json;
using Microsoft.Playwright;
using SoloDevBoard.E2E.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace SoloDevBoard.E2E.Tests.Tests;

/// <summary>
/// Authentication entry tests for hosted sign-in mode.
/// </summary>
[Trait("Category", "E2E")]
[Trait("AuthMode", "Hosted")]
public sealed class AuthEntryHostedTests : SoloDevBoardPageTest
{
    private const int WelcomeSignInTimeoutMs = 30_000;

    /// <summary>
    /// Verifies unauthenticated home redirects to the welcome sign-in page.
    /// </summary>
    [Fact]
    public async Task UnauthenticatedHome_RedirectsToWelcomeSignIn()
    {
        Skip.If(!E2eAuthMode.IsHosted, "Hosted sign-in mode only");

        await Page.GotoAsync("/");

        await Expect(Page).ToHaveURLAsync(new Regex("/welcome"));
        await ExpectWelcomeSignInVisibleAsync();
    }

    /// <summary>
    /// Verifies the welcome page renders the hosted sign-in landing.
    /// </summary>
    [Fact]
    public async Task WelcomePage_RendersHostedSignInLanding()
    {
        Skip.If(!E2eAuthMode.IsHosted, "Hosted sign-in mode only");

        await Page.GotoAsync("/welcome");

        await Expect(Page).ToHaveURLAsync(new Regex("/welcome"));
        await ExpectWelcomeSignInVisibleAsync();
        await Expect(Page.GetByText(new Regex("sign in with your github account", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies Blazor negotiate succeeds before authentication.
    /// </summary>
    [Fact]
    public async Task BlazorNegotiate_SucceedsBeforeAuthentication()
    {
        Skip.If(!E2eAuthMode.IsHosted, "Hosted sign-in mode only");

        var response = await Page.APIRequest.PostAsync(
            "/_blazor/negotiate?negotiateVersion=1",
            new() { Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" } });

        Assert.Equal(200, response.Status);
        using var document = JsonDocument.Parse(await response.TextAsync());
        Assert.True(document.RootElement.TryGetProperty("connectionToken", out _));
    }

    /// <summary>
    /// Verifies a protected route does not render the feature shell without sign-in.
    /// </summary>
    [Fact]
    public async Task ProtectedRoute_DoesNotRenderFeatureShellWithoutSignIn()
    {
        Skip.If(!E2eAuthMode.IsHosted, "Hosted sign-in mode only");

        await Page.GotoAsync("/repositories");

        await Expect(Page).ToHaveURLAsync(new Regex("/welcome"));
        await ExpectWelcomeSignInVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("repositories", RegexOptions.IgnoreCase) })).ToHaveCountAsync(0);
    }

    private async Task ExpectWelcomeSignInVisibleAsync()
    {
        await Expect(Page.GetByTestId("welcome-sign-in")).ToBeVisibleAsync(new() { Timeout = WelcomeSignInTimeoutMs });
        await Expect(Page.GetByTestId("welcome-sign-in")).ToContainTextAsync("Sign in with GitHub");
    }
}
