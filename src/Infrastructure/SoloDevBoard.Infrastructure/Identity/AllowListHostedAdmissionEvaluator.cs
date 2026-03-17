using System.Security.Claims;
using Microsoft.Extensions.Options;
using SoloDevBoard.Infrastructure.GitHub;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Evaluates hosted admission using operator-managed user and organisation allow-lists.
/// </summary>
public sealed class AllowListHostedAdmissionEvaluator(
    IOptions<HostedAdmissionControlOptions> admissionOptions,
    IOptions<GitHubAuthOptions> authOptions) : IHostedAdmissionEvaluator
{
    private static readonly char[] ClaimValueSeparators = [',', ';', ' '];

    private readonly HostedAdmissionControlOptions _admissionOptions = admissionOptions?.Value ?? throw new ArgumentNullException(nameof(admissionOptions));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));

    /// <inheritdoc/>
    public HostedAdmissionDecision Evaluate(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity?.IsAuthenticated != true)
        {
            return new HostedAdmissionDecision(false, "Hosted request is not authenticated.");
        }

        if (string.IsNullOrWhiteSpace(_authOptions.HostedOwnerLoginClaimType))
        {
            return new HostedAdmissionDecision(false, "Hosted owner-login claim type configuration is missing.");
        }

        var ownerLogin = principal.FindFirst(_authOptions.HostedOwnerLoginClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(ownerLogin))
        {
            return new HostedAdmissionDecision(false, "Hosted owner-login claim is missing.");
        }

        var allowedUsers = BuildNormalisedSet(_admissionOptions.AllowedUserLogins);
        var allowedOrganisations = BuildNormalisedSet(_admissionOptions.AllowedOrganisationLogins);
        if (allowedUsers.Count == 0 && allowedOrganisations.Count == 0)
        {
            return new HostedAdmissionDecision(false, "Hosted admission allow-lists are empty.");
        }

        if (allowedUsers.Contains(ownerLogin))
        {
            return new HostedAdmissionDecision(true, "Hosted admission allowed by user allow-list.");
        }

        foreach (var organisationLogin in GetOrganisationLogins(principal))
        {
            if (allowedOrganisations.Contains(organisationLogin))
            {
                return new HostedAdmissionDecision(true, "Hosted admission allowed by organisation allow-list.");
            }
        }

        return new HostedAdmissionDecision(false, "Hosted identity is not present in user or organisation allow-lists.");
    }

    private static HashSet<string> BuildNormalisedSet(IEnumerable<string> values)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Add(value.Trim());
            }
        }

        return result;
    }

    private IEnumerable<string> GetOrganisationLogins(ClaimsPrincipal principal)
    {
        var claimType = _admissionOptions.HostedOrganisationLoginsClaimType;
        if (string.IsNullOrWhiteSpace(claimType))
        {
            yield break;
        }

        foreach (var claim in principal.FindAll(claimType))
        {
            var claimValue = claim.Value;
            if (string.IsNullOrWhiteSpace(claimValue))
            {
                continue;
            }

            foreach (var login in claimValue.Split(ClaimValueSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return login;
            }
        }
    }
}