using SoloDevBoard.Domain.Entities;
using SoloDevBoard.Infrastructure;

namespace SoloDevBoard.Application.Services;

/// <summary>Stub implementation of <see cref="IWorkflowTemplateService"/>.</summary>
public sealed class WorkflowTemplateService : IWorkflowTemplateService
{
    private readonly IGitHubService _gitHubService;

    public WorkflowTemplateService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

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
