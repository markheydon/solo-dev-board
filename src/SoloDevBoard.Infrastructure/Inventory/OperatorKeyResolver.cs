using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.OpenSource;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>Resolves inventory partition keys for web requests.</summary>
public sealed class OperatorKeyResolver(
    ICurrentUserContext currentUserContext,
    IOptions<GitHubAuthOptions> authOptions,
    IHttpContextAccessor httpContextAccessor) : IOperatorKeyResolver
{
    private readonly ICurrentUserContext _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public Task<string> ResolveCurrentOperatorKeyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_authOptions.HostedSignInEnabled)
        {
            var installationId = ResolveInstallationId();
            if (installationId is null)
            {
                throw new InvalidOperationException("Hosted inventory requires an installation id.");
            }

            return Task.FromResult(FormattableString.Invariant($"installation:{installationId}"));
        }

        var ownerLogin = _currentUserContext.OwnerLogin;
        return Task.FromResult(FormattableString.Invariant($"user:{ownerLogin}"));
    }

    private long? ResolveInstallationId()
    {
        var claimType = _authOptions.HostedInstallationIdClaimType;
        if (!string.IsNullOrWhiteSpace(claimType))
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
            if (long.TryParse(claim, out var fromClaim))
            {
                return fromClaim;
            }
        }

        if (long.TryParse(_authOptions.InstallationId, out var fromConfig))
        {
            return fromConfig;
        }

        return null;
    }
}