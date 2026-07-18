namespace SoloDevBoard.Application.Services.Workflows;

/// <summary>Provides workflow template operations.</summary>
public interface IWorkflowTemplateService
{
    /// <summary>Retrieves all available workflow templates.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of available workflow templates.</returns>
    Task<IReadOnlyList<WorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves detail for a single workflow template, including parameters and YAML preview.</summary>
    /// <param name="templateId">The identifier of the workflow template.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The workflow template detail.</returns>
    Task<WorkflowTemplateDetailDto> GetTemplateDetailAsync(int templateId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves workflow template application status for the specified repositories.</summary>
    /// <param name="templateId">The identifier of the workflow template.</param>
    /// <param name="repositoryFullNames">The repositories in owner/repository format.</param>
    /// <param name="parameterValues">The parameter values used to render the canonical template content.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-repository status results.</returns>
    Task<IReadOnlyList<WorkflowTemplateRepositoryStatusDto>> GetRepositoryStatusesAsync(
        int templateId,
        IReadOnlyList<string> repositoryFullNames,
        IReadOnlyDictionary<string, string> parameterValues,
        CancellationToken cancellationToken = default);

    /// <summary>Applies the selected workflow template to the specified repositories.</summary>
    /// <param name="templateId">The identifier of the workflow template.</param>
    /// <param name="repositoryFullNames">The repositories in owner/repository format.</param>
    /// <param name="parameterValues">The parameter values used to render the template content.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-repository apply results.</returns>
    Task<IReadOnlyList<WorkflowTemplateRepositoryResultDto>> ApplyTemplateAsync(
        int templateId,
        IReadOnlyList<string> repositoryFullNames,
        IReadOnlyDictionary<string, string> parameterValues,
        CancellationToken cancellationToken = default);
}
