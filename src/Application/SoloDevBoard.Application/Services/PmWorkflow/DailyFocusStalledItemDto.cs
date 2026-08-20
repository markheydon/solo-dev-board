namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>A planning-board item that has been in Up Next for the stall threshold or longer.</summary>
/// <param name="Title">The issue or pull request title.</param>
/// <param name="AgeInDays">Whole days since the stall clock started, floored.</param>
/// <param name="Url">The GitHub HTML URL for the linked issue or pull request.</param>
/// <param name="UsedUpdatedAtFallback">
/// Whether age used the item last-updated time because Status-changed-at was unavailable.
/// </param>
public sealed record DailyFocusStalledItemDto(
    string Title,
    int AgeInDays,
    string Url,
    bool UsedUpdatedAtFallback);
