using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.GitHub;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class SingleUserCurrentUserContextTests
{
    [Fact]
    public void OwnerLogin_OptionsContainValidOwnerLogin_ReturnsOwnerLogin()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            OwnerLogin = "owner",
        });
        var sut = new SingleUserCurrentUserContext(options, new ResolvedPatOwnerLogin());

        // Act
        var result = sut.OwnerLogin;

        // Assert
        Assert.Equal("owner", result);
    }

    [Fact]
    public void OwnerLogin_ConfiguredOwnerLoginTakesPrecedenceOverResolvedLogin()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            OwnerLogin = "configured-owner",
        });
        var resolved = new ResolvedPatOwnerLogin { Value = "resolved-owner" };
        var sut = new SingleUserCurrentUserContext(options, resolved);

        // Act
        var result = sut.OwnerLogin;

        // Assert
        Assert.Equal("configured-owner", result);
    }

    [Fact]
    public void OwnerLogin_OwnerLoginNotConfigured_UsesResolvedLogin()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions());
        var resolved = new ResolvedPatOwnerLogin { Value = "resolved-owner" };
        var sut = new SingleUserCurrentUserContext(options, resolved);

        // Act
        var result = sut.OwnerLogin;

        // Assert
        Assert.Equal("resolved-owner", result);
    }

    [Fact]
    public void OwnerLogin_OwnerLoginAndResolvedLoginMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            OwnerLogin = AuthConfigurationPlaceholders.Disabled,
        });
        var sut = new SingleUserCurrentUserContext(options, new ResolvedPatOwnerLogin());

        // Act
        var act = () => sut.OwnerLogin;

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void GetAccessToken_OptionsContainValidToken_ReturnsToken()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            PersonalAccessToken = "test-token",
        });
        var sut = new SingleUserCurrentUserContext(options, new ResolvedPatOwnerLogin());

        // Act
        var result = sut.GetAccessToken();

        // Assert
        Assert.Equal("test-token", result);
    }

    [Fact]
    public void GetAccessToken_OptionsContainDisabledPlaceholder_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            PersonalAccessToken = AuthConfigurationPlaceholders.Disabled,
        });
        var sut = new SingleUserCurrentUserContext(options, new ResolvedPatOwnerLogin());

        // Act
        var act = () => sut.GetAccessToken();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }
}
