namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Represents the apply result for a single repository.</summary>
/// <param name="RepositoryFullName">The owner/repository full name.</param>
/// <param name="Action">The apply action taken for the repository.</param>
/// <param name="ErrorMessage">The repository-specific error message when apply fails.</param>
public sealed record ActionsTemplateRepositoryResultDto(
    string RepositoryFullName,
    string Action,
    string? ErrorMessage)
{
    /// <summary>Gets a value indicating whether the apply operation failed for this repository.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
