namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Represents a workflow template exposed to the presentation layer.</summary>
/// <param name="Id">The unique identifier of the workflow template.</param>
/// <param name="Name">The display name of the workflow template.</param>
/// <param name="Description">The description shown in the template browser.</param>
/// <param name="Category">The template category used for browsing and filtering.</param>
/// <param name="Tags">The tags associated with the workflow template.</param>
/// <param name="WorkflowFilePath">The relative workflow file path applied to repositories.</param>
/// <param name="TriggerDescription">A short description of when the workflow runs.</param>
/// <param name="CreatedAt">The date and time when the template was created.</param>
public sealed record ActionsTemplateDto(
    string Id,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<string> Tags,
    string WorkflowFilePath,
    string TriggerDescription,
    DateTimeOffset CreatedAt);
