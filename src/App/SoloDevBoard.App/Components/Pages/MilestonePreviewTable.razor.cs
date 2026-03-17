using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.Migration;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>
/// Code-behind for the MilestonePreviewTable component.
/// </summary>
public partial class MilestonePreviewTable : ComponentBase
{
    /// <summary>
    /// Gets or sets the heading displayed above the table.
    /// </summary>
    [Parameter]
    public string Heading { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of milestones to display in the table.
    /// </summary>
    [Parameter]
    public IReadOnlyList<MilestoneDto> Milestones { get; set; } = Array.Empty<MilestoneDto>();

    private static string FormatDueDate(DateTimeOffset? dueOn)
        => dueOn?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "No due date";

    private static string FormatDescription(string description)
        => string.IsNullOrWhiteSpace(description) ? "No description" : description;
}
