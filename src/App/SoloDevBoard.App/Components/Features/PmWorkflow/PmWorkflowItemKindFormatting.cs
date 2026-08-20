using MudBlazor;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Components.Features.PmWorkflow;

/// <summary>Shared Issue and pull request chip styling for PM Workflow panels.</summary>
public static class PmWorkflowItemKindFormatting
{
    /// <summary>Filter value that includes both issues and pull requests.</summary>
    public const string AllTypesFilter = "";

    /// <summary>Filter value for issues only.</summary>
    public const string IssueTypeFilter = "issue";

    /// <summary>Filter value for pull requests only.</summary>
    public const string PullRequestTypeFilter = "pr";

    /// <summary>Returns the display label for an item kind chip.</summary>
    /// <param name="itemType">The work item type.</param>
    /// <returns><c>Issue</c> or <c>Pull request</c>.</returns>
    public static string FormatLabel(PmWorkItemTypeDto itemType)
        => itemType == PmWorkItemTypeDto.PullRequest ? "Pull request" : "Issue";

    /// <summary>Returns the MudBlazor colour for an item kind chip.</summary>
    /// <param name="itemType">The work item type.</param>
    /// <returns><see cref="Color.Warning"/> for pull requests; otherwise, <see cref="Color.Info"/>.</returns>
    public static Color FormatChipColor(PmWorkItemTypeDto itemType)
        => itemType == PmWorkItemTypeDto.PullRequest ? Color.Warning : Color.Info;

    /// <summary>Returns whether a work item matches the selected type filter.</summary>
    /// <param name="itemType">The work item type.</param>
    /// <param name="selectedTypeFilter">The selected filter value.</param>
    /// <returns><see langword="true" /> when the item should be shown.</returns>
    public static bool MatchesTypeFilter(PmWorkItemTypeDto itemType, string selectedTypeFilter)
    {
        if (selectedTypeFilter == IssueTypeFilter && itemType != PmWorkItemTypeDto.Issue)
        {
            return false;
        }

        if (selectedTypeFilter == PullRequestTypeFilter && itemType != PmWorkItemTypeDto.PullRequest)
        {
            return false;
        }

        return true;
    }
}
