namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Helpers for remapping labels on issues and pull requests.</summary>
public static class LabelRemapHelper
{
    /// <summary>
    /// Builds the label set for an item after swapping a source label for a destination label.
    /// </summary>
    /// <param name="currentLabelNames">The label names currently on the item.</param>
    /// <param name="sourceLabelName">The source label to remove.</param>
    /// <param name="destinationLabelName">The destination label to add when absent.</param>
    /// <returns>The updated label names to send to GitHub.</returns>
    public static IReadOnlyList<string> BuildRetaggedLabelNames(
        IReadOnlyList<string> currentLabelNames,
        string sourceLabelName,
        string destinationLabelName)
    {
        ArgumentNullException.ThrowIfNull(currentLabelNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationLabelName);

        var sourceName = sourceLabelName.Trim();
        var destinationName = destinationLabelName.Trim();
        var retaggedLabels = new List<string>(currentLabelNames.Count + 1);
        var destinationPresent = false;

        foreach (var labelName in currentLabelNames)
        {
            if (string.IsNullOrWhiteSpace(labelName))
            {
                continue;
            }

            if (string.Equals(labelName, sourceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(labelName, destinationName, StringComparison.OrdinalIgnoreCase))
            {
                destinationPresent = true;
            }

            retaggedLabels.Add(labelName);
        }

        if (!destinationPresent)
        {
            retaggedLabels.Add(destinationName);
        }

        return retaggedLabels;
    }
}
