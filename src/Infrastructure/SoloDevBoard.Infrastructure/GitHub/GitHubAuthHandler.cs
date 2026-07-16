using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Infrastructure.Diagnostics;

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

        var isGraphQlRequest = request.RequestUri?.AbsolutePath.Contains("/graphql", StringComparison.OrdinalIgnoreCase) == true;

        // #region agent log
        if (isGraphQlRequest)
        {
            AgentDebugLog.Write(
                "GitHubAuthHandler.cs:SendAsync",
                "GraphQL request auth context",
                new
                {
                    hostedSignInEnabled = _authOptions.HostedSignInEnabled,
                    tokenPrefix = accessToken.Length >= 4 ? accessToken[..4] : accessToken,
                    tokenLength = accessToken.Length,
                    hostedSignInScopes = _authOptions.HostedSignInScopes,
                },
                "A");
        }
        // #endregion

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // #region agent log
        if (isGraphQlRequest)
        {
            AgentDebugLog.Write(
                "GitHubAuthHandler.cs:SendAsync",
                "GraphQL response auth headers",
                new
                {
                    statusCode = (int)response.StatusCode,
                    oauthScopes = GetHeaderValue(response, "X-OAuth-Scopes"),
                    acceptedOAuthScopes = GetHeaderValue(response, "X-Accepted-OAuth-Scopes"),
                    tokenExpiration = GetHeaderValue(response, "GitHub-Authentication-Token-Expiration"),
                },
                "A");
        }
        // #endregion

        if (_authOptions.HostedSignInEnabled && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            throw new HostedAuthenticationRequiredException(
                "GitHub rejected the hosted access token. Sign in again to continue.");
        }

        return response;
    }

    private static string? GetHeaderValue(HttpResponseMessage response, string headerName)
    {
        return response.Headers.TryGetValues(headerName, out var values)
            ? string.Join(", ", values)
            : null;
    }
}
