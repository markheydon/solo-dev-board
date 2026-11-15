namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents a failed label delete during a bulk delete operation.</summary>
/// <param name="LabelName">The label name that could not be deleted.</param>
/// <param name="RepositoryFullName">The owner/repository full name where deletion failed.</param>
/// <param name="ErrorMessage">The error message returned for this label and repository.</param>
public sealed record LabelBulkDeleteErrorDto(string LabelName, string RepositoryFullName, string ErrorMessage);
