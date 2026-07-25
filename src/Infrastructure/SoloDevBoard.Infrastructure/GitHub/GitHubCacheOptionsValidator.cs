using Microsoft.Extensions.Options;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Validates <see cref="GitHubCacheOptions"/> at application startup.</summary>
public sealed class GitHubCacheOptionsValidator : IValidateOptions<GitHubCacheOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, GitHubCacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.RepositoriesTtlSeconds < 1)
        {
            failures.Add($"{nameof(GitHubCacheOptions.RepositoriesTtlSeconds)} must be at least 1 second.");
        }

        if (options.LabelsTtlSeconds < 1)
        {
            failures.Add($"{nameof(GitHubCacheOptions.LabelsTtlSeconds)} must be at least 1 second.");
        }

        if (options.MilestonesTtlSeconds < 1)
        {
            failures.Add($"{nameof(GitHubCacheOptions.MilestonesTtlSeconds)} must be at least 1 second.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
