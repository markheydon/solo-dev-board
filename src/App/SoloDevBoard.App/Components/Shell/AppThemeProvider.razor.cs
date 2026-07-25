using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SoloDevBoard.App.Theming;

namespace SoloDevBoard.App.Components.Shell;

/// <summary>Applies the MudBlazor theme and synchronises browser document styling with the user's preference.</summary>
public partial class AppThemeProvider : IAsyncDisposable
{
    private const string ThemePreferenceModulePath = "./js/themePreference.js";

    private MudThemeProvider? _themeProvider;
    private IJSObjectReference? _themePreferenceModule;
    private bool _isDarkMode;
    private bool _observeSystem;

    /// <summary>Gets or sets the MudBlazor theme applied by the provider.</summary>
    [Parameter]
    public MudTheme Theme { get; set; } = default!;

    [Inject]
    private IThemePreferenceService ThemePreferenceService { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        ThemePreferenceService.PreferenceChanged += OnPreferenceChanged;
        await ThemePreferenceService.InitialiseAsync().ConfigureAwait(false);
        await ApplyPreferenceAsync().ConfigureAwait(false);
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }

    private async void OnPreferenceChanged()
    {
        try
        {
            await ApplyPreferenceAsync().ConfigureAwait(false);
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TaskCanceledException or JSDisconnectedException)
        {
            // The circuit disconnected while applying a preference change.
        }
    }

    private async Task OnIsDarkModeChangedAsync(bool isDarkMode)
    {
        if (ThemePreferenceService.ObserveSystemPreference)
        {
            await SetResolvedDarkModeAsync(isDarkMode).ConfigureAwait(false);
        }
    }

    private async Task ApplyPreferenceAsync()
    {
        _observeSystem = ThemePreferenceService.ObserveSystemPreference;

        var isDarkMode = ThemePreferenceService.ObserveSystemPreference && _themeProvider is not null
            ? await _themeProvider.GetSystemDarkModeAsync().ConfigureAwait(false)
            : ThemePreferenceService.EffectiveIsDarkMode;

        await SetResolvedDarkModeAsync(isDarkMode).ConfigureAwait(false);
    }

    private async Task SetResolvedDarkModeAsync(bool isDarkMode)
    {
        _isDarkMode = isDarkMode;
        await SyncDocumentThemeAsync(isDarkMode).ConfigureAwait(false);
    }

    private async Task SyncDocumentThemeAsync(bool isDarkMode)
    {
        try
        {
            var module = await GetThemePreferenceModuleAsync().ConfigureAwait(false);
            await module.InvokeVoidAsync("applyDocumentTheme", isDarkMode).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // The circuit disconnected before document styling could be updated.
        }
    }

    private async Task<IJSObjectReference> GetThemePreferenceModuleAsync()
    {
        _themePreferenceModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            ThemePreferenceModulePath).ConfigureAwait(false);

        return _themePreferenceModule;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        ThemePreferenceService.PreferenceChanged -= OnPreferenceChanged;
        await JsInteropGuards.DisposeModuleSafeAsync(_themePreferenceModule).ConfigureAwait(false);
        _themePreferenceModule = null;
    }
}
