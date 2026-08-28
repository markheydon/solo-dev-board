using Bunit;
using MudBlazor;

namespace SoloDevBoard.App.Tests;

/// <summary>Shared bUnit helpers for asserting MudBlazor snackbar content.</summary>
internal static class SnackbarTestAssertions
{
    /// <summary>Asserts that the most recent snackbar contains the expected text.</summary>
    /// <param name="snackbarProvider">The rendered snackbar provider.</param>
    /// <param name="expected">The expected substring.</param>
    internal static void AssertLatestContains(IRenderedComponent<MudSnackbarProvider> snackbarProvider, string expected)
    {
        ArgumentNullException.ThrowIfNull(snackbarProvider);

        var snackbars = snackbarProvider.FindAll(".mud-snackbar");
        Assert.NotEmpty(snackbars);
        Assert.Contains(expected, snackbars[^1].TextContent, StringComparison.Ordinal);
    }
}
