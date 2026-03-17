using SoloDevBoard.Domain.Entities.BoardRules;
using SoloDevBoard.Domain.Entities.Labels;
using SoloDevBoard.Domain.Entities.Milestones;
using SoloDevBoard.Domain.Entities.Repositories;
using SoloDevBoard.Domain.Entities.Triage;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Services.Workflows;

/// <summary>Stub implementation of <see cref="IWorkflowTemplateService"/>.</summary>
public sealed class WorkflowTemplateService : IWorkflowTemplateService
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkflowTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement retrieval of workflow templates from a data store.
        IReadOnlyList<WorkflowTemplate> result = [];
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<WorkflowTemplate> ApplyTemplateAsync(string owner, string repo, int templateId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement applying a workflow template to a repository.
        return Task.FromResult(new WorkflowTemplate { Id = templateId });
    }
}
