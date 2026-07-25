using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Options;
using MudBlazor;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Themes;

namespace SoloDevBoard.App.Components.Shell.Layout;

/// <summary>Provides the main application shell layout.</summary>
public partial class MainLayout : IDisposable
{
    private bool _drawerOpen = true;
    private bool _isDarkMode;
    private bool _showSignOut;
    private bool _showPatConnectionStatus;
    private GitHubConnectivityStatusDto? _patConnectionStatus;
    private readonly MudTheme _theme = SoloDevBoardTheme.MudTheme;

    /// <summary>Gets or sets GitHub authentication options.</summary>
    [Inject]
    public IOptions<GitHubAuthOptions> GitHubAuthOptions { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Gets or sets the authentication state provider.</summary>
    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    /// <summary>Gets or sets the GitHub connectivity status service.</summary>
    [Inject]
    public IGitHubConnectivityStatusService ConnectivityStatusService { get; set; } = default!;

    /// <summary>Gets the icon for the dark mode toggle button.</summary>
    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.AutoMode,
        false => Icons.Material.Outlined.DarkMode,
    };

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        await LoadShellStateAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        await InvokeAsync(async () =>
        {
            await LoadShellStateAsync().ConfigureAwait(false);
            StateHasChanged();
        });
    }

    private async Task LoadShellStateAsync()
    {
        if (GitHubAuthOptions.Value.HostedSignInEnabled)
        {
            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
            _showSignOut = authenticationState.User.Identity?.IsAuthenticated == true;
            _showPatConnectionStatus = false;
            _patConnectionStatus = null;
            return;
        }

        _patConnectionStatus = await ConnectivityStatusService.GetStatusAsync().ConfigureAwait(false);
        _showPatConnectionStatus = true;
        _showSignOut = false;
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
