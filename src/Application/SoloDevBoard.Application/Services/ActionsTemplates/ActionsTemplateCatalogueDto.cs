namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Represents the merged workflow template catalogue, including optional custom-source load errors.</summary>
/// <param name="Templates">The workflow templates available for browsing and apply flows.</param>
/// <param name="CustomSourceError">A user-facing error when a custom template source could not be loaded; otherwise, <see langword="null" />.</param>
public sealed record ActionsTemplateCatalogueDto(
    IReadOnlyList<ActionsTemplateDto> Templates,
    string? CustomSourceError);
