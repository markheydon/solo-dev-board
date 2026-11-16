using SoloDevBoard.Application.Services.OpenSource;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Infrastructure.Inventory;

/// <summary>Fetches funding and Community Standards facts for one repository.</summary>
public sealed class GitHubOpenSourceInventoryScanner(InventoryGitHubClient gitHubClient) : IGitHubOpenSourceInventoryScanner
{
    private readonly InventoryGitHubClient _gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));

    public async Task<RepositoryInventorySnapshot> ScanRepositoryAsync(
        InventoryRun run,
        string repositoryFullName,
        bool isArchived,
        bool isFork,
        bool isPrivate,
        string repositoryDescription,
        string? standardContent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        var snapshot = new RepositoryInventorySnapshot
        {
            InventoryRunId = run.Id,
            OperatorKey = run.OperatorKey,
            RepositoryFullName = repositoryFullName,
            IsArchived = isArchived,
            IsFork = isFork,
            IsPrivate = isPrivate,
            InstallationVisible = true,
            ScanState = RepositoryScanState.Checked,
        };

        var dotGitHubPath = ".github/FUNDING.yml";
        var (dotFound, dotContent, dotDenied) = await _gitHubClient.TryGetFileContentAsync(repositoryFullName, dotGitHubPath, cancellationToken).ConfigureAwait(false);
        if (dotDenied)
        {
            return NotVisibleSnapshot(snapshot);
        }

        var (rootFound, rootContent, rootDenied) = await _gitHubClient.TryGetFileContentAsync(repositoryFullName, "FUNDING.yml", cancellationToken).ConfigureAwait(false);
        if (rootDenied)
        {
            return NotVisibleSnapshot(snapshot);
        }

        string? comparedContent = null;
        string? comparedPath = null;
        if (dotFound)
        {
            comparedContent = dotContent;
            comparedPath = dotGitHubPath;
            if (rootFound)
            {
                snapshot.NotedExtraFundingPath = "FUNDING.yml";
            }
        }
        else if (rootFound)
        {
            comparedContent = rootContent;
            comparedPath = "FUNDING.yml";
        }

        snapshot.ComparedPath = comparedPath;
        if (comparedContent is null)
        {
            snapshot.FundingStatus = FundingStatus.Missing;
        }
        else if (string.IsNullOrWhiteSpace(standardContent))
        {
            snapshot.FundingStatus = FundingStatus.NotCompared;
            snapshot.FundingPlatforms = ExtractPlatforms(comparedContent);
        }
        else
        {
            var compare = FundingFileComparer.Compare(comparedContent, standardContent);
            snapshot.FundingStatus = compare.Outcome switch
            {
                FundingCompareOutcome.Matches => FundingStatus.MatchesStandard,
                FundingCompareOutcome.Differs => FundingStatus.Differs,
                FundingCompareOutcome.TooLarge => null,
                _ => FundingStatus.Missing,
            };
            if (compare.Outcome == FundingCompareOutcome.TooLarge)
            {
                snapshot.ScanState = RepositoryScanState.NotScanned;
                snapshot.FailureReason = compare.Reason;
                snapshot.FundingStatus = null;
            }
            else
            {
                snapshot.FundingPlatforms = ExtractPlatforms(comparedContent);
            }
        }

        snapshot.HasDescription = !string.IsNullOrWhiteSpace(repositoryDescription);
        return snapshot;
    }

    private static RepositoryInventorySnapshot NotVisibleSnapshot(RepositoryInventorySnapshot snapshot)
    {
        snapshot.InstallationVisible = false;
        snapshot.ScanState = RepositoryScanState.NotVisible;
        snapshot.FundingStatus = null;
        return snapshot;
    }

    private static string? ExtractPlatforms(string content)
    {
        var platforms = new List<string>();
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) && trimmed.Contains(':'))
            {
                continue;
            }

            if (trimmed.EndsWith(':'))
            {
                var key = trimmed[..^1];
                if (!string.Equals(key, "platforms", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(key))
                {
                    platforms.Add(key);
                }
            }
        }

        return platforms.Count == 0 ? null : string.Join(',', platforms);
    }
}