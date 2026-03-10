using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>
/// Code-behind for the LabelPreviewTable component.
/// </summary>
public partial class LabelPreviewTable : ComponentBase
{
    /// <summary>
    /// Gets or sets the heading displayed above the table.
    /// </summary>
    [Parameter]
    public string Heading { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of labels to display in the table.
    /// </summary>
    [Parameter]
    public IReadOnlyList<LabelDto> Labels { get; set; } = Array.Empty<LabelDto>();

    private static string GetColourIconStyle(string colour)
    {
        var normalised = NormaliseHexColour(colour);
        return $"color: #{normalised};";
    }

    private static string GetColourChipStyle(string colour)
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