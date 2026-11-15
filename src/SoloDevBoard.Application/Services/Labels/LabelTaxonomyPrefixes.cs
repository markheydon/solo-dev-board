namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Shared prefix rules for label taxonomy operations.</summary>
public static class LabelTaxonomyPrefixes
{
    /// <summary>The prefix used for repository-specific area labels.</summary>
    public const string AreaPrefix = "area/";

    /// <summary>Determines whether a label name uses the area prefix.</summary>
    /// <param name="labelName">The label name to evaluate.</param>
    /// <returns><see langword="true" /> when the name starts with <see cref="AreaPrefix"/>; otherwise, <see langword="false" />.</returns>
    public static bool IsAreaLabel(string labelName)
        => labelName.StartsWith(AreaPrefix, StringComparison.OrdinalIgnoreCase);
}
