# Application service contracts (Web boundary)

**Feature**: `specs/001-oss-funding-hygiene`

Public Application interfaces consumed by Blazor (`SoloDevBoard.Web`). All return **DTO records** only (constitution II). Implementations live in Application; persistence in Infrastructure.

---

## `IOpenSourceFundingService`

Read-only surface for the Open Source funding section.

```csharp
/// <summary>Loads stored funding hygiene for the current operator. No live GitHub calls.</summary>
Task<OpenSourceFundingPageDto> GetFundingHygienePageAsync(CancellationToken cancellationToken);

/// <summary>Summary counts for toolbar chips or KPI row.</summary>
Task<FundingSummaryCountsDto> GetFundingSummaryAsync(CancellationToken cancellationToken);
```

**`OpenSourceFundingPageDto`** (record):

- `DisplayedRun` : `InventoryRunSummaryDto?` (latest finished `Completed` or `Partial`; null when never run)
- `LatestAttempt` : `InventoryRunSummaryDto?` (newest run by start time, including `Failed`)
- `LatestAttemptFailed` : bool (banner when `LatestAttempt.Outcome = Failed` and differs from displayed run)
- `IsStale` : bool (true when displayed run finished older than 36 hours)
- `HostedInventoryConfigured` : bool (hosted: installation credential present; PAT mode always true)
- `HostedInventoryUnavailableMessage` : string? (when hosted and credential missing or last attempt failed for credential reasons; FR-015)
- `HostedInventoryLastFailureReason` : string? (from latest `Failed` attempt when no newer successful run)
- `Standard` : `CanonicalFundingStandardDto?`
- `StandardDriftWarning` : bool (saved standard hash differs from hash used for displayed run)
- `StandardComparedAtUtc` : DateTimeOffset? (when displayed run finished, for FR-019 labelling)
- `Rows` : `IReadOnlyList<RepositoryFundingRowDto>`

**`RepositoryFundingRowDto`** (record): repository display name, GitHub URL, traits (archived/fork/private), `FundingStatus` (Application UI enum: missing, matches, differs, or **notCompared** when file present but no standard at run start; FR-003), extra-file note, platforms, `RowScanState` (Application DTO enum: `Checked`, `NotScanned`, `NotYetScanned`, `NotVisible`; maps from Domain `RepositoryScanState` on snapshots; `NotYetScanned` only on synthetic catalogue rows, never persisted), UK English label text, failure or not-visible reason.

**`InventoryRunSummaryDto`** (record): `StartedAtUtc`, `FinishedAtUtc?`, `Trigger` (scheduled/manual), `AccessKind`, `Outcome`, `WasRateLimited`, `CheckedRepositoryCount`, `IntendedRepositoryCount`, `StandardContentHashAtStart`.

**`FundingSummaryCountsDto`** (record): `Matches`, `Differs`, `Missing`, `NotCompared`, `NotScanned`, `NotYetScanned`, `NotVisible`; needs-attention total excludes archived and counts only non-archived `Missing` and `Differs` (FR-012).

---

## `ICanonicalFundingStandardService`

```csharp
Task<CanonicalFundingStandardDto?> GetCurrentAsync(CancellationToken cancellationToken);
Task<CanonicalFundingStandardDto> SaveAsync(SaveCanonicalFundingStandardRequest request, CancellationToken cancellationToken);
```

**`SaveCanonicalFundingStandardRequest`**: `Content` string; validation rejects empty/whitespace.

---

## `IInventoryRunCommandService`

Manual scan orchestration from Web (acknowledge fast, work async).

```csharp
/// <summary>Queues a manual inventory if none active; otherwise returns already-underway result.</summary>
Task<RequestInventoryRunResultDto> RequestScanNowAsync(CancellationToken cancellationToken);
```

**`RequestInventoryRunResultDto`**: `Accepted` | `AlreadyUnderway` | `Unavailable` (hosted installation not configured), optional message for snackbar (UK English).

---

## Infrastructure contracts (not Web-facing)

| Interface | Responsibility |
|-----------|----------------|
| `IInventoryRunRepository` | CRUD runs and snapshots |
| `ICanonicalFundingStandardRepository` | Operator standard row |
| `IInventoryScanQueue` | Enqueue manual scan messages. Infrastructure implementation takes `QueueServiceClient` from DI (`AddAzureQueueServiceClient("inventory-queues")` on the Web host). It does not call `GetConnectionString` or `new QueueClient(connectionString, ...)`. Not injected into Blazor. |
| `IGitHubOpenSourceInventoryScanner` | Per-repo GitHub fetch. Funding facts plus Community Standards facts from [data-model.md](../data-model.md): `HasDescription`, `HasReadme`, and for the six health files an in-repository flag and an organisation-only flag, with one `LicenceName`. These fields are persisted on the snapshot and are not members of `RepositoryFundingRowDto`. |
| `IGitHubInstallationTokenProvider` | Installation JWT for Functions hosted path |

---

## Functions host (internal)

No HTTP API. Queue and timer handlers resolve the same Application/Infrastructure graph via `AddSoloDevBoardHost` (SQL client integration, then `AddSoloDevBoard`). Functions do not register the queue client. The queue trigger uses host storage from `WithHostStorage` and omits `Connection`. Scanner entry point: `IInventoryScanOrchestrator.RunAsync(operatorKey, trigger, cancellationToken)`.

**Unattended GitHub access**: Functions register a host-specific scoped factory: one DI scope per `OperatorKey` from the queue message or timer target. `IGitHubInstallationTokenProvider` mints and caches installation tokens asynchronously before each walk. PAT mode uses static PAT from configuration without blocking calls in `GetAccessToken()`.