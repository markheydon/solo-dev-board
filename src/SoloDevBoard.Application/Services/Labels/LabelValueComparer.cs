using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Compares GitHub label values for synchronisation and consistency analysis.</summary>
public static class LabelValueComparer
{
    /// <summary>Determines whether two labels have equivalent values for synchronisation purposes.</summary>
    /// <param name="left">The first label to compare.</param>
    /// <param name="right">The second label to compare.</param>
    /// <returns><see langword="true" /> if labels are equivalent; otherwise, <see langword="false" />.</returns>
    public static bool HaveSameValues(Label left, Label right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return NamesMatch(left.Name, right.Name)
            && ColoursMatch(left.Colour, right.Colour)
            && DescriptionsMatch(left.Description, right.Description);
    }

    /// <summary>Determines whether two label names are equivalent.</summary>
    /// <param name="left">The first label name.</param>
    /// <param name="right">The second label name.</param>
    /// <returns><see langword="true" /> when the names match case-insensitively; otherwise, <see langword="false" />.</returns>
    public static bool NamesMatch(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    /// <summary>Determines whether two label colours are equivalent.</summary>
    /// <param name="left">The first colour value.</param>
    /// <param name="right">The second colour value.</param>
    /// <returns><see langword="true" /> when the colours match after normalisation; otherwise, <see langword="false" />.</returns>
    public static bool ColoursMatch(string left, string right)
        => string.Equals(NormaliseColour(left), NormaliseColour(right), StringComparison.OrdinalIgnoreCase);

    /// <summary>Determines whether two label descriptions are equivalent.</summary>
    /// <param name="left">The first description.</param>
    /// <param name="right">The second description.</param>
    /// <returns><see langword="true" /> when the descriptions match; otherwise, <see langword="false" />.</returns>
    public static bool DescriptionsMatch(string? left, string? right)
        => string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);

    /// <summary>Normalises a GitHub label colour for comparison and display.</summary>
    /// <param name="colour">The colour value to normalise.</param>
    /// <returns>The normalised six-character colour without a leading hash.</returns>
    public static string NormaliseColour(string colour)
    {
        ArgumentNullException.ThrowIfNull(colour);

        var trimmed = colour.Trim();
        if (trimmed.StartsWith('#'))
        {
            return trimmed[1..];
        }

        return trimmed;
    }
}
