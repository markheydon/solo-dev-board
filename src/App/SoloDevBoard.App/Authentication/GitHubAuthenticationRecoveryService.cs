using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.App.Authentication;

/// <summary>Redirects users to hosted or PAT recovery routes when GitHub authentication fails.</summary>
public sealed class GitHubAuthenticationRecoveryService(
    NavigationManager navigationManager,
    IOptions<GitHubAuthOptions> authOptions) : IGitHubAuthenticationRecoveryService
{
    private readonly NavigationManager _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));

    /// <inheritdoc/>
    public bool TryInitiateRecovery(Exception exception, string? returnUrl = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_authOptions.HostedSignInEnabled)
        {
            return TryInitiateHostedRecovery(exception, returnUrl);
        }

        return TryInitiatePatRecovery(exception, returnUrl);
    }

    private bool TryInitiateHostedRecovery(Exception exception, string? returnUrl)
    {
        if (exception is not HostedAuthenticationRequiredException)
        {
            return false;
        }

        var recoveryUrl = QueryHelpers.AddQueryString(
            "/auth/session-expired",
            "returnUrl",
            GetSafeReturnUrl(returnUrl ?? _navigationManager.ToBaseRelativePath(_navigationManager.Uri)));

        _navigationManager.NavigateTo(recoveryUrl, forceLoad: true);
        return true;
    }

    private bool TryInitiatePatRecovery(Exception exception, string? returnUrl)
    {
        if (exception is not GitHubPatConnectivityRequiredException)
        {
            return false;
        }

        var recoveryUrl = PatConnectivityErrorRoutes.BuildErrorUrl(
            PatConnectivityErrorRoutes.TokenRejected,
            GetSafeReturnUrl(returnUrl ?? _navigationManager.ToBaseRelativePath(_navigationManager.Uri)));

        _navigationManager.NavigateTo(recoveryUrl, forceLoad: true);
        return true;
    }

    private static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteUri))
        {
            return GetSafeReturnUrl(absoluteUri.PathAndQuery);
        }

        var normalisedReturnUrl = returnUrl.StartsWith("/", StringComparison.Ordinal)
            ? returnUrl
            : $"/{returnUrl.TrimStart('/')}";

        if (normalisedReturnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        return normalisedReturnUrl;
    }
}
