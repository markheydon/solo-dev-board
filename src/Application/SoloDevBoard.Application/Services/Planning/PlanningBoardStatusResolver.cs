namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Resolves discovered planning-board Status options by display name.</summary>
public static class PlanningBoardStatusResolver
{
    /// <summary>Status option name for unstarted work.</summary>
    public const string TodoStatusName = "Todo";

    /// <summary>
    /// Returns the Status option whose display name matches <paramref name="statusName"/>.
    /// </summary>
    /// <param name="statusOptions">Status options discovered on the selected board.</param>
    /// <param name="statusName">The Status display name to resolve.</param>
    /// <returns>The matching Status option.</returns>
    /// <exception cref="InvalidOperationException">The board does not expose the requested Status option.</exception>
    public static ProjectBoardStatusOptionDto ResolveStatusOption(
        IReadOnlyList<ProjectBoardStatusOptionDto> statusOptions,
        string statusName)
    {
        ArgumentNullException.ThrowIfNull(statusOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(statusName);

        var option = statusOptions.FirstOrDefault(candidate =>
            candidate.Name.Equals(statusName, StringComparison.OrdinalIgnoreCase));

        if (option is null)
        {
            throw new InvalidOperationException(
                $"The planning board does not expose a {statusName} Status option.");
        }

        return option;
    }
}
