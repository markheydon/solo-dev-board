using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Pages;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Repositories"/> page.</summary>
public sealed class RepositoriesTests : BunitContext
{
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();

    /// <summary>
    /// Initialises the bUnit test context with MudBlazor services and a loose JS interop mode
    /// so that MudBlazor components render without requiring a real browser JavaScript runtime.
    /// </summary>
    public RepositoriesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddScoped(_ => _repositoryServiceMock.Object);
    }

    [Fact]
    public void Repositories_WhileServiceIsLoading_ShowsLoadingIndicator()
    {
        // Arrange — keep the service call permanently pending so the component stays in loading state
        var tcs = new TaskCompletionSource<IReadOnlyList<Repository>>();
        _repositoryServiceMock
            .Setup(s => s.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act — render before the async initialisation completes
        var cut = Render<Repositories>();

        // Assert — initial render must show loading indicator, not data or error states
        Assert.Contains("Loading repositories", cut.Markup);
        Assert.DoesNotContain("Unable to load repositories", cut.Markup);
        Assert.DoesNotContain("No repositories found", cut.Markup);
    }

    [Fact]
    public void Repositories_ServiceThrowsHttpRequestException_ShowsErrorMessage()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(s => s.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var cut = Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load repositories", cut.Markup);
            Assert.Contains("Connection refused", cut.Markup);
            Assert.Contains("Try again", cut.Markup);
        });
    }

    [Fact]
    public void Repositories_ServiceThrowsUnexpectedException_ShowsGenericErrorMessage()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(s => s.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Internal failure"));

        // Act
        var cut = Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
            Assert.Contains("An unexpected error occurred while loading repositories", cut.Markup));
    }

    [Fact]
    public void Repositories_ServiceReturnsEmptyList_ShowsEmptyState()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(s => s.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Repository>());

        // Act
        var cut = Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories found", cut.Markup);
            Assert.DoesNotContain("Loading repositories", cut.Markup);
        });
    }

    [Fact]
    public void Repositories_ServiceReturnsRepositories_ShowsRepositoryNamesInGrid()
    {
        // Arrange
        var repositories = new List<Repository>
        {
            new() { Id = 1, Name = "my-first-repo", FullName = "owner/my-first-repo", IsPrivate = false, UpdatedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero) },
            new() { Id = 2, Name = "my-private-repo", FullName = "owner/my-private-repo", IsPrivate = true, UpdatedAt = new DateTimeOffset(2026, 2, 20, 12, 0, 0, TimeSpan.Zero) },
        };

        _repositoryServiceMock
            .Setup(s => s.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var cut = Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("my-first-repo", cut.Markup);
            Assert.Contains("my-private-repo", cut.Markup);
            Assert.DoesNotContain("Loading repositories", cut.Markup);
        });
    }
}
