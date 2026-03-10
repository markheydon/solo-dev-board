using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Pages;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Repositories"/> page.</summary>
public sealed class RepositoriesTests
{
    private readonly Mock<IRepositoryService> _repositoryServiceMock = new();

    [Fact]
    public async Task Repositories_WhileServiceIsLoading_ShowsLoadingIndicator()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        Assert.Contains("Loading repositories", cut.Markup);
        Assert.DoesNotContain("Unable to load repositories", cut.Markup);
        Assert.DoesNotContain("No repositories found", cut.Markup);
    }

    [Fact]
    public async Task Repositories_ServiceThrowsHttpRequestException_ShowsErrorMessage()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unable to load repositories", cut.Markup);
            Assert.Contains("Connection refused", cut.Markup);
            Assert.Contains("Try again", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_ServiceThrowsUnexpectedException_ShowsGenericErrorMessage()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Internal failure"));

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
            Assert.Contains("An unexpected error occurred while loading repositories", cut.Markup));
    }

    [Fact]
    public async Task Repositories_ServiceReturnsEmptyList_ShowsEmptyState()
    {
        // Arrange
        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RepositoryDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories found", cut.Markup);
            Assert.DoesNotContain("Loading repositories", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_ServiceReturnsRepositories_ShowsRepositoryNamesInGrid()
    {
        // Arrange
        var repositories = new List<RepositoryDto>
        {
            new(1, "my-first-repo", "owner/my-first-repo", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero)),
            new(2, "my-private-repo", "owner/my-private-repo", string.Empty, string.Empty, true, false, DateTimeOffset.UnixEpoch, new DateTimeOffset(2026, 2, 20, 12, 0, 0, TimeSpan.Zero)),
        };

        _repositoryServiceMock
            .Setup(service => service.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("my-first-repo", cut.Markup);
            Assert.Contains("my-private-repo", cut.Markup);
            Assert.DoesNotContain("Loading repositories", cut.Markup);
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => _repositoryServiceMock.Object);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }
}
