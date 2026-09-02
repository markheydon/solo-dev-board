namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Represents the merged workflow template catalogue, including optional custom-source load errors.</summary>
public sealed record ActionsTemplateCatalogueDto
{
    /// <summary>Gets the workflow templates available for browsing and apply flows.</summary>
    public required IReadOnlyList<ActionsTemplateDto> Templates { get; init; }

    /// <summary>Gets a user-facing error when a custom template source could not be loaded; otherwise, <see langword="null" />.</summary>
    public string? CustomSourceError { get; init; }

    /// <summary>Gets a user-facing warning when some custom workflow files could not be loaded; otherwise, <see langword="null" />.</summary>
    public string? CustomSourceWarning { get; init; }

    /// <summary>Gets workflow file paths that were listed but could not be loaded from the custom source.</summary>
    public IReadOnlyList<string> SkippedWorkflowPaths { get; init; } = [];
}
