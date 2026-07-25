using Microsoft.JSInterop;

namespace SoloDevBoard.App.Theming;

/// <summary>Guards JavaScript interop calls during Blazor Server circuit teardown.</summary>
internal static class JsInteropGuards
{
    /// <summary>Disposes a JavaScript module reference when the circuit is still connected.</summary>
    /// <param name="moduleReference">The module reference to dispose.</param>
    public static async ValueTask DisposeModuleSafeAsync(IJSObjectReference? moduleReference)
    {
        if (moduleReference is null)
        {
            return;
        }

        try
        {
            await moduleReference.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // The circuit disconnected before module cleanup completed.
        }
    }
}
