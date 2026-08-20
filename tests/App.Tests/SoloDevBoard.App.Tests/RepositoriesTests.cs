using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.App.Components.Features.Repositories.Pages;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for the <see cref="Repositories"/> page.</summary>
public sealed class RepositoriesTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();

    [Fact]
    public async Task Repositories_InitialRender_ShowsPrimaryCommandSurface()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        Assert.Contains("Repository command strip", cut.Markup);
        Assert.Contains("Refresh", cut.Markup);
        Assert.True(
            cut.Markup.Contains("Bulk actions", StringComparison.Ordinal) ||
            cut.Markup.Contains("Actions", StringComparison.Ordinal));
        Assert.Contains("Search repositories", cut.Markup);
    }

    [Fact]
    public async Task Repositories_WhileServiceIsLoading_ShowsLoadingIndicator()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);

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
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<RepositoryDto>>(new HttpRequestException("Connection refused")));

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
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<RepositoryDto>>(new InvalidOperationException("Internal failure")));

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
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<RepositoryDto>());

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories found", cut.Markup);
            Assert.Contains("No repositories are connected yet", cut.Markup);
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

        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositories);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("my-first-repo", cut.Markup);
            Assert.Contains("my-private-repo", cut.Markup);
            Assert.Contains("Repository name", cut.Markup);
            Assert.Contains("Status", cut.Markup);
            Assert.Contains("Actions", cut.Markup);
            Assert.Contains("Connected", cut.Markup);
            Assert.Contains("Private", cut.Markup);
            Assert.Contains("Loaded 2 repositories", cut.Markup);
            Assert.DoesNotContain("Loading repositories", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_ServiceReturnsLongRepositoryName_KeepsFullNameAccessibleInStackedGrid()
    {
        var repositories = new List<RepositoryDto>
        {
            new(
                1,
                "mhcg-cs-mhcgintegration-platform-service",
                "owner/mhcg-cs-mhcgintegration-platform-service",
                string.Empty,
                string.Empty,
                false,
                false,
                DateTimeOffset.UnixEpoch,
                new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero)),
        };

        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositories);

        await using var ctx = CreateContext();

        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("sdb-responsive-grid", cut.Markup);
            Assert.Contains("mhcg-cs-mhcgintegration-platform-service", cut.Markup);
            Assert.Contains("Connected", cut.Markup);
            Assert.Contains("Public", cut.Markup);
            Assert.Contains("Edit repository", cut.Markup);
        });
    }

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _repositoryService);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }
}
