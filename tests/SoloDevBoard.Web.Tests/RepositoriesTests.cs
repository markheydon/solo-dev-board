using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Web.Components.Features.Repositories.Pages;

namespace SoloDevBoard.Web.Tests;

/// <summary>Component tests for the <see cref="Repositories"/> page.</summary>
[Collection(nameof(RepositoriesTestsCollection))]
public sealed class RepositoriesTests
{
    private readonly IRepositoryService _repositoryService = Substitute.For<IRepositoryService>();
    private readonly IOperatorSnapshotCollectionService _snapshotCollectionService = Substitute.For<IOperatorSnapshotCollectionService>();

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
        Assert.Contains("Reload from GitHub", cut.Markup);
        Assert.Contains("Collect snapshot", cut.Markup);
        Assert.Contains("repositories-page-feedback", cut.Markup);
        Assert.True(
            cut.Markup.Contains("Bulk actions", StringComparison.Ordinal) ||
            cut.Markup.Contains("Actions", StringComparison.Ordinal));
        Assert.Contains("Search repositories", cut.Markup);
        Assert.Contains("Catalogue filter", cut.Markup);
        Assert.Contains("Open source", cut.Markup);
        Assert.Contains("Not open source", cut.Markup);
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
        Assert.NotEmpty(cut.FindAll("[data-testid='repositories-loading-state'] .mud-skeleton"));
        Assert.DoesNotContain("Unable to load repositories", cut.Markup);
        Assert.DoesNotContain("No repositories found", cut.Markup);
    }

    [Fact]
    public async Task Repositories_InitialRender_ShowsPageHeaderAndToolbar()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<RepositoryDto>>();
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);

        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<Repositories>();

        // Assert
        Assert.Contains("mud-typography-h5", cut.Markup);
        Assert.Contains("Repository command strip", cut.Markup);
        Assert.Contains("Reload from GitHub", cut.Markup);
        Assert.Contains("Collect snapshot", cut.Markup);
        Assert.Contains("repositories-page-feedback", cut.Markup);
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
            new(1, "my-first-repo", "owner/my-first-repo", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero), ["open-source"], true),
            new(2, "my-private-repo", "owner/my-private-repo", string.Empty, string.Empty, true, false, DateTimeOffset.UnixEpoch, new DateTimeOffset(2026, 2, 20, 12, 0, 0, TimeSpan.Zero), [], false),
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
                new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
                [],
                false),
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

    [Fact]
    public async Task Repositories_OpenSourceFilter_ShowsOnlyOpenSourceRepositories()
    {
        var repositories = new List<RepositoryDto>
        {
            new(1, "oss-repo", "owner/oss-repo", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ["open-source"], true),
            new(2, "customer-repo", "owner/customer-repo", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [], false),
        };

        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositories);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("oss-repo", cut.Markup));

        cut.Find("[data-testid='repositories-catalogue-filter-open-source']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("oss-repo", cut.Markup);
            Assert.DoesNotContain("customer-repo", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_NotOpenSourceFilter_ShowsComplementRepositories()
    {
        var repositories = new List<RepositoryDto>
        {
            new(1, "oss-repo", "owner/oss-repo", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ["open-source"], true),
            new(2, "customer-repo", "owner/customer-repo", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [], false),
        };

        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositories);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("customer-repo", cut.Markup));

        cut.Find("[data-testid='repositories-catalogue-filter-not-open-source']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("customer-repo", cut.Markup);
            Assert.DoesNotContain("oss-repo", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_OpenSourceFilterWithSearch_AppliesBothConstraints()
    {
        var repositories = new List<RepositoryDto>
        {
            new(1, "oss-alpha", "owner/oss-alpha", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ["open-source"], true),
            new(2, "oss-beta", "owner/oss-beta", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ["open-source"], true),
        };

        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositories);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("oss-alpha", cut.Markup));

        cut.Find("[data-testid='repositories-catalogue-filter-open-source']").Click();
        cut.Find("input").Input("alpha");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("oss-alpha", cut.Markup);
            Assert.DoesNotContain("oss-beta", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_OpenSourceFilterEmpty_ShowsFilterEmptyMessage()
    {
        var repositories = new List<RepositoryDto>
        {
            new(1, "customer-repo", "owner/customer-repo", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [], false),
        };

        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositories);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("customer-repo", cut.Markup));

        cut.Find("[data-testid='repositories-catalogue-filter-open-source']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories match this filter", cut.Markup);
            Assert.Contains("No catalogue repositories currently have the open-source topic.", cut.Markup);
            Assert.DoesNotContain("customer-repo", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_OpenSourceFilterWithSearchEmpty_ShowsCombinedEmptyMessage()
    {
        var repositories = new List<RepositoryDto>
        {
            new(1, "oss-alpha", "owner/oss-alpha", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ["open-source"], true),
            new(2, "oss-beta", "owner/oss-beta", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ["open-source"], true),
        };

        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositories);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("oss-alpha", cut.Markup));

        cut.Find("[data-testid='repositories-catalogue-filter-open-source']").Click();
        cut.Find("input").Input("missing-name");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No repositories match this filter", cut.Markup);
            Assert.Contains("No repositories match your search and filter.", cut.Markup);
            Assert.DoesNotContain("oss-alpha", cut.Markup);
            Assert.DoesNotContain("oss-beta", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_NotOpenSourceFilterEmpty_ShowsComplementEmptyMessage()
    {
        var repositories = new List<RepositoryDto>
        {
            new(1, "oss-repo", "owner/oss-repo", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ["open-source"], true),
        };

        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(repositories);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("oss-repo", cut.Markup));

        cut.Find("[data-testid='repositories-catalogue-filter-not-open-source']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Every catalogue repository currently has the open-source topic.", cut.Markup);
            Assert.DoesNotContain("oss-repo", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_AfterInitialLoad_ShowsReloadFromGitHubButton()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='repositories-reload-from-github-button']"));
            Assert.Contains("Reload from GitHub", cut.Markup);
            Assert.Contains("Collect snapshot", cut.Markup);
            Assert.Contains("repositories-page-feedback", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_ReloadFromGitHub_ForceReloadsCatalogueAndKeepsSearchFilter()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a"), CreateRepository("owner", "repo-b")]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("repo-a", cut.Markup));

        cut.Find("input").Input("repo-b");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("repo-b", cut.Markup);
            Assert.DoesNotContain("repo-a", cut.Markup);
        });

        await cut.Find("[data-testid='repositories-reload-from-github-button']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("repo-b", cut.Markup);
            Assert.DoesNotContain("repo-a", cut.Markup);
        });

        await _repositoryService.Received(1).GetRepositoriesAsync(Arg.Any<CancellationToken>(), true);
    }

    [Fact]
    public async Task Repositories_CollectSnapshot_UpdatesCombinedFeedbackWithoutReload()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();

        var statusReads = 0;
        var collectionStarted = 0;
        _snapshotCollectionService.StartAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Exchange(ref collectionStarted, 1);
                return OperatorSnapshotStartResult.Started;
            });

        _snapshotCollectionService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Volatile.Read(ref collectionStarted) == 0)
                {
                    return new OperatorSnapshotStatusDto { Kind = OperatorSnapshotStatusKind.NeverRun };
                }

                var read = Interlocked.Increment(ref statusReads);
                return read < 3
                    ? new OperatorSnapshotStatusDto
                    {
                        Kind = OperatorSnapshotStatusKind.InProgress,
                        RunStartedAt = DateTimeOffset.Parse("2025-06-01T12:00:00Z"),
                        RepositoriesRecordedThisRun = 1,
                    }
                    : new OperatorSnapshotStatusDto
                    {
                        Kind = OperatorSnapshotStatusKind.Succeeded,
                        LastSuccessfulCollectionAt = DateTimeOffset.Parse("2025-06-01T12:00:01Z"),
                        SnapshotRepositoryCount = 1,
                    };
            });

        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("No snapshot exists.", cut.Markup));

        await cut.Find("[data-testid='repositories-collect-snapshot-button']").ClickAsync(
            new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(
            () => Assert.Contains("Snapshot saved at", cut.Markup),
            timeout: TimeSpan.FromSeconds(15));

        Assert.True(statusReads >= 3, $"Expected repeated status reads after collect, got {statusReads}.");
    }

    [Fact]
    public async Task Repositories_InitialRender_DoesNotStartCollection()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();
        ctx.Render<Repositories>();

        await _snapshotCollectionService.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await _snapshotCollectionService.Received().GetStatusAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Repositories_ReloadFromGitHub_DoesNotStartCollection()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("repo-a", cut.Markup));

        await cut.Find("[data-testid='repositories-reload-from-github-button']").ClickAsync(
            new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() => Assert.Contains("Loaded 1 repositories.", cut.Markup));
        await _snapshotCollectionService.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Repositories_InitialRender_DoesNotExposeRunHistory()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("Loaded 1 repositories.", cut.Markup));
        Assert.DoesNotContain("run history", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("previous runs", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Repositories_SucceededSnapshot_ShowsFinishTimeInFeedback()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();
        _snapshotCollectionService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new OperatorSnapshotStatusDto
            {
                Kind = OperatorSnapshotStatusKind.Succeeded,
                LastSuccessfulCollectionAt = DateTimeOffset.Parse("2026-11-23T16:14:55Z"),
                SnapshotRepositoryCount = 3,
                DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine,
            });
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Snapshot saved at", cut.Markup);
            Assert.Contains("3 repositories included", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_SucceededWithZeroRepositories_ShowsZeroIncludedCopy()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([]);

        await using var ctx = CreateContext();
        _snapshotCollectionService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new OperatorSnapshotStatusDto
            {
                Kind = OperatorSnapshotStatusKind.Succeeded,
                LastSuccessfulCollectionAt = DateTimeOffset.Parse("2026-11-23T16:14:55Z"),
                SnapshotRepositoryCount = 0,
                DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine,
            });
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Snapshot saved at", cut.Markup);
            Assert.Contains("0 repositories included", cut.Markup);
            Assert.DoesNotContain("No snapshot exists.", cut.Markup);
        });
    }

    [Fact]
    public async Task Repositories_NeverRunVsZeroSuccess_ShowDistinctCopy()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([]);

        _snapshotCollectionService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new OperatorSnapshotStatusDto
            {
                Kind = OperatorSnapshotStatusKind.NeverRun,
                DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine,
            });

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("No snapshot exists.", cut.Markup));
    }

    [Fact]
    public async Task Repositories_InProgressPolling_RefreshesStatusWithoutCatalogueReload()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();

        var statusReads = 0;
        _snapshotCollectionService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                statusReads++;
                return new OperatorSnapshotStatusDto
                {
                    Kind = OperatorSnapshotStatusKind.InProgress,
                    RunStartedAt = DateTimeOffset.Parse("2025-06-01T12:00:00Z"),
                    RepositoriesRecordedThisRun = statusReads,
                };
            });

        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(
            () => Assert.Contains("recorded so far", cut.Markup),
            timeout: TimeSpan.FromSeconds(5));

        await _repositoryService.Received(1).GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>());
        Assert.True(statusReads >= 2);
    }

    [Fact]
    public async Task Repositories_HostedBlobDurability_UsesStandardNeverRunCopy()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        await using var ctx = CreateContext();
        _snapshotCollectionService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new OperatorSnapshotStatusDto
            {
                Kind = OperatorSnapshotStatusKind.NeverRun,
                DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob,
            });
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No snapshot exists.", cut.Markup);
            Assert.DoesNotContain("PAT-only", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Repositories_CollectSnapshot_UnsafeOperatorLogin_ShowsStorageSafetyFeedback()
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);

        _snapshotCollectionService.StartAsync(Arg.Any<CancellationToken>())
            .Returns(OperatorSnapshotStartResult.UnsafeOperatorLogin);

        await using var ctx = CreateContext();
        var cut = ctx.Render<Repositories>();

        cut.WaitForAssertion(() => Assert.Contains("Loaded 1 repositories.", cut.Markup));

        await cut.Find("[data-testid='repositories-collect-snapshot-button']").ClickAsync(
            new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(
            () => Assert.Contains("could not be stored safely", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Repositories_HostedNeverRun_ShowsNeverRunWithoutLastRunFacts()
    {
        await using var ctx = CreateContext();
        var cut = RenderHosted(ctx, HostedStatus(ReportedAccountScanStatus.NeverRun));

        cut.WaitForAssertion(() => Assert.Contains("Never run.", cut.Markup));
        Assert.DoesNotContain("Snapshot saved", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("could not be saved", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Last successful", cut.Markup, StringComparison.Ordinal);
        Assert.False(cut.Find("[data-testid='repositories-collect-snapshot-button']").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Repositories_HostedScheduled_ShowsScheduledNotRunning()
    {
        await using var ctx = CreateContext();
        var cut = RenderHosted(ctx, HostedStatus(ReportedAccountScanStatus.Scheduled));

        cut.WaitForAssertion(() => Assert.Contains("Scheduled.", cut.Markup));
        Assert.Contains("Next scheduled collection: 05:00 UTC.", cut.Markup);
        Assert.DoesNotContain("Running.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Saving snapshot", cut.Markup, StringComparison.Ordinal);
        Assert.True(cut.Find("[data-testid='repositories-collect-snapshot-button']").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Repositories_HostedScheduledAsRetryWait_ShowsNoTryListOrWaits()
    {
        var status = HostedStatus(
            ReportedAccountScanStatus.Scheduled,
            lastSuccess: DateTimeOffset.Parse("2025-06-01T12:00:00Z"));
        await using var ctx = CreateContext();
        var cut = RenderHosted(ctx, status);

        cut.WaitForAssertion(() => Assert.Contains("Scheduled.", cut.Markup));
        Assert.Contains("Last successful snapshot:", cut.Markup);
        Assert.DoesNotContain("could not be saved", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("attempt", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("retry", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("minutes", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OperatorSnapshotCollectionTrigger.Daily, "The daily occasion started it.")]
    [InlineData(OperatorSnapshotCollectionTrigger.Operator, "You started it.")]
    public async Task Repositories_HostedRunning_ShowsWhoStartedTheRun(
        OperatorSnapshotCollectionTrigger trigger,
        string expected)
    {
        await using var ctx = CreateContext();
        var cut = RenderHosted(ctx, HostedStatus(ReportedAccountScanStatus.Running, trigger: trigger));

        cut.WaitForAssertion(() => Assert.Contains("Running.", cut.Markup));
        Assert.Contains(expected, cut.Markup);
        Assert.Contains("recorded so far", cut.Markup);
        Assert.True(cut.Find("[data-testid='repositories-collect-snapshot-button']").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Repositories_HostedStalled_ShowsStalledAndCollectSnapshot()
    {
        var status = HostedStatus(
            ReportedAccountScanStatus.Stalled,
            lastSuccess: DateTimeOffset.Parse("2025-06-01T12:00:00Z"));
        await using var ctx = CreateContext();
        var cut = RenderHosted(ctx, status);

        cut.WaitForAssertion(() => Assert.Contains("Stalled.", cut.Markup));
        Assert.Contains("Last successful snapshot:", cut.Markup);
        Assert.DoesNotContain("Running.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("site stopped", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Something went wrong", cut.Markup, StringComparison.Ordinal);
        Assert.False(cut.Find("[data-testid='repositories-collect-snapshot-button']").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Repositories_HostedNotAllowed_UsesNotAllowedSentenceOnly()
    {
        var status = HostedStatus(
            ReportedAccountScanStatus.Suspended,
            suspension: AccountScanSuspensionReason.NotAllowed,
            noScheduleReason: OperatorSnapshotNoScheduleReason.NotAllowed,
            resultComment: "Should not appear");
        await using var ctx = CreateContext();
        var cut = RenderHosted(ctx, status);

        cut.WaitForAssertion(() => Assert.Contains("Suspended.", cut.Markup));
        Assert.Contains("You are not allowed to use the product.", cut.Markup);
        Assert.Contains("No scheduled collection will run because you are not allowed to use the product.", cut.Markup);
        Assert.DoesNotContain("Should not appear", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Running.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("sign in again", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Repositories_HostedCredentialUnusable_ShowsSuspendedFailureAndSignInAgain()
    {
        var status = HostedStatus(
            ReportedAccountScanStatus.Suspended,
            noScheduleReason: OperatorSnapshotNoScheduleReason.CredentialUnusable,
            lastResult: AccountScanStatusDocumentConstants.LastResultFailure,
            resultComment: "Could not reach GitHub with your current sign-in.",
            lastScanAt: DateTimeOffset.Parse("2025-06-02T12:00:00Z"));
        await using var ctx = CreateContext();
        var cut = RenderHosted(ctx, status);

        cut.WaitForAssertion(() => Assert.Contains("Suspended.", cut.Markup));
        Assert.Contains("No scheduled collection will run until you sign in again.", cut.Markup);
        Assert.Contains("Could not reach GitHub with your current sign-in.", cut.Markup);
        Assert.DoesNotContain("not allowed", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Repositories_LeanPosture_ShowsNoWorkerStatusWords()
    {
        var status = new OperatorSnapshotStatusDto
        {
            Kind = OperatorSnapshotStatusKind.NeverRun,
            DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine,
            SchedulePosture = OperatorSnapshotSchedulePosture.Lean,
            NoScheduleReason = OperatorSnapshotNoScheduleReason.Lean,
        };
        await using var ctx = CreateContext();
        var cut = RenderHosted(ctx, status);

        cut.WaitForAssertion(() => Assert.Contains("Nothing collects on a schedule.", cut.Markup));
        Assert.Contains("No snapshot exists.", cut.Markup);
        foreach (var word in new[] { "Never run.", "Scheduled.", "Running.", "Stalled.", "Suspended.", "05:00 UTC" })
        {
            Assert.DoesNotContain(word, cut.Markup, StringComparison.Ordinal);
        }
    }

    private IRenderedComponent<Repositories> RenderHosted(BunitContext ctx, OperatorSnapshotStatusDto status)
    {
        _repositoryService.GetRepositoriesAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns([CreateRepository("owner", "repo-a")]);
        _snapshotCollectionService.GetStatusAsync(Arg.Any<CancellationToken>()).Returns(status);

        var cut = ctx.Render<Repositories>();
        cut.WaitForAssertion(() => Assert.Contains("Loaded 1 repositories.", cut.Markup));
        return cut;
    }

    private static OperatorSnapshotStatusDto HostedStatus(
        ReportedAccountScanStatus reported,
        OperatorSnapshotCollectionTrigger? trigger = null,
        DateTimeOffset? lastSuccess = null,
        AccountScanSuspensionReason suspension = AccountScanSuspensionReason.None,
        OperatorSnapshotNoScheduleReason noScheduleReason = OperatorSnapshotNoScheduleReason.None,
        string? lastResult = null,
        string? resultComment = null,
        DateTimeOffset? lastScanAt = null)
    {
        var running = reported == ReportedAccountScanStatus.Running;
        return new OperatorSnapshotStatusDto
        {
            Kind = running ? OperatorSnapshotStatusKind.InProgress : OperatorSnapshotStatusKind.NeverRun,
            DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob,
            SchedulePosture = OperatorSnapshotSchedulePosture.Hosted,
            ReportedScanStatus = reported,
            Trigger = trigger,
            RunStartedAt = running ? DateTimeOffset.Parse("2025-06-01T12:00:00Z") : null,
            RepositoriesRecordedThisRun = running ? 3 : 0,
            LastSuccessfulCollectionAt = lastSuccess,
            SnapshotRepositoryCount = lastSuccess is null ? 0 : 1,
            ScanSuspensionReason = suspension,
            NoScheduleReason = noScheduleReason,
            LastScanResult = lastResult,
            ResultComment = resultComment,
            LastScanAt = lastScanAt,
            NextOccasionAtUtc = noScheduleReason == OperatorSnapshotNoScheduleReason.None
                ? new DateTimeOffset(2026, 11, 26, 5, 0, 0, TimeSpan.Zero)
                : null,
        };
    }

    private static RepositoryDto CreateRepository(string owner, string name)
        => new(1, name, $"{owner}/{name}", string.Empty, string.Empty, false, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [], false);

    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddTestGitHubAuthenticationRecovery();
        ctx.Services.AddScoped(_ => _repositoryService);
        _snapshotCollectionService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new OperatorSnapshotStatusDto { Kind = OperatorSnapshotStatusKind.NeverRun });
        ctx.Services.AddScoped(_ => _snapshotCollectionService);

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }
}
