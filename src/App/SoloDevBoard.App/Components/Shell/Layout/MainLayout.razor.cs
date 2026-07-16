using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using MudBlazor;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Themes;

namespace SoloDevBoard.App.Components.Shell.Layout;

/// <summary>Provides the main application shell layout.</summary>
public partial class MainLayout
{
    private bool _drawerOpen = true;
    private bool _isDarkMode;
    private bool _showSignOut;
    private readonly MudTheme _theme = SoloDevBoardTheme.MudTheme;

    /// <summary>Gets or sets GitHub authentication options.</summary>
    [Inject]
    public IOptions<GitHubAuthOptions> GitHubAuthOptions { get; set; } = default!;

    /// <summary>Gets or sets the authentication state provider.</summary>
    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    /// <summary>Gets the icon for the dark mode toggle button.</summary>
    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.AutoMode,
        false => Icons.Material.Outlined.DarkMode,
    };

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        if (GitHubAuthOptions.Value.HostedSignInEnabled)
        {
            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
            _showSignOut = authenticationState.User.Identity?.IsAuthenticated == true;
        }
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void DarkModeToggle()
    {
        _isDarkMode = !_isDarkMode;
    }
}
