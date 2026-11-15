using SoloDevBoard.Application.Identity;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for Application identity exceptions.</summary>
public sealed class ApplicationIdentityExceptionTests
{
    [Fact]
    public void HostedAuthenticationRequiredException_DefaultConstructor_UsesExpectedMessage()
    {
        // Act
        var exception = new HostedAuthenticationRequiredException();

        // Assert
        Assert.Equal("Hosted GitHub authentication is required. Sign in again to continue.", exception.Message);
    }

    [Fact]
    public void HostedAuthenticationRequiredException_MessageConstructor_PreservesMessage()
    {
        // Act
        var exception = new HostedAuthenticationRequiredException("Session expired.");

        // Assert
        Assert.Equal("Session expired.", exception.Message);
    }

    [Fact]
    public void HostedAuthenticationRequiredException_InnerExceptionConstructor_PreservesInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Token rejected.");

        // Act
        var exception = new HostedAuthenticationRequiredException("Session expired.", inner);

        // Assert
        Assert.Equal("Session expired.", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void GitHubPatConnectivityRequiredException_DefaultConstructor_CreatesExceptionWithoutInnerException()
    {
        // Act
        var exception = new GitHubPatConnectivityRequiredException();

        // Assert
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void GitHubPatConnectivityRequiredException_MessageConstructor_PreservesMessage()
    {
        // Act
        var exception = new GitHubPatConnectivityRequiredException("PAT rejected.");

        // Assert
        Assert.Equal("PAT rejected.", exception.Message);
    }

    [Fact]
    public void GitHubPatConnectivityRequiredException_InnerExceptionConstructor_PreservesInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Unauthorized.");

        // Act
        var exception = new GitHubPatConnectivityRequiredException("PAT rejected.", inner);

        // Assert
        Assert.Equal("PAT rejected.", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }
}
