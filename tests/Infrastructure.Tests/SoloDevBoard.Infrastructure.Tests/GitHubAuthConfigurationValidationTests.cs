using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class GitHubAuthConfigurationValidationTests
{
    [Fact]
    public void Validate_PatModeWithoutToken_ThrowsInvalidOperationException()
    {
        var authOptions = new GitHubAuthOptions();
        var admissionOptions = new HostedAdmissionControlOptions();

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("gh-pat", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_PatModeWithToken_DoesNotThrow()
    {
        var authOptions = new GitHubAuthOptions
        {
            PersonalAccessToken = "ghp_test-token",
        };
        var admissionOptions = new HostedAdmissionControlOptions();

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        act();
    }

    [Fact]
    public void Validate_HostedModeWithoutCredentials_ThrowsInvalidOperationException()
    {
        var authOptions = new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
        };
        var admissionOptions = new HostedAdmissionControlOptions();

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("gh-app-client-id", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gh-app-client-secret", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_HostedModeWithoutAllowList_ThrowsInvalidOperationException()
    {
        var authOptions = new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            HostedGitHubAppClientId = "client-id",
            HostedGitHubAppClientSecret = "client-secret",
        };
        var admissionOptions = new HostedAdmissionControlOptions
        {
            Enabled = true,
        };

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("allowed-user-logins", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_HostedModeWithAllowList_DoesNotThrow()
    {
        var authOptions = new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            HostedGitHubAppClientId = "client-id",
            HostedGitHubAppClientSecret = "client-secret",
        };
        var admissionOptions = new HostedAdmissionControlOptions
        {
            Enabled = true,
            AllowedUserLogins = "markheydon",
        };

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        act();
    }

    [Fact]
    public void Validate_PatModeWithHostedCredentialsSet_ThrowsInvalidOperationException()
    {
        var authOptions = new GitHubAuthOptions
        {
            PersonalAccessToken = "ghp_test-token",
            HostedGitHubAppClientId = "client-id",
        };
        var admissionOptions = new HostedAdmissionControlOptions();

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("gh-app-client-id", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_HostedModeWithPatSet_ThrowsInvalidOperationException()
    {
        var authOptions = new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            PersonalAccessToken = "ghp_test-token",
            HostedGitHubAppClientId = "client-id",
            HostedGitHubAppClientSecret = "client-secret",
        };
        var admissionOptions = new HostedAdmissionControlOptions
        {
            Enabled = true,
            AllowedUserLogins = "markheydon",
        };

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("gh-pat", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_HostedModeWithOneAllowListAndOtherSentinel_DoesNotThrow()
    {
        var authOptions = new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            HostedGitHubAppClientId = "client-id",
            HostedGitHubAppClientSecret = "client-secret",
        };
        var admissionOptions = new HostedAdmissionControlOptions
        {
            Enabled = true,
            AllowedUserLogins = "markheydon",
            AllowedOrganisationLogins = AuthConfigurationPlaceholders.NotUsed,
        };

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        act();
    }

    [Fact]
    public void Validate_HostedModeWithBothAllowListsSentinel_ThrowsInvalidOperationException()
    {
        var authOptions = new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            HostedGitHubAppClientId = "client-id",
            HostedGitHubAppClientSecret = "client-secret",
        };
        var admissionOptions = new HostedAdmissionControlOptions
        {
            Enabled = true,
            AllowedUserLogins = AuthConfigurationPlaceholders.NotUsed,
            AllowedOrganisationLogins = AuthConfigurationPlaceholders.NotUsed,
        };

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Validate_HostedModeWithDisabledPlaceholderValues_ThrowsInvalidOperationException()
    {
        var authOptions = new GitHubAuthOptions
        {
            HostedSignInEnabled = true,
            HostedGitHubAppClientId = AuthConfigurationPlaceholders.Disabled,
            HostedGitHubAppClientSecret = AuthConfigurationPlaceholders.Disabled,
        };
        var admissionOptions = new HostedAdmissionControlOptions
        {
            Enabled = true,
            AllowedUserLogins = AuthConfigurationPlaceholders.Disabled,
        };

        var act = () => GitHubAuthConfigurationValidation.Validate(authOptions, admissionOptions);

        Assert.Throws<InvalidOperationException>(act);
    }
}
