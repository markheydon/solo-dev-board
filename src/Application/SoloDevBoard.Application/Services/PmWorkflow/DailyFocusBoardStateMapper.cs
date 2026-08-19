namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Maps a project board item catalogue to Daily Focus occupancy and active-load figures.</summary>
public static class DailyFocusBoardStateMapper
{
    /// <summary>Status option name for committed work that has not started.</summary>
    /// <remarks>Matched by display name, not a compiled Project option identifier (DEC-029).</remarks>
    public const string UpNextStatusName = "Up Next";

    /// <summary>Status option name for work currently in flight.</summary>
    /// <remarks>Matched by display name, not a compiled Project option identifier (DEC-029).</remarks>
    public const string InProgressStatusName = "In Progress";

    /// <summary>Chip label used when a board item has no Status value.</summary>
    public const string NoStatusChipName = "No status";

    /// <summary>
    /// Builds occupancy chips from discovered Status options and computes active load as
    /// <see cref="UpNextStatusName"/> plus <see cref="InProgressStatusName"/>.
    /// </summary>
    /// <param name="statusOptions">Status options discovered on the selected board.</param>
    /// <param name="items">Items currently on the selected board.</param>
    /// <param name="capacity">The persisted planning capacity; values less than 1 fall back to the default.</param>
    /// <returns>The Daily Focus board snapshot.</returns>
    public static DailyFocusBoardStateDto Map(
        IReadOnlyList<ProjectBoardStatusOptionDto> statusOptions,
        IReadOnlyList<ProjectBoardItemDto> items,
        int capacity)
    {
        ArgumentNullException.ThrowIfNull(statusOptions);
        ArgumentNullException.ThrowIfNull(items);

        var resolvedCapacity = capacity > 0 ? capacity : PmSettingsDefaults.Capacity;
        var occupancyOptions = statusOptions.Count > 0
            ? statusOptions
            : DeriveStatusOptionsFromItems(items);

        var occupancy = occupancyOptions
            .Select(option => new DailyFocusOccupancyChipDto(
                option.Name,
                items.Count(item => MatchesStatusOption(item, option))))
            .ToList();

        var unstatusedCount = items.Count(static item => item.Status is null);
        if (unstatusedCount > 0)
        {
            occupancy.Add(new DailyFocusOccupancyChipDto(NoStatusChipName, unstatusedCount));
        }

        var activeLoad = items.Count(static item => IsActiveLoadStatus(item.Status?.Name));

        return new DailyFocusBoardStateDto(occupancy, activeLoad, resolvedCapacity, items.Count);
    }

    /// <summary>
    /// Returns whether the Status name counts toward active load.
    /// </summary>
    /// <param name="statusName">The Status option display name, or <see langword="null"/> when unset.</param>
    /// <returns><see langword="true" /> when the name is Up Next or In Progress; otherwise, <see langword="false" />.</returns>
    public static bool IsActiveLoadStatus(string? statusName)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return false;
        }

        return statusName.Equals(UpNextStatusName, StringComparison.OrdinalIgnoreCase)
            || statusName.Equals(InProgressStatusName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Builds occupancy chips from item Status names when the board did not return option metadata.</summary>
    /// <param name="items">Items currently on the selected board.</param>
    /// <returns>Distinct Status options in first-seen order.</returns>
    private static IReadOnlyList<ProjectBoardStatusOptionDto> DeriveStatusOptionsFromItems(
        IReadOnlyList<ProjectBoardItemDto> items)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var derived = new List<ProjectBoardStatusOptionDto>();

        foreach (var item in items)
        {
            if (item.Status is null
                || string.IsNullOrWhiteSpace(item.Status.Name)
                || !seen.Add(item.Status.Name))
            {
                continue;
            }

            derived.Add(new ProjectBoardStatusOptionDto(item.Status.OptionId, item.Status.Name));
        }

        return derived;
    }

    /// <summary>Returns whether an item belongs to a discovered Status option.</summary>
    /// <param name="item">The board item to match.</param>
    /// <param name="option">The discovered Status option.</param>
    /// <returns><see langword="true" /> when the item Status matches the option identifier or name; otherwise, <see langword="false" />.</returns>
    private static bool MatchesStatusOption(ProjectBoardItemDto item, ProjectBoardStatusOptionDto option)
    {
        if (item.Status is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(option.OptionId)
            && !string.IsNullOrWhiteSpace(item.Status.OptionId))
        {
            return item.Status.OptionId.Equals(option.OptionId, StringComparison.Ordinal);
        }

        return item.Status.Name.Equals(option.Name, StringComparison.OrdinalIgnoreCase);
    }
}
