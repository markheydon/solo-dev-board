namespace SoloDevBoard.App.Components.Pages;

/// <summary>
/// Provides shared colour style helpers for label presentation components.
/// </summary>
internal static class LabelColourStyleHelper
{
    /// <summary>
    /// Builds the inline chip style for the provided hexadecimal colour.
    /// </summary>
    /// <param name="colour">The hexadecimal colour value.</param>
    /// <returns>A style string for the label chip.</returns>
    public static string GetColourChipStyle(string colour)
    {
        var normalised = NormaliseHexColour(colour);
        var textColour = GetReadableTextColour(normalised);
        return $"background-color: #{normalised}; color: {textColour}; border-color: #{normalised};";
    }

    private static string NormaliseHexColour(string colour)
    {
        var candidate = colour?.Trim().TrimStart('#') ?? string.Empty;
        return candidate.Length == 6 && candidate.All(Uri.IsHexDigit)
            ? candidate
            : "ededed";
    }

    private static string GetReadableTextColour(string normalisedHexColour)
    {
        var red = Convert.ToInt32(normalisedHexColour[..2], 16);
        var green = Convert.ToInt32(normalisedHexColour[2..4], 16);
        var blue = Convert.ToInt32(normalisedHexColour[4..6], 16);

        var relativeLuminance = ((0.299 * red) + (0.587 * green) + (0.114 * blue)) / 255;
        return relativeLuminance >= 0.6 ? "#1f1f1f" : "#ffffff";
    }
}