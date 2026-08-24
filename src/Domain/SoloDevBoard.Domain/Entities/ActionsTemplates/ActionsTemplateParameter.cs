namespace SoloDevBoard.Domain.Entities.ActionsTemplates;

/// <summary>Represents a configurable parameter for an Actions template.</summary>
public sealed record ActionsTemplateParameter
{
    /// <summary>Gets the parameter name used for template substitution.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the display label shown in the parameter editor.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Gets the help text describing the parameter.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the default value applied when the parameter is optional.</summary>
    public string DefaultValue { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the parameter must be provided before apply.</summary>
    public bool IsRequired { get; init; }
}
