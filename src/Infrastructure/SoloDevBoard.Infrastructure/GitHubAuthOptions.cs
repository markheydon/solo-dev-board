namespace SoloDevBoard.Infrastructure;

/// <summary>Configuration options for GitHub authentication.</summary>
public sealed class GitHubAuthOptions
{
    public const string SectionName = "GitHubAuth";

    /// <summary>Personal access token for GitHub API authentication.</summary>
    public string PersonalAccessToken { get; set; } = string.Empty;

    /// <summary>GitHub App ID, used when authenticating as a GitHub App.</summary>
    public string GitHubAppId { get; set; } = string.Empty;

    /// <summary>GitHub App private key in PEM format.</summary>
    public string GitHubAppPrivateKey { get; set; } = string.Empty;

    /// <summary>When true, uses GitHub App authentication instead of a personal access token.</summary>
    public bool UseGitHubApp { get; set; }
}
