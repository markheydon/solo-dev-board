using MudBlazor;

namespace SoloDevBoard.App.Feedback;

/// <summary>Centralises snackbar duration defaults for transient user feedback.</summary>
internal static class SnackbarFeedback
{
    private const int DefaultDurationMs = 5000;
    private const int ErrorDurationMs = 6000;

    /// <summary>Shows a snackbar with severity-appropriate visibility duration.</summary>
    /// <param name="snackbar">The snackbar service.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="severity">The message severity.</param>
    public static void Show(ISnackbar snackbar, string message, Severity severity)
    {
        ArgumentNullException.ThrowIfNull(snackbar);

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var duration = severity == Severity.Error ? ErrorDurationMs : DefaultDurationMs;
        snackbar.Add(message, severity, configure: config => config.VisibleStateDuration = duration);
    }
}
