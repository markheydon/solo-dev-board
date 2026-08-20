namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a failed label delete during recommended taxonomy apply.</summary>
/// <param name="LabelName">The label name that could not be deleted.</param>
/// <param name="ErrorMessage">The error message returned for this label.</param>
public sealed record RecommendedTaxonomyLabelDeleteErrorDto(string LabelName, string ErrorMessage);
