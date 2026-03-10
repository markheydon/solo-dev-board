namespace SoloDevBoard.Application.Services;

/// <summary>Represents a workflow run item returned by audit dashboard operations.</summary>
/// <param name="WorkflowName">The workflow name.</param>
/// <param name="Status">The workflow run status.</param>
/// <param name="Conclusion">The workflow run conclusion.</param>
/// <param name="HtmlUrl">The web URL of the workflow run.</param>
/// <param name="RepositoryFullName">The fully-qualified repository name in owner/name format.</param>
public sealed record WorkflowRunDto(
    string WorkflowName,
    string Status,
    string Conclusion,
    string HtmlUrl,
    string RepositoryFullName);
