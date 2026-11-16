using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Loads funding hygiene from stored inventory snapshots.</summary>
public sealed class OpenSourceFundingService(
    IOperatorKeyResolver operatorKeyResolver,
    IInventoryRunRepository runRepository,
    ICanonicalFundingStandardRepository standardRepository,
    IInventoryCatalogueClient catalogueClient,
    IOptions<GitHubAuthOptions> authOptions,
    IOptions<InventoryOptions> inventoryOptions) : IOpenSourceFundingService
{
    private readonly IOperatorKeyResolver _operatorKeyResolver = operatorKeyResolver ?? throw new ArgumentNullException(nameof(operatorKeyResolver));
    private readonly IInventoryRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
    private readonly ICanonicalFundingStandardRepository _standardRepository = standardRepository ?? throw new ArgumentNullException(nameof(standardRepository));
    private readonly IInventoryCatalogueClient _catalogueClient = catalogueClient ?? throw new ArgumentNullException(nameof(catalogueClient));
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly InventoryOptions _inventoryOptions = inventoryOptions?.Value ?? throw new ArgumentNullException(nameof(inventoryOptions));

    public async Task<OpenSourceFundingPageDto> GetFundingHygienePageAsync(CancellationToken cancellationToken)
    {
        var operatorKey = await _operatorKeyResolver.ResolveCurrentOperatorKeyAsync(cancellationToken).ConfigureAwait(false);
        var standardEntity = await _standardRepository.GetByOperatorKeyAsync(operatorKey, cancellationToken).ConfigureAwait(false);
        var standardDto = standardEntity is null
            ? null
            : new CanonicalFundingStandardDto(standardEntity.Content, standardEntity.SavedAtUtc, standardEntity.ContentHash);

        var displayedRun = await _runRepository.GetLatestFinishedRunAsync(operatorKey, cancellationToken).ConfigureAwait(false);
        var latestAttempt = await _runRepository.GetLatestAttemptAsync(operatorKey, cancellationToken).ConfigureAwait(false);

        var hostedConfigured = ResolveHostedInventoryConfigured();
        var unavailableMessage = hostedConfigured ? null : "Hosted overnight inventory is unavailable until the GitHub App installation credentials are configured.";
        string? lastFailureReason = null;
        var latestAttemptFailed = false;
        if (latestAttempt is { Outcome: InventoryRunOutcome.Failed })
        {
            latestAttemptFailed = displayedRun is null || displayedRun.Id != latestAttempt.Id;
            lastFailureReason = latestAttempt.FailureReason;
        }

        var rows = new List<RepositoryFundingRowDto>();
        InventoryRunSummaryDto? displayedSummary = null;
        var isStale = false;
        var standardDrift = false;
        DateTimeOffset? comparedAt = null;

        if (displayedRun is not null)
        {
            displayedSummary = MapRun(displayedRun);
            var snapshots = await _runRepository.GetSnapshotsForRunAsync(displayedRun.Id, cancellationToken).ConfigureAwait(false);
            rows.AddRange(snapshots.Select(MapSnapshot));

            if (displayedRun.FinishedAtUtc is { } finishedAt)
            {
                isStale = DateTimeOffset.UtcNow - finishedAt > InventoryOptions.StaleAfter;
                comparedAt = finishedAt;
            }

            if (standardEntity is not null
                && !string.Equals(standardEntity.ContentHash, displayedRun.StandardContentHashAtStart, StringComparison.Ordinal))
            {
                standardDrift = true;
            }
        }

        var catalogueOpenSource = await LoadOpenSourceCatalogueAsync(cancellationToken).ConfigureAwait(false);
        var snapshotNames = rows.Select(row => row.RepositoryFullName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var repository in catalogueOpenSource)
        {
            if (!snapshotNames.Contains(repository.FullName))
            {
                rows.Add(new RepositoryFundingRowDto(
                    repository.FullName,
                    repository.HtmlUrl,
                    repository.IsArchived,
                    repository.IsFork,
                    repository.IsPrivate,
                    FundingStatus: null,
                    ComparedPath: null,
                    ExtraFundingPathNote: null,
                    FundingPlatforms: null,
                    RowScanState.NotYetScanned,
                    StatusLabel: "Not yet scanned",
                    FailureOrVisibilityReason: null));
            }
        }

        rows.Sort(static (left, right) => string.Compare(left.RepositoryFullName, right.RepositoryFullName, StringComparison.OrdinalIgnoreCase));

        return new OpenSourceFundingPageDto(
            displayedSummary,
            latestAttempt is null ? null : MapRun(latestAttempt),
            latestAttemptFailed,
            isStale,
            hostedConfigured,
            unavailableMessage,
            lastFailureReason,
            standardDto,
            standardDrift,
            comparedAt,
            rows);
    }

    public async Task<FundingSummaryCountsDto> GetFundingSummaryAsync(CancellationToken cancellationToken)
    {
        var page = await GetFundingHygienePageAsync(cancellationToken).ConfigureAwait(false);
        return BuildSummary(page.Rows);
    }

    private bool ResolveHostedInventoryConfigured()
    {
        if (!_authOptions.HostedSignInEnabled)
        {
            return true;
        }

        if (_inventoryOptions.HostedInventoryConfigured)
        {
            return true;
        }

        return AuthConfigurationPlaceholders.IsConfigured(_authOptions.GitHubAppId)
            && AuthConfigurationPlaceholders.IsConfigured(_authOptions.GitHubAppPrivateKey);
    }

    private Task<IReadOnlyList<InventoryCatalogueRepository>> LoadOpenSourceCatalogueAsync(CancellationToken cancellationToken)
    {
        return _catalogueClient.GetOpenSourceRepositoriesAsync(cancellationToken);
    }

    private static FundingSummaryCountsDto BuildSummary(IReadOnlyList<RepositoryFundingRowDto> rows)
    {
        var matches = 0;
        var differs = 0;
        var missing = 0;
        var notCompared = 0;
        var notScanned = 0;
        var notYet = 0;
        var notVisible = 0;
        var needsAttention = 0;

        foreach (var row in rows)
        {
            switch (row.RowScanState)
            {
                case RowScanState.NotYetScanned:
                    notYet++;
                    if (!row.IsArchived)
                    {
                        needsAttention++;
                    }

                    break;
                case RowScanState.NotScanned:
                    notScanned++;
                    if (!row.IsArchived)
                    {
                        needsAttention++;
                    }

                    break;
                case RowScanState.NotVisible:
                    notVisible++;
                    break;
                case RowScanState.Checked:
                    switch (row.FundingStatus)
                    {
                        case FundingRowStatus.Matches:
                            matches++;
                            break;
                        case FundingRowStatus.Differs:
                            differs++;
                            if (!row.IsArchived)
                            {
                                needsAttention++;
                            }

                            break;
                        case FundingRowStatus.Missing:
                            missing++;
                            if (!row.IsArchived)
                            {
                                needsAttention++;
                            }

                            break;
                        case FundingRowStatus.NotCompared:
                            notCompared++;
                            break;
                    }

                    break;
            }
        }

        return new FundingSummaryCountsDto(matches, differs, missing, notCompared, notScanned, notYet, notVisible, needsAttention);
    }

    private static RepositoryFundingRowDto MapSnapshot(RepositoryInventorySnapshot snapshot)
    {
        if (!snapshot.InstallationVisible || snapshot.ScanState == RepositoryScanState.NotVisible)
        {
            return new RepositoryFundingRowDto(
                snapshot.RepositoryFullName,
                FormattableString.Invariant($"https://github.com/{snapshot.RepositoryFullName}"),
                snapshot.IsArchived,
                snapshot.IsFork,
                snapshot.IsPrivate,
                null,
                null,
                null,
                null,
                RowScanState.NotVisible,
                "Not visible to installation",
                "This repository is outside the GitHub App installation.");
        }

        if (snapshot.ScanState == RepositoryScanState.NotScanned)
        {
            return new RepositoryFundingRowDto(
                snapshot.RepositoryFullName,
                FormattableString.Invariant($"https://github.com/{snapshot.RepositoryFullName}"),
                snapshot.IsArchived,
                snapshot.IsFork,
                snapshot.IsPrivate,
                null,
                null,
                null,
                null,
                RowScanState.NotScanned,
                "Not scanned",
                snapshot.FailureReason);
        }

        var fundingStatus = MapFundingStatus(snapshot.FundingStatus);
        var label = fundingStatus switch
        {
            FundingRowStatus.Matches => "Matches standard",
            FundingRowStatus.Differs => "Differs from standard",
            FundingRowStatus.Missing => "Missing from repository",
            FundingRowStatus.NotCompared => "Present (not compared)",
            _ => "Unknown",
        };

        return new RepositoryFundingRowDto(
            snapshot.RepositoryFullName,
            FormattableString.Invariant($"https://github.com/{snapshot.RepositoryFullName}"),
            snapshot.IsArchived,
            snapshot.IsFork,
            snapshot.IsPrivate,
            fundingStatus,
            snapshot.ComparedPath,
            snapshot.NotedExtraFundingPath,
            snapshot.FundingPlatforms,
            RowScanState.Checked,
            label,
            null);
    }

    private static FundingRowStatus? MapFundingStatus(Domain.Entities.Inventory.FundingStatus? status)
    {
        return status switch
        {
            Domain.Entities.Inventory.FundingStatus.Missing => FundingRowStatus.Missing,
            Domain.Entities.Inventory.FundingStatus.MatchesStandard => FundingRowStatus.Matches,
            Domain.Entities.Inventory.FundingStatus.Differs => FundingRowStatus.Differs,
            Domain.Entities.Inventory.FundingStatus.NotCompared => FundingRowStatus.NotCompared,
            _ => null,
        };
    }

    private static InventoryRunSummaryDto MapRun(InventoryRun run)
    {
        return new InventoryRunSummaryDto(
            run.Id,
            run.StartedAtUtc,
            run.FinishedAtUtc,
            (InventoryRunTriggerDto)run.Trigger,
            (InventoryAccessKindDto)run.AccessKind,
            (InventoryRunOutcomeDto)run.Outcome,
            run.WasRateLimited,
            run.CheckedRepositoryCount,
            run.IntendedRepositoryCount,
            run.StandardContentHashAtStart);
    }
}