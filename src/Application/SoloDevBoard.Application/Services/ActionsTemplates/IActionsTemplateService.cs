namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Provides workflow template operations.</summary>
public interface IActionsTemplateService
{
    /// <summary>Retrieves available workflow templates, optionally merging templates from a custom GitHub source repository.</summary>
    /// <param name="customSourceRepository">An optional source repository in owner/repository format.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The merged workflow template catalogue.</returns>
    Task<ActionsTemplateCatalogueDto> GetTemplatesAsync(string? customSourceRepository = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieves detail for a single workflow template, including parameters and YAML preview.</summary>
    /// <param name="templateId">The stable identifier of the workflow template.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The workflow template detail.</returns>
    Task<ActionsTemplateDetailDto> GetTemplateDetailAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves workflow template application status for the specified repositories.</summary>
    /// <param name="templateId">The stable identifier of the workflow template.</param>
    /// <param name="repositoryFullNames">The repositories in owner/repository format.</param>
    /// <param name="parameterValues">The parameter values used to render the canonical template content.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-repository status results.</returns>
    Task<IReadOnlyList<ActionsTemplateRepositoryStatusDto>> GetRepositoryStatusesAsync(
        string templateId,
        IReadOnlyList<string> repositoryFullNames,
        IReadOnlyDictionary<string, string> parameterValues,
        CancellationToken cancellationToken = default);

    /// <summary>Applies the selected workflow template to the specified repositories.</summary>
    /// <param name="templateId">The stable identifier of the workflow template.</param>
    /// <param name="repositoryFullNames">The repositories in owner/repository format.</param>
    /// <param name="parameterValues">The parameter values used to render the template content.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A read-only list of per-repository apply results.</returns>
    Task<IReadOnlyList<ActionsTemplateRepositoryResultDto>> ApplyTemplateAsync(
        string templateId,
        IReadOnlyList<string> repositoryFullNames,
        IReadOnlyDictionary<string, string> parameterValues,
        CancellationToken cancellationToken = default);
}
