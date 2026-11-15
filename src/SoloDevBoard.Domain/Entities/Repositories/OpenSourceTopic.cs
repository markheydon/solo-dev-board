namespace SoloDevBoard.Domain.Entities.Repositories;

/// <summary>Canonical GitHub topic slug and helper for open-source project identification.</summary>
public static class OpenSourceTopic
{
    /// <summary>Gets the canonical GitHub topic slug for open-source project repositories.</summary>
    public const string Canonical = "open-source";

    /// <summary>Determines whether the supplied topics indicate an open-source project repository.</summary>
    /// <param name="topics">The GitHub repository topics to evaluate.</param>
    /// <returns><see langword="true"/> when <paramref name="topics"/> includes the canonical slug; otherwise <see langword="false"/>.</returns>
    public static bool IsOpenSource(IReadOnlyList<string>? topics)
    {
        if (topics is null || topics.Count == 0)
        {
            return false;
        }

        return topics.Any(topic => string.Equals(topic, Canonical, StringComparison.OrdinalIgnoreCase));
    }
}
