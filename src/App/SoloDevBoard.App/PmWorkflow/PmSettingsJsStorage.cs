using Microsoft.JSInterop;
using SoloDevBoard.App.Theming;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.PmWorkflow;

/// <summary>Stores PM settings in browser <c>localStorage</c> via JavaScript interop.</summary>
public sealed class PmSettingsJsStorage(IJSRuntime jsRuntime) : IPmSettingsStorage, IAsyncDisposable
{
    private const string ModulePath = "./js/pmSettings.js";
    private IJSObjectReference? module;

    /// <inheritdoc/>
    public async Task<string?> GetStoredJsonAsync()
    {
        var moduleReference = await GetModuleAsync().ConfigureAwait(false);
        return await moduleReference.InvokeAsync<string?>("getSettingsJson").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetStoredJsonAsync(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var moduleReference = await GetModuleAsync().ConfigureAwait(false);
        await moduleReference.InvokeVoidAsync("setSettingsJson", json).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await JsInteropGuards.DisposeModuleSafeAsync(module).ConfigureAwait(false);
        module = null;
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
