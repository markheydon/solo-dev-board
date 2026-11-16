# Data model: OSS funding file hygiene

**Feature**: `specs/001-oss-funding-hygiene`  
**Storage**: Azure SQL database `inventory` ([ADR-0046](../../adr/adr-0046-overnight-inventory-functions-and-sql.md))

Logical model for EF Core implementation. Physical table names may use a consistent prefix (for example `Inventory_`) in Infrastructure configuration.

---

## Operator partition

All entities below are scoped by **`OperatorKey`** (string, required, indexed):

| Mode | Format | Source |
|------|--------|--------|
| PAT | `user:{ownerLogin}` | `GitHubAuthOptions.OwnerLogin` / PAT resolution |
| Hosted (Web) | `installation:{installationId}` | Installation id from interactive sign-in claims or `GitHubAuth__InstallationId` on Web (same id Functions uses for that tenant) |
| Hosted (Functions) | `installation:{installationId}` | `gh-app-installation-id` when set, otherwise discovery at Functions startup for timer multi-target |

Unique constraints that involve "latest run" are enforced per `OperatorKey`.

---

## CanonicalFundingStandard

One row per operator (upsert on save).

| Field | Type | Rules |
|-------|------|--------|
| `OperatorKey` | string | PK |
| `Content` | string (text) | Non-empty; reject whitespace-only (FR-010) |
| `SavedAtUtc` | DateTimeOffset | Set on each save |
| `ContentHash` | string | SHA-256 of normalised content; stored on snapshots for stale-standard UX (FR-019) |

**Validation**: Application service rejects empty standard; Web shows validation via MudBlazor form.

---

## InventoryRun

One row per started pass (scheduled or manual).

| Field | Type | Rules |
|-------|------|--------|
| `Id` | Guid | PK |
| `OperatorKey` | string | FK partition |
| `Trigger` | enum | `Scheduled`, `Manual` |
| `AccessKind` | enum | `PersonalAccessToken`, `Installation` |
| `StartedAtUtc` | DateTimeOffset | Required |
| `FinishedAtUtc` | DateTimeOffset? | Null while in progress |
| `Outcome` | enum | `Queued`, `InProgress`, `Completed`, `Partial`, `Failed` |
| `WasRateLimited` | bool | FR-013 |
| `StandardContentHashAtStart` | string | Hash compared for this run |
| `IntendedRepositoryCount` | int | Open-source scope count at start |
| `CheckedRepositoryCount` | int | Repositories with a definitive funding or failure row |
| `IsSuperseded` | bool | True when a newer finished run exists for operator |

**State transitions**:

```text
Queued → InProgress → Completed | Partial | Failed
```

**Single active run**: At most one row per `OperatorKey` with `Outcome` in (`Queued`, `InProgress`). Enforce with a filtered unique index on `(OperatorKey)` where outcome is active. Scan-now creates or updates `Queued` before the queue worker promotes to `InProgress` (FR-005).

**Stuck runs**: If `InProgress` exceeds the Functions host timeout plus a small grace window with no completion, **T034a** marks the run `Failed` so the filtered unique index allows a new run (document window in `host.json` and spec US3 scenario 4). No separate heartbeat entity in v1.

**Index**: `(OperatorKey, FinishedAtUtc DESC)` for "latest run" queries.

---

## RepositoryInventorySnapshot

One row per repository per run (child of `InventoryRun`).

| Field | Type | Rules |
|-------|------|--------|
| `Id` | long | PK identity |
| `InventoryRunId` | Guid | FK |
| `OperatorKey` | string | Denormalised for filtering |
| `RepositoryFullName` | string | `owner/repo` |
| `IsArchived` | bool | Display only; excluded from needs-attention roll-up when archived |
| `IsFork` | bool | Display |
| `IsPrivate` | bool | Display |
| `InstallationVisible` | bool | False → funding/checklist unset; show not visible (FR-016) |
| `ScanState` | enum (persisted) | `Checked`, `NotScanned`, `NotVisible` only on snapshot rows |
| `FailureReason` | string? | Short text when `NotScanned` |
| **Funding** | | |
| `FundingStatus` | enum? | `Missing`, `MatchesStandard`, `Differs`, `NotCompared` (file present, no standard at run start); null when not visible or not scanned |
| `ComparedPath` | string? | e.g. `.github/FUNDING.yml`, `FUNDING.yml` |
| `NotedExtraFundingPath` | string? | Other path note (FR-009) |
| `FundingPlatforms` | string? | JSON array or comma-separated platform keys |
| `OrganisationDefaultNoted` | bool | FR-011 |
| **Community Standards** (stored, not shown on page) | | |
| `HasDescription` | bool? | True when the repository description is non-empty. No organisation-default column. |
| `HasReadme` | bool? | True when the project repository has a README. No organisation-default column. |
| `CodeOfConductInRepository` | bool? | |
| `CodeOfConductOrganisationOnly` | bool? | True only when the file is absent from the project repository and present via the owner `.github` default. |
| `ContributingInRepository` | bool? | |
| `ContributingOrganisationOnly` | bool? | Same rule as code of conduct. |
| `LicenceInRepository` | bool? | |
| `LicenceOrganisationOnly` | bool? | Same rule as code of conduct. |
| `LicenceName` | string? | Project repository licence name when `LicenceInRepository` is true; otherwise the owner-default licence name when `LicenceOrganisationOnly` is true. |
| `SecurityPolicyInRepository` | bool? | |
| `SecurityPolicyOrganisationOnly` | bool? | Same rule as code of conduct. Security policy is not on the community profile payload; see research. |
| `IssueTemplatesInRepository` | bool? | |
| `IssueTemplatesOrganisationOnly` | bool? | Same rule as code of conduct. |
| `PullRequestTemplateInRepository` | bool? | |
| `PullRequestTemplateOrganisationOnly` | bool? | Same rule as code of conduct. |

All checklist columns are null when `ScanState` is `NotScanned` or `NotVisible`. For each pair, `InRepository` true requires `OrganisationOnly` false. Both false means the item is absent.

**Uniqueness**: `(InventoryRunId, RepositoryFullName)`.

---

## InventoryRunRequest (queue / coalescing)

Transient message shape (not necessarily a SQL table in v1):

| Field | Type | Purpose |
|-------|------|---------|
| `OperatorKey` | string | Target partition |
| `RequestedAtUtc` | DateTimeOffset | Audit |
| `requestedByLogin` | string | GitHub login of operator who clicked scan now (audit only; matches [inventory-queue-message.json](./contracts/inventory-queue-message.json)) |

Coalescing rule: if `InventoryRun` exists with `Outcome` in (`Queued`, `InProgress`) for operator, return already-underway without a second queue message.

---

## Derived read model (Application DTOs, not tables)

| DTO | Source |
|-----|--------|
| `OpenSourceFundingPageDto` | **Displayed run**: latest finished `Completed` or `Partial` run (not superseded) plus snapshots; **latest attempt** may be `Failed` for banner only. Application merges catalogue ids not in snapshots as synthetic rows with DTO `RowScanState.NotYetScanned` (not stored on `RepositoryInventorySnapshot`; FR-002). |
| `FundingSummaryCountsDto` | Aggregates match/differ/missing/not scanned; excludes archived from attention count (FR-012) |
| `StaleStateDto` | `FinishedAtUtc` older than 36 hours (FR-004) |

Mapping stays in Application services per ADR-0011.

---

## Retention

When a new run reaches `Completed` or `Partial`, mark prior runs `IsSuperseded = true`. Optionally delete old snapshot rows in a background step; **must not** delete the last successful/partial run on a failed attempt (spec assumptions). Implement retention as delete snapshots for superseded runs only after the new run commits.