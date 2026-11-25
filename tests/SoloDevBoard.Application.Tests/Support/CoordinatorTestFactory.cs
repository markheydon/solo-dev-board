using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Application.Tests.Support;

internal static class CoordinatorTestFactory
{
    public static OperatorSnapshotCollectionCoordinator CreateWithGithub(IGitHubService github)
    {
        var scopeFactory = Substitute.For<ISnapshotCollectionCredentialScopeFactory>();
        scopeFactory.CreateScope(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => new GitHubServiceScope(github));

        return new OperatorSnapshotCollectionCoordinator(
            scopeFactory,
            TimeProvider.System,
            new OperatorSnapshotApplicationStopping(CancellationToken.None),
            NullLogger<OperatorSnapshotCollectionCoordinator>.Instance);
    }

    public static IGitHubService Gated(TaskCompletionSource gate, Exception? failure = null)
    {
        var github = Substitute.For<IGitHubService>();
        github.GetRepositoriesAsync(
                Arg.Any<CancellationToken>(),
                Arg.Any<bool>(),
                Arg.Any<IProgress<int>?>())
            .Returns(async call =>
            {
                await gate.Task.WaitAsync(call.Arg<CancellationToken>());
                if (failure is not null)
                {
                    throw failure;
                }

                return (IReadOnlyList<Domain.Entities.Repositories.Repository>)[];
            });
        return github;
    }

    private sealed class GitHubServiceScope(IGitHubService github) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new GitHubServiceProvider(github);

        public void Dispose()
        {
        }
    }

    private sealed class GitHubServiceProvider(IGitHubService github) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(IGitHubService) ? github : null;
    }
}
