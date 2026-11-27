namespace SoloDevBoard.Application.Authentication;

/// <summary>Named HTTP clients for hosted GitHub App sign-in.</summary>
public static class HostedGitHubHttpClientNames
{
    /// <summary>GitHub REST API client.</summary>
    public const string ApiClient = "HostedGitHubApiClient";

    /// <summary>GitHub OAuth token endpoint client.</summary>
    public const string OAuthClient = "HostedGitHubOAuthClient";
}
