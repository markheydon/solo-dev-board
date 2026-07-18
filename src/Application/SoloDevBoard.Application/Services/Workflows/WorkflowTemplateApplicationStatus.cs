namespace SoloDevBoard.Application.Services.Workflows;

/// <summary>Represents the application status of a workflow template in a repository.</summary>
public enum WorkflowTemplateApplicationStatus
{
    /// <summary>The workflow file is not present in the repository.</summary>
    NotApplied = 0,

    /// <summary>The workflow file matches the canonical template content.</summary>
    Applied = 1,

    /// <summary>The workflow file exists but differs from the canonical template content.</summary>
    Drifted = 2,
}
