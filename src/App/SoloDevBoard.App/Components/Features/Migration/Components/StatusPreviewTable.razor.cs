using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.Migration;

namespace SoloDevBoard.App.Components.Features.Migration.Components;

/// <summary>Renders a preview table for Projects v2 Status column options.</summary>
public partial class StatusPreviewTable : ComponentBase
{
    /// <summary>Gets or sets the table heading.</summary>
    [Parameter]
    public string Heading { get; set; } = string.Empty;

    /// <summary>Gets or sets the Status options to display.</summary>
    [Parameter]
    public IReadOnlyList<ProjectBoardStatusOptionDto> StatusOptions { get; set; } = [];
}
