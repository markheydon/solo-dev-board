namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Standard request headers for GitHub REST API (<c>api.github.com</c>) calls.</summary>
public static class GitHubApiHeaders
{
    /// <summary>GitHub REST JSON media type recommended for all <c>api.github.com</c> requests.</summary>
    public const string JsonAcceptMediaType = "application/vnd.github+json";

    /// <summary>GitHub REST API version that includes repository <c>topics</c> on list-repos payloads.</summary>
    public const string ApiVersion = "2022-11-28";

    /// <summary>Applies the standard REST Accept and API version headers to an <see cref="HttpClient"/>.</summary>
    /// <param name="client">The client configured for <c>https://api.github.com</c>.</param>
    public static void ApplyRestDefaults(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        client.DefaultRequestHeaders.Accept.ParseAdd(JsonAcceptMediaType);
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", ApiVersion);
    }
}
