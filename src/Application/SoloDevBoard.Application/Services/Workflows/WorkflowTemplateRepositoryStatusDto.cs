namespace SoloDevBoard.Application.Services.Workflows;

/// <summary>Represents the workflow template status for a single repository.</summary>
/// <param name="RepositoryFullName">The owner/repository full name.</param>
/// <param name="Status">The application status for the workflow template.</param>
/// <param name="StatusDescription">A user-facing description of the repository status.</param>
public sealed record WorkflowTemplateRepositoryStatusDto(
    string RepositoryFullName,
    WorkflowTemplateApplicationStatus Status,
    string StatusDescription);
