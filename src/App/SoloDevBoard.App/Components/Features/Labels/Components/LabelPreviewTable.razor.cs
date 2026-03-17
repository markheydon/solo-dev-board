using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.Labels;

namespace SoloDevBoard.App.Components.Features.Labels.Components;

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

    private static string GetColourChipStyle(string colour) => LabelColourStyleHelper.GetColourChipStyle(colour);
}