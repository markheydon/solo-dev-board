namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Represents a configurable workflow template parameter exposed to the presentation layer.</summary>
/// <param name="Name">The parameter name used for template substitution.</param>
/// <param name="Label">The display label shown in the parameter editor.</param>
/// <param name="Description">The help text describing the parameter.</param>
/// <param name="DefaultValue">The default value applied when the parameter is optional.</param>
/// <param name="IsRequired">A value indicating whether the parameter must be provided before apply.</param>
public sealed record ActionsTemplateParameterDto(
    string Name,
    string Label,
    string Description,
    string DefaultValue,
    bool IsRequired);
