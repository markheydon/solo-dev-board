namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a label at the Application-to-App boundary.</summary>
/// <param name="Name">The label name.</param>
/// <param name="Colour">The hexadecimal colour value of the label.</param>
/// <param name="Description">The label description.</param>
/// <param name="RepositoryName">The short repository name the label belongs to.</param>
public sealed record LabelDto(string Name, string Colour, string Description, string RepositoryName);
