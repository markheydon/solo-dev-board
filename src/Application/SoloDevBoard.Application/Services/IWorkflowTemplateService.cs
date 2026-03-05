using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Provides workflow template operations.</summary>
public interface IWorkflowTemplateService
{
    Task<IReadOnlyList<WorkflowTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<WorkflowTemplate> ApplyTemplateAsync(string owner, string repo, int templateId, CancellationToken cancellationToken = default);
}
