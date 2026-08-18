namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Describes a partial failure while loading PM work items for one repository.</summary>
/// <param name="RepositoryFullName">The repository in <c>owner/name</c> form.</param>
/// <param name="Message">A human-readable summary of what failed.</param>
/// <param name="HttpStatusCode">The HTTP status code when the failure originated from the GitHub API.</param>
public sealed record PmRepositoryCatalogueFailureDto(
    string RepositoryFullName,
    string Message,
    int? HttpStatusCode);
