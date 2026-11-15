using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Compares repository labels against a recommended taxonomy.</summary>
public static class LabelConsistencyAnalyser
{
    /// <summary>
    /// Builds warnings for labels that are missing from a repository or that differ in colour or description.
    /// Extra repository labels that are not in the taxonomy are ignored.
    /// </summary>
    /// <param name="repositoryFullName">The fully-qualified repository name in owner/name format.</param>
    /// <param name="existingLabels">The labels currently present in the repository.</param>
    /// <param name="taxonomyLabels">The canonical taxonomy labels to compare against.</param>
    /// <returns>A read-only list of consistency warnings for the repository.</returns>
    public static IReadOnlyList<LabelConsistencyWarningDto> Analyse(
        string repositoryFullName,
        IReadOnlyList<Label> existingLabels,
        IReadOnlyList<LabelDto> taxonomyLabels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryFullName);
        ArgumentNullException.ThrowIfNull(existingLabels);
        ArgumentNullException.ThrowIfNull(taxonomyLabels);

        var existingByName = existingLabels.ToDictionary(static label => label.Name, StringComparer.OrdinalIgnoreCase);
        var warnings = new List<LabelConsistencyWarningDto>();

        foreach (var taxonomyLabel in taxonomyLabels)
        {
            if (!existingByName.TryGetValue(taxonomyLabel.Name, out var existing))
            {
                warnings.Add(new LabelConsistencyWarningDto(
                    repositoryFullName,
                    taxonomyLabel.Name,
                    LabelConsistencyWarningKind.Missing,
                    "Missing from the repository."));
                continue;
            }

            var colourMatches = LabelValueComparer.ColoursMatch(taxonomyLabel.Colour, existing.Colour);
            var descriptionMatches = LabelValueComparer.DescriptionsMatch(taxonomyLabel.Description, existing.Description);

            if (colourMatches && descriptionMatches)
            {
                continue;
            }

            warnings.Add(new LabelConsistencyWarningDto(
                repositoryFullName,
                taxonomyLabel.Name,
                LabelConsistencyWarningKind.Divergent,
                BuildDivergentDetail(taxonomyLabel, existing, colourMatches, descriptionMatches)));
        }

        return warnings
            .OrderBy(static warning => warning.LabelName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildDivergentDetail(
        LabelDto taxonomyLabel,
        Label existing,
        bool colourMatches,
        bool descriptionMatches)
    {
        if (!colourMatches && !descriptionMatches)
        {
            return $"Colour and description differ (expected #{LabelValueComparer.NormaliseColour(taxonomyLabel.Colour)}, found #{LabelValueComparer.NormaliseColour(existing.Colour)}).";
        }

        if (!colourMatches)
        {
            return $"Colour differs (expected #{LabelValueComparer.NormaliseColour(taxonomyLabel.Colour)}, found #{LabelValueComparer.NormaliseColour(existing.Colour)}).";
        }

        return "Description differs from the taxonomy.";
    }
}
