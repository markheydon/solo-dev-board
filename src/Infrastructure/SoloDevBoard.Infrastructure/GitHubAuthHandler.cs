using System.Net.Http.Headers;
using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Infrastructure;

/// <summary>
/// Injects GitHub access token authentication into outbound API requests.
/// </summary>
public sealed class GitHubAuthHandler(ICurrentUserContext currentUserContext) : DelegatingHandler
{
    private readonly ICurrentUserContext _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var accessToken = _currentUserContext.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("GitHub access token returned by the current user context is empty.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return base.SendAsync(request, cancellationToken);
    }
}