namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Computes Focus Order assignment for Iteration Planning Up Next batches.</summary>
public static class PlanningFocusOrderSequencer
{
    /// <summary>The story type label name.</summary>
    public const string StoryTypeLabel = "type/story";

    /// <summary>The enabler type label name.</summary>
    public const string EnablerTypeLabel = "type/enabler";

    /// <summary>The test type label name.</summary>
    public const string TestTypeLabel = "type/test";

    /// <summary>
    /// Returns whether a work item should receive sequential Focus Order when moved to Up Next.
    /// Feature and Epic cards skip Focus Order; stories, enablers, and tests receive it.
    /// </summary>
    /// <param name="labels">Label names on the work item.</param>
    /// <returns><see langword="true" /> when Focus Order should be assigned; otherwise, <see langword="false" />.</returns>
    public static bool ShouldAssignFocusOrder(IReadOnlyList<string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        var typeLabel = PmLabelHelpers.ParseTypeLabel(labels);
        return typeLabel is StoryTypeLabel or EnablerTypeLabel or TestTypeLabel;
    }

    /// <summary>
    /// Returns the next sequential Focus Order value after existing Up Next items on the board.
    /// </summary>
    /// <param name="upNextItems">Board items currently in Up Next.</param>
    /// <returns>The next Focus Order value, starting at 1 when no values exist.</returns>
    public static double GetNextFocusOrder(IReadOnlyList<ProjectBoardItemDto> upNextItems)
    {
        ArgumentNullException.ThrowIfNull(upNextItems);

        var maxExisting = upNextItems
            .Where(static item => item.FocusOrder.HasValue)
            .Select(static item => item.FocusOrder!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return maxExisting + 1;
    }
}
