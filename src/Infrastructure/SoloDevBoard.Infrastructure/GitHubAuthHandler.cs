using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace SoloDevBoard.Infrastructure;

/// <summary>
/// Injects GitHub PAT authentication into outbound API requests.
/// </summary>
public sealed class GitHubAuthHandler(IOptions<GitHubAuthOptions> authOptions) : DelegatingHandler
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(_authOptions.PersonalAccessToken))
        {
            throw new ArgumentException(
                "GitHub personal access token is not configured.",
                $"{GitHubAuthOptions.SectionName}:{nameof(GitHubAuthOptions.PersonalAccessToken)}");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authOptions.PersonalAccessToken);

        return base.SendAsync(request, cancellationToken);
    }
}