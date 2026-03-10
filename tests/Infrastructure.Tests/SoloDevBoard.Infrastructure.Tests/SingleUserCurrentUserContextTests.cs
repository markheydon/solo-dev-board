using Microsoft.Extensions.Options;
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
        var sut = new SingleUserCurrentUserContext(options);

        // Act
        var result = sut.OwnerLogin;

        // Assert
        Assert.Equal("owner", result);
    }

    [Fact]
    public void OwnerLogin_OptionsContainEmptyOwnerLogin_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            OwnerLogin = string.Empty,
        });
        var sut = new SingleUserCurrentUserContext(options);

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
        var sut = new SingleUserCurrentUserContext(options);

        // Act
        var result = sut.GetAccessToken();

        // Assert
        Assert.Equal("test-token", result);
    }

    [Fact]
    public void GetAccessToken_OptionsContainEmptyToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new GitHubAuthOptions
        {
            PersonalAccessToken = string.Empty,
        });
        var sut = new SingleUserCurrentUserContext(options);

        // Act
        var act = () => sut.GetAccessToken();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }
}
