using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Services.Workflows;

/// <summary>Provides workflow template operations.</summary>
public interface IWorkflowTemplateService
{
    /// <summary>Retrieves all available workflow templates.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of available workflow templates.</returns>
    Task<IReadOnlyList<WorkflowTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies the selected workflow template to the specified repository.</summary>
    /// <param name="owner">The GitHub account owner login.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="templateId">The identifier of the workflow template to apply.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The workflow template that was applied.</returns>
    Task<WorkflowTemplate> ApplyTemplateAsync(string owner, string repo, int templateId, CancellationToken cancellationToken = default);
}
