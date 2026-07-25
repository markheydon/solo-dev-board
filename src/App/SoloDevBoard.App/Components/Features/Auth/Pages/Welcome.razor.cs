using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.App.Components.Features.Auth.Pages;

/// <summary>Hosted sign-in landing page for unauthenticated visitors.</summary>
public partial class Welcome
{
    private string _signInUrl = "/auth/sign-in";

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Gets or sets the authentication state provider.</summary>
    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    /// <summary>Gets or sets GitHub authentication options.</summary>
    [Inject]
    public IOptions<GitHubAuthOptions> GitHubAuthOptions { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        if (!GitHubAuthOptions.Value.HostedSignInEnabled)
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        var returnUrl = HostedAuthReturnUrl.GetRequestedReturnUrl(NavigationManager.ToAbsoluteUri(NavigationManager.Uri));
        _signInUrl = HostedAuthReturnUrl.BuildSignInUrl(returnUrl);

        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);

        if (authenticationState.User.Identity?.IsAuthenticated == true)
        {
            NavigationManager.NavigateTo(HostedAuthReturnUrl.ResolveDestination(returnUrl));
        }
    }
}
