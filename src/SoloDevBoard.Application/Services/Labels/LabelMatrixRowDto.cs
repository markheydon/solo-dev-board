namespace SoloDevBoard.Application.Services.Labels;

/// <summary>A consolidated Label Manager grid row spanning selected repositories.</summary>
/// <param name="Name">The label name.</param>
/// <param name="Colour">The hexadecimal colour without a leading <c>#</c>.</param>
/// <param name="Description">The display description, or <see cref="MissingDescriptionDisplay"/> when GitHub has none.</param>
/// <param name="RepositoriesWithLabel">Selected repositories that currently contain the label, in <c>owner/repository</c> form.</param>
/// <param name="MissingRepositories">Selected repositories that currently lack the label, in <c>owner/repository</c> form.</param>
public sealed record LabelMatrixRowDto(
    string Name,
    string Colour,
    string Description,
    IReadOnlyList<string> RepositoriesWithLabel,
    IReadOnlyList<string> MissingRepositories)
{
    /// <summary>Gets the display text used when a label has no GitHub description.</summary>
    public const string MissingDescriptionDisplay = "No description";

    /// <summary>Gets repository names containing the label as readable text.</summary>
    public string RepositoriesWithLabelText => RepositoriesWithLabel.Count == 0
        ? "None"
        : string.Join(", ", RepositoriesWithLabel);

    /// <summary>Gets repository names missing the label as readable text.</summary>
    public string MissingRepositoriesText => MissingRepositories.Count == 0
        ? "None"
        : string.Join(", ", MissingRepositories);
}
