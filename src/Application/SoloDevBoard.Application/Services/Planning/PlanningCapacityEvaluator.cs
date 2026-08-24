namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Evaluates Iteration Planning active load against the persisted capacity limit.</summary>
public static class PlanningCapacityEvaluator
{
    /// <summary>Counts board items whose Status is Up Next or In Progress.</summary>
    /// <param name="boardItems">Items currently on the selected planning board.</param>
    /// <returns>The active load count.</returns>
    public static int CountActiveLoad(IReadOnlyList<ProjectBoardItemDto> boardItems)
    {
        ArgumentNullException.ThrowIfNull(boardItems);

        return boardItems.Count(static item => DailyFocusBoardStateMapper.IsActiveLoadStatus(item.Status?.Name));
    }

    /// <summary>Resolves a persisted capacity value, falling back to the default when invalid.</summary>
    /// <param name="capacity">The persisted capacity from PM settings.</param>
    /// <returns>A positive capacity limit.</returns>
    public static int ResolveCapacity(int capacity) =>
        capacity > 0 ? capacity : PlanningSettingsDefaults.Capacity;

    /// <summary>
    /// Returns whether active load has reached or exceeded the capacity limit.
    /// </summary>
    /// <param name="activeLoad">The current Up Next plus In Progress count.</param>
    /// <param name="capacity">The persisted capacity from PM settings.</param>
    /// <returns><see langword="true" /> when at or over the limit; otherwise, <see langword="false" />.</returns>
    public static bool IsAtOrOverCapacity(int activeLoad, int capacity) =>
        activeLoad >= ResolveCapacity(capacity);

    /// <summary>
    /// Returns whether adding one more item would exceed the capacity limit.
    /// </summary>
    /// <param name="activeLoad">The current Up Next plus In Progress count.</param>
    /// <param name="capacity">The persisted capacity from PM settings.</param>
    /// <returns><see langword="true" /> when the add would exceed capacity; otherwise, <see langword="false" />.</returns>
    public static bool WouldExceedCapacityAfterAdd(int activeLoad, int capacity) =>
        activeLoad + 1 > ResolveCapacity(capacity);
}
