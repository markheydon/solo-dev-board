using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>
/// Injects GitHub access token authentication into outbound API requests and
/// translates hosted authentication failures from GitHub API responses.
/// </summary>
public sealed class GitHubAuthHandler(
    ICurrentUserContext currentUserContext,
    IOptions<GitHubAuthOptions> authOptions) : DelegatingHandler
{
    private readonly ICurrentUserContext _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var accessToken = _currentUserContext.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("GitHub access token returned by the current user context is empty.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !AuthConfigurationPlaceholders.IsE2ePlaceholder(accessToken))
        {
            response.Dispose();

            if (_authOptions.HostedSignInEnabled)
            {
                throw new HostedAuthenticationRequiredException(
                    "GitHub rejected the hosted access token. Sign in again to continue.");
            }

            throw new GitHubPatConnectivityRequiredException(
                "GitHub rejected the configured personal access token. Update the token and restart the application.");
        }

        return response;
    }
}
