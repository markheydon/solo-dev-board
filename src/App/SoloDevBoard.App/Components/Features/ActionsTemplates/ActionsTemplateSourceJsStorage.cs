using Microsoft.JSInterop;
using SoloDevBoard.App.Theming;

namespace SoloDevBoard.App.Components.Features.ActionsTemplates;

/// <summary>Stores the last-used custom Actions template source in browser <c>localStorage</c> via JavaScript interop.</summary>
public sealed class ActionsTemplateSourceJsStorage(IJSRuntime jsRuntime) : IActionsTemplateSourceStorage, IAsyncDisposable
{
    private const string ModulePath = "./js/actionsTemplateSource.js";
    private IJSObjectReference? module;

    /// <inheritdoc/>
    public async Task<string?> GetLastUsedSourceAsync()
    {
        var moduleReference = await GetModuleAsync().ConfigureAwait(false);
        return await moduleReference.InvokeAsync<string?>("getLastUsedSource").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetLastUsedSourceAsync(string repositoryFullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryFullName);

        var moduleReference = await GetModuleAsync().ConfigureAwait(false);
        await moduleReference.InvokeVoidAsync("setLastUsedSource", repositoryFullName.Trim()).ConfigureAwait(false);
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
