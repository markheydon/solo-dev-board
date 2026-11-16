namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Page model for Open Source funding hygiene.</summary>
public sealed record OpenSourceFundingPageDto(
    InventoryRunSummaryDto? DisplayedRun,
    InventoryRunSummaryDto? LatestAttempt,
    bool LatestAttemptFailed,
    bool IsStale,
    bool HostedInventoryConfigured,
    string? HostedInventoryUnavailableMessage,
    string? HostedInventoryLastFailureReason,
    CanonicalFundingStandardDto? Standard,
    bool StandardDriftWarning,
    DateTimeOffset? StandardComparedAtUtc,
    IReadOnlyList<RepositoryFundingRowDto> Rows);

public sealed record InventoryRunSummaryDto(
    Guid Id,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    InventoryRunTriggerDto Trigger,
    InventoryAccessKindDto AccessKind,
    InventoryRunOutcomeDto Outcome,
    bool WasRateLimited,
    int CheckedRepositoryCount,
    int IntendedRepositoryCount,
    string StandardContentHashAtStart);

public sealed record RepositoryFundingRowDto(
    string RepositoryFullName,
    string GitHubUrl,
    bool IsArchived,
    bool IsFork,
    bool IsPrivate,
    FundingRowStatus? FundingStatus,
    string? ComparedPath,
    string? ExtraFundingPathNote,
    string? FundingPlatforms,
    RowScanState RowScanState,
    string StatusLabel,
    string? FailureOrVisibilityReason);

public sealed record FundingSummaryCountsDto(
    int Matches,
    int Differs,
    int Missing,
    int NotCompared,
    int NotScanned,
    int NotYetScanned,
    int NotVisible,
    int NeedsAttention);

public sealed record CanonicalFundingStandardDto(
    string Content,
    DateTimeOffset SavedAtUtc,
    string ContentHash);

public sealed record SaveCanonicalFundingStandardRequest(string Content);

public enum InventoryRunTriggerDto
{
    Scheduled,
    Manual,
}

public enum InventoryAccessKindDto
{
    PersonalAccessToken,
    Installation,
}

public enum InventoryRunOutcomeDto
{
    Queued,
    InProgress,
    Completed,
    Partial,
    Failed,
}

public enum RequestInventoryRunResultKind
{
    Accepted,
    AlreadyUnderway,
    Unavailable,
}

public sealed record RequestInventoryRunResultDto(
    RequestInventoryRunResultKind Kind,
    string Message);