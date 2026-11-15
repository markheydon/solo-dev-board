namespace SoloDevBoard.Application.Services.GitHub;

/// <summary>Formats user-facing copy when linked project boards cannot be loaded.</summary>
public static class LinkedProjectBoardVisibility
{
    /// <summary>
    /// Builds a warning message when one or more linked project boards could not be loaded.
    /// Returns <see langword="null" /> when every linked board was readable.
    /// </summary>
    /// <param name="totalLinkedProjectCount">The number of project boards GitHub reports as linked.</param>
    /// <param name="inaccessibleLinkedProjectCount">The number of linked boards that could not be read.</param>
    /// <returns>A warning message, or <see langword="null" /> when no inaccessible boards were reported.</returns>
    public static string? BuildInaccessibleProjectsWarning(int totalLinkedProjectCount, int inaccessibleLinkedProjectCount)
    {
        if (inaccessibleLinkedProjectCount <= 0)
        {
            return null;
        }

        if (totalLinkedProjectCount <= 0)
        {
            totalLinkedProjectCount = inaccessibleLinkedProjectCount;
        }

        var boardNoun = totalLinkedProjectCount == 1 ? "board" : "boards";
        var inaccessibleNoun = inaccessibleLinkedProjectCount == 1 ? "board" : "boards";

        if (inaccessibleLinkedProjectCount >= totalLinkedProjectCount)
        {
            return $"GitHub reports {totalLinkedProjectCount} linked project {boardNoun}, but none could be loaded with the current sign-in. Private user-owned projects are commonly inaccessible to GitHub App sign-in; use PAT mode with the read:project scope or make the project public.";
        }

        return $"GitHub reports {totalLinkedProjectCount} linked project {boardNoun}, but {inaccessibleLinkedProjectCount} {inaccessibleNoun} could not be loaded with the current sign-in. Private user-owned projects are commonly inaccessible to GitHub App sign-in; use PAT mode with the read:project scope or make the project public.";
    }
}
