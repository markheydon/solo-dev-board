using Microsoft.JSInterop;

namespace SoloDevBoard.App.Theming;

/// <summary>Stores theme preferences in browser <c>localStorage</c> via JavaScript interop.</summary>
public sealed class ThemePreferenceJsStorage(IJSRuntime jsRuntime) : IThemePreferenceStorage, IAsyncDisposable
{
    private const string ModulePath = "./js/themePreference.js";
    private IJSObjectReference? module;

    /// <inheritdoc/>
    public async Task<ThemePreference> GetPreferenceAsync()
    {
        var moduleReference = await GetModuleAsync().ConfigureAwait(false);
        var storedValue = await moduleReference.InvokeAsync<string>("getPreference").ConfigureAwait(false);
        return ThemePreferenceExtensions.ParsePreference(storedValue);
    }

    /// <inheritdoc/>
    public async Task SetPreferenceAsync(ThemePreference preference)
    {
        var moduleReference = await GetModuleAsync().ConfigureAwait(false);
        await moduleReference.InvokeVoidAsync("setPreference", preference.ToStorageValue()).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> GetSystemIsDarkModeAsync()
    {
        var moduleReference = await GetModuleAsync().ConfigureAwait(false);
        return await moduleReference.InvokeAsync<bool>("getSystemIsDarkMode").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            await module.DisposeAsync().ConfigureAwait(false);
            module = null;
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (module is null)
        {
            module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(false);
        }

        return module;
    }
}
