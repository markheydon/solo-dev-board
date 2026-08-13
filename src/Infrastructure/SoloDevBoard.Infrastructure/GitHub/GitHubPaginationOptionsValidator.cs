using Microsoft.Extensions.Options;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Validates <see cref="GitHubPaginationOptions"/> at application startup.</summary>
public sealed class GitHubPaginationOptionsValidator : IValidateOptions<GitHubPaginationOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, GitHubPaginationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.WorkflowRunsMaxPages < 1)
        {
            failures.Add($"{nameof(GitHubPaginationOptions.WorkflowRunsMaxPages)} must be at least 1.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
