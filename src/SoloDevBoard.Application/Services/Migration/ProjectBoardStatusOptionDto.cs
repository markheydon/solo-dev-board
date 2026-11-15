namespace SoloDevBoard.Application.Services.Migration;

/// <summary>Represents a Projects v2 Status option in migration preview and apply results.</summary>
/// <param name="Id">The GitHub node identifier for the option, when known.</param>
/// <param name="Name">The display name of the option.</param>
/// <param name="Colour">The GitHub single-select colour enum value.</param>
/// <param name="Description">The plain-text description of the option.</param>
/// <param name="Order">The zero-based display order of the option.</param>
public sealed record ProjectBoardStatusOptionDto(
    string Id,
    string Name,
    string Colour,
    string Description,
    int Order);
