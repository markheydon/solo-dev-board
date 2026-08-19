using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.PmWorkflow;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Provides a default implementation of <see cref="IProjectItemCatalogueService"/>.</summary>
public sealed class ProjectItemCatalogueService : IProjectItemCatalogueService
{
    private readonly IGitHubService _gitHubService;

    /// <summary>Initialises a new instance of the <see cref="ProjectItemCatalogueService"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used to retrieve and update project board items.</param>
    public ProjectItemCatalogueService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
    }

    /// <inheritdoc/>
    public async Task<ProjectBoardItemCatalogueDto> GetCatalogueAsync(string projectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var catalogue = await _gitHubService
            .GetProjectBoardItemsAsync(projectId, cancellationToken)
            .ConfigureAwait(false);

        return MapCatalogue(catalogue);
    }

    /// <inheritdoc/>
    public async Task UpdateFocusOrderAsync(
        string projectId,
        string projectItemId,
        string focusOrderFieldId,
        double focusOrder,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectItemId);
        ValidateFocusOrderFieldId(focusOrderFieldId);

        await _gitHubService
            .UpdateProjectBoardItemFocusOrderAsync(projectId, projectItemId, focusOrderFieldId, focusOrder, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearFocusOrderAsync(
        string projectId,
        string projectItemId,
        string focusOrderFieldId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectItemId);
        ValidateFocusOrderFieldId(focusOrderFieldId);

        await _gitHubService
            .ClearProjectBoardItemFocusOrderAsync(projectId, projectItemId, focusOrderFieldId, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void ValidateFocusOrderFieldId(string focusOrderFieldId)
    {
        if (string.IsNullOrWhiteSpace(focusOrderFieldId))
        {
            throw new InvalidOperationException("The project board does not expose a Focus Order field.");
        }
    }

    private static ProjectBoardItemCatalogueDto MapCatalogue(ProjectBoardItemCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        var fieldIds = new ProjectBoardFieldIdsDto(
            catalogue.FieldIds.StatusFieldId,
            catalogue.FieldIds.FocusOrderFieldId);

        var statusOptions = catalogue.StatusOptions
            .Select(static option => new ProjectBoardStatusOptionDto(option.OptionId, option.Name))
            .ToArray();

        var items = catalogue.Items
            .Select(MapItem)
            .ToArray();

        return new ProjectBoardItemCatalogueDto(fieldIds, statusOptions, items);
    }

    private static ProjectBoardItemDto MapItem(ProjectBoardItem item)
    {
        var status = item.Status is null
            ? null
            : new ProjectBoardItemStatusDto(item.Status.OptionId, item.Status.Name);

        var contentType = item.Content.ContentType == TriageItemType.PullRequest
            ? ProjectBoardItemContentTypeDto.PullRequest
            : ProjectBoardItemContentTypeDto.Issue;

        var content = new ProjectBoardItemContentDto(
            contentType,
            item.Content.Number,
            item.Content.RepositoryOwner,
            item.Content.RepositoryName,
            item.Content.Title,
            item.Content.Url);

        return new ProjectBoardItemDto(
            item.ProjectItemId,
            status,
            item.FocusOrder,
            content,
            item.ActivityTimestamp);
    }
}
