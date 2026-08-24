namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Ranks <c>priority/</c> labels for PM workflow ordering.</summary>
public static class PlanningPriorityRanker
{
    /// <summary>Compares two priority labels for descending urgency (critical first, none last).</summary>
    /// <param name="left">The left-hand priority label, or <see langword="null"/> for none.</param>
    /// <param name="right">The right-hand priority label, or <see langword="null"/> for none.</param>
    /// <returns>A negative value when <paramref name="left"/> is more urgent than <paramref name="right"/>.</returns>
    public static int ComparePriority(string? left, string? right)
        => GetRank(left).CompareTo(GetRank(right));

    /// <summary>Returns the rank for a priority label where lower values are more urgent.</summary>
    /// <param name="priorityLabel">The priority label name, or <see langword="null"/> for none.</param>
    /// <returns>The rank value.</returns>
    public static int GetRank(string? priorityLabel)
    {
        if (string.IsNullOrWhiteSpace(priorityLabel))
        {
            return 4;
        }

        if (priorityLabel.Equals(PlanningLabelHelpers.CriticalPriorityLabel, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (priorityLabel.Equals(PlanningLabelHelpers.HighPriorityLabel, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (priorityLabel.Equals("priority/medium", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (priorityLabel.Equals("priority/low", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }
}
