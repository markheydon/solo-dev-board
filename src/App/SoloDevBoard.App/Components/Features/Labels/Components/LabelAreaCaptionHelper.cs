namespace SoloDevBoard.App.Components.Features.Labels.Components;

/// <summary>
/// Provides shared caption formatting for kept <c>area/*</c> label previews.
/// </summary>
internal static class LabelAreaCaptionHelper
{
    /// <summary>
    /// Formats the preview caption describing how many <c>area/*</c> labels are kept during delete operations.
    /// </summary>
    /// <param name="keptAreaLabelCount">The number of kept <c>area/*</c> labels.</param>
    /// <returns>A user-facing caption for the preview summary.</returns>
    public static string FormatKeptAreaLabelsCaption(int keptAreaLabelCount)
        => keptAreaLabelCount == 1
            ? "1 area/* label is excluded from delete and will be left unchanged."
            : $"{keptAreaLabelCount} area/* labels are excluded from delete and will be left unchanged.";

    /// <summary>
    /// Formats the preview caption describing how many source <c>area/*</c> labels are ignored during copy operations.
    /// </summary>
    /// <param name="ignoredAreaLabelCount">The number of ignored source <c>area/*</c> labels.</param>
    /// <returns>A user-facing caption for the preview summary.</returns>
    public static string FormatIgnoredAreaLabelsCaption(int ignoredAreaLabelCount)
        => ignoredAreaLabelCount == 1
            ? "1 source area/* label is ignored and will not be copied."
            : $"{ignoredAreaLabelCount} source area/* labels are ignored and will not be copied.";
}
