using System.Net;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>Probes GitHub user API to see whether an access token is still accepted.</summary>
public sealed class HostedGitHubSessionAccessValidator(
    IHttpClientFactory httpClientFactory,
    ILogger<HostedGitHubSessionAccessValidator> logger) : IHostedGitHubSessionAccessValidator
{
    /// <inheritdoc/>
    public async Task<bool> IsAccessTokenAcceptedAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var client = httpClientFactory.CreateClient(HostedGitHubHttpClientNames.ApiClient);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/user");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Hosted session access validation failed.");
            return false;
        }
    }
}
