using System.Collections.Concurrent;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Domain.Entities.PmWorkflow;
using SoloDevBoard.Domain.Entities.Triage;

namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Provides a default implementation of <see cref="IProjectItemCatalogueService"/>.</summary>
public sealed class ProjectItemCatalogueService : IProjectItemCatalogueService
{
    private readonly IGitHubService _gitHubService;
    private readonly ConcurrentDictionary<string, Task<ProjectBoardItemCatalogueDto>> _catalogueByProjectId =
        new(StringComparer.Ordinal);

    /// <summary>Initialises a new instance of the <see cref="ProjectItemCatalogueService"/> class.</summary>
    /// <param name="gitHubService">The GitHub service used to retrieve and update project board items.</param>
    public ProjectItemCatalogueService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Successful catalogues are cached for the DI scope so occupancy and recommendations share one Projects v2 round-trip.
    /// Failed loads are not cached, so Retry can fetch again.
    /// </remarks>
    public async Task<ProjectBoardItemCatalogueDto> GetCatalogueAsync(string projectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_catalogueByProjectId.TryGetValue(projectId, out var existing))
            {
                return await existing.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            var completion = new TaskCompletionSource<ProjectBoardItemCatalogueDto>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_catalogueByProjectId.TryAdd(projectId, completion.Task))
            {
                continue;
            }

            try
            {
                var catalogue = await _gitHubService
                    .GetProjectBoardItemsAsync(projectId, cancellationToken)
                    .ConfigureAwait(false);
                var mapped = MapCatalogue(catalogue);
                completion.SetResult(mapped);
                return mapped;
            }
            catch (Exception exception)
            {
                _catalogueByProjectId.TryRemove(projectId, out _);
                completion.SetException(exception);
                throw;
            }
        }
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

        _catalogueByProjectId.TryRemove(projectId, out _);
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

        _catalogueByProjectId.TryRemove(projectId, out _);
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
            item.ActivityTimestamp,
            item.UsedItemUpdatedAtFallback);
    }
}
