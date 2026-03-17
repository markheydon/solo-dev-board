using System.Security.Claims;
using Microsoft.Extensions.Options;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class AllowListHostedAdmissionEvaluatorTests
{
    [Fact]
    public void Evaluate_UserInAllowList_ReturnsAllowedDecision()
    {
        // Arrange
        var evaluator = CreateEvaluator(
            new HostedAdmissionControlOptions
            {
                AllowedUserLogins = ["markheydon"],
            },
            new GitHubAuthOptions
            {
                HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            });
        var principal = CreatePrincipal(
            isAuthenticated: true,
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"));

        // Act
        var result = evaluator.Evaluate(principal);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_OrganisationInAllowList_ReturnsAllowedDecision()
    {
        // Arrange
        var evaluator = CreateEvaluator(
            new HostedAdmissionControlOptions
            {
                AllowedOrganisationLogins = ["my-org"],
                HostedOrganisationLoginsClaimType = HostedAuthClaimTypes.OrganisationLogins,
            },
            new GitHubAuthOptions
            {
                HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            });
        var principal = CreatePrincipal(
            isAuthenticated: true,
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
            new Claim(HostedAuthClaimTypes.OrganisationLogins, "other-org,my-org"));

        // Act
        var result = evaluator.Evaluate(principal);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_UserMissingFromAllAllowLists_ReturnsDeniedDecision()
    {
        // Arrange
        var evaluator = CreateEvaluator(
            new HostedAdmissionControlOptions
            {
                AllowedUserLogins = ["someone-else"],
                AllowedOrganisationLogins = ["other-org"],
                HostedOrganisationLoginsClaimType = HostedAuthClaimTypes.OrganisationLogins,
            },
            new GitHubAuthOptions
            {
                HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            });
        var principal = CreatePrincipal(
            isAuthenticated: true,
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
            new Claim(HostedAuthClaimTypes.OrganisationLogins, "my-org"));

        // Act
        var result = evaluator.Evaluate(principal);

        // Assert
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_AllowListsEmpty_ReturnsDeniedDecision()
    {
        // Arrange
        var evaluator = CreateEvaluator(
            new HostedAdmissionControlOptions(),
            new GitHubAuthOptions
            {
                HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            });
        var principal = CreatePrincipal(
            isAuthenticated: true,
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"));

        // Act
        var result = evaluator.Evaluate(principal);

        // Assert
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_OwnerLoginClaimMissing_ReturnsDeniedDecision()
    {
        // Arrange
        var evaluator = CreateEvaluator(
            new HostedAdmissionControlOptions
            {
                AllowedUserLogins = ["markheydon"],
            },
            new GitHubAuthOptions
            {
                HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            });
        var principal = CreatePrincipal(isAuthenticated: true);

        // Act
        var result = evaluator.Evaluate(principal);

        // Assert
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_UnauthenticatedPrincipal_ReturnsDeniedDecision()
    {
        // Arrange
        var evaluator = CreateEvaluator(
            new HostedAdmissionControlOptions
            {
                AllowedUserLogins = ["markheydon"],
            },
            new GitHubAuthOptions
            {
                HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            });
        var principal = CreatePrincipal(
            isAuthenticated: false,
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"));

        // Act
        var result = evaluator.Evaluate(principal);

        // Assert
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_OrganisationClaimUsesMixedSeparators_ReturnsAllowedDecision()
    {
        // Arrange
        var evaluator = CreateEvaluator(
            new HostedAdmissionControlOptions
            {
                AllowedOrganisationLogins = ["my-org"],
                HostedOrganisationLoginsClaimType = HostedAuthClaimTypes.OrganisationLogins,
            },
            new GitHubAuthOptions
            {
                HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            });
        var principal = CreatePrincipal(
            isAuthenticated: true,
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"),
            new Claim(HostedAuthClaimTypes.OrganisationLogins, "other-org;my-org another-org"));

        // Act
        var result = evaluator.Evaluate(principal);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_AllowListValuesUseDifferentCasing_ReturnsAllowedDecision()
    {
        // Arrange
        var evaluator = CreateEvaluator(
            new HostedAdmissionControlOptions
            {
                AllowedUserLogins = ["MarkHeydon"],
            },
            new GitHubAuthOptions
            {
                HostedOwnerLoginClaimType = HostedAuthClaimTypes.OwnerLogin,
            });
        var principal = CreatePrincipal(
            isAuthenticated: true,
            new Claim(HostedAuthClaimTypes.OwnerLogin, "markheydon"));

        // Act
        var result = evaluator.Evaluate(principal);

        // Assert
        Assert.True(result.IsAllowed);
    }

    private static AllowListHostedAdmissionEvaluator CreateEvaluator(
        HostedAdmissionControlOptions admissionOptions,
        GitHubAuthOptions authOptions)
    {
        return new AllowListHostedAdmissionEvaluator(Options.Create(admissionOptions), Options.Create(authOptions));
    }

    private static ClaimsPrincipal CreatePrincipal(bool isAuthenticated, params Claim[] claims)
    {
        var authenticationType = isAuthenticated ? "Hosted" : string.Empty;
        var identity = new ClaimsIdentity(claims, authenticationType);

        return new ClaimsPrincipal(identity);
    }
}