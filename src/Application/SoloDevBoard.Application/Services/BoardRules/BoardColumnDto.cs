using System.Collections.Generic;

namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Represents a board column in the Application layer.</summary>
/// <param name="Id">The column identifier.</param>
/// <param name="Name">The display name of the column.</param>
/// <param name="Order">The display order of the column.</param>
/// <param name="LabelFilters">The label filters used to route issues into this column.</param>
public sealed record BoardColumnDto(
    int Id,
    string Name,
    int Order,
    IReadOnlyList<string> LabelFilters);
