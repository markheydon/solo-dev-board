namespace SoloDevBoard.Application.Authentication;

/// <summary>Checks whether GitHub still accepts an interactive session access token.</summary>
public interface IHostedGitHubSessionAccessValidator
{
    /// <summary>True when GitHub accepts the token for a user read.</summary>
    Task<bool> IsAccessTokenAcceptedAsync(string accessToken, CancellationToken cancellationToken = default);
}
