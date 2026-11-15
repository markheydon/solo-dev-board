namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a failed retag of a single issue or pull request during label remap.</summary>
/// <param name="ItemNumber">The repository-scoped issue or pull request number that could not be retagged.</param>
/// <param name="ErrorMessage">The error message returned for this item.</param>
public sealed record LabelRemapItemErrorDto(int ItemNumber, string ErrorMessage);
