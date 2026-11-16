---
description: "Task list for OSS funding file hygiene implementation"
---

# Tasks: OSS funding file hygiene

**Input**: Design documents from `/specs/001-oss-funding-hygiene/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Unit tests for comparison, coalescing, and DTO mapping are called out in [plan.md](./plan.md) constitution check; add them in the polish phase or alongside the owning story when implementing (not separate TDD-first tasks unless you choose that approach).

**Organization**: Tasks grouped by user story (spec priority). Foundational work blocks all stories. **US1** page read path can be validated with seeded SQL before **US3** ships; full operator journey needs **US2** + **US3**.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US4) on story-phase tasks only

## Path Conventions

Single layered repo: `src/`, `tests/` at repository root (see [plan.md](./plan.md)).

## Implementation progress (branch snapshot)

Work has started on `001-oss-funding-hygiene` before all paperwork gates complete. **Reconcile checkboxes before opening a PR.** Gates that still block merge:

| Gate | Task | Status |
|------|------|--------|
| Human package approval | T002 | Applied in `Directory.Packages.props` (implement run) |
| Open Source wireframe | T003 | `plan/wireframes/open-source-wireframe.md` |
| Extension ADR (queue + App install wiring) | T004a | `adr/adr-0047-oss-funding-inventory-topology.md` (Accepted) |

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Projects, packages, and wireframe gate before feature code.

- [X] T001 Add `src/SoloDevBoard.Inventory.Migrations/SoloDevBoard.Inventory.Migrations.csproj` referencing `SoloDevBoard.Infrastructure` and register in `SoloDevBoard.slnx` only (migrations assembly has no unit-test surface; constitution III)
- [X] T002 [P] Add central package versions in `Directory.Packages.props` for **all** new packages (EF/Aspire, queue extensions, storage clients, any JWT helper) per [research.md](./research.md); obtain explicit human approval before edit (AGENTS.md package gate; **blocks all of Phase 2 and T029**)
- [X] T003 [P] Create feature wireframe `plan/wireframes/open-source-wireframe.md` and add an index entry in `plan/wireframes/README.md` aligned with [spec.md](./spec.md) US4 and constitution IV (purpose line, toolbar, table, empty/stale/partial states, UK English labels)
- [X] T004a Record architecture decisions for EF migrations project, storage-queue scan now, ACA Job bundle, hosted scope, and GitHub App installation auth in a new ADR via `create-architectural-decision-record` (**blocks Phase 2 AppHost wiring**; extends ADR-0046; do not edit the accepted ADR body)
- [X] T004 [P] Document new queue and CRON bindings in `src/SoloDevBoard.AppHost/README.md` cross-linking [contracts/apphost-inventory-parameters.md](./contracts/apphost-inventory-parameters.md) and [quickstart.md](./quickstart.md)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain model, SQL schema, Aspire orchestration, and composition wiring. **No user story UI until this phase completes.**

**⚠️ CRITICAL**: Blocks US1–US4 implementation. Complete **T004a** and approved **T002** before T011.

- [X] T005 Add Domain entities under `src/SoloDevBoard.Domain/Entities/Inventory/` with persisted `RepositoryScanState` (`Checked`, `NotScanned`, `NotVisible`); add Application `RowScanState` DTO enum including `NotYetScanned` for synthetic rows only (constitution II)
- [X] T006 Implement `InventoryDbContext` and Fluent API entity configuration in `src/SoloDevBoard.Infrastructure/Inventory/` for entities from T005 with `OperatorKey` indexes, `(InventoryRunId, RepositoryFullName)` uniqueness, and filtered unique index enforcing one active (`Queued` or `InProgress`) run per `OperatorKey`
- [X] T007 Add `IOperatorKeyResolver` and `IOperatorInventoryTargetProvider` in `src/SoloDevBoard.Application/`: PAT → `user:{ownerLogin}`; hosted Web → `installation:{id}` from sign-in or `GitHubAuth__InstallationId`; Functions timer → configured `installation:{id}` when `gh-app-installation-id` is set (single-tenant default; no multi-installation fan-out in v1)
- [X] T008 [P] Add Application interfaces in `src/SoloDevBoard.Application/`: repositories, `IOpenSourceFundingService`, `ICanonicalFundingStandardService`, `IInventoryRunCommandService`, `IInventoryScanOrchestrator`, `IInventoryCatalogueClient`, `IGitHubOpenSourceInventoryScanner`, `IInventoryOperatorCredentials`, and Application port `IInventoryScanQueue` (Infrastructure adapter in T035) per [contracts/application-services.md](./contracts/application-services.md)
- [X] T030a Implement `IInventoryCatalogueClient` as adapter over `IRepositoryService` (open-source filter), shared by Web funding page merge and Functions orchestrator (FR-007a); hosted mode uses the same operator login as interactive catalogue listing (not `GET /installation/repositories` for scope)
- [X] T009 Implement EF repositories in `src/SoloDevBoard.Infrastructure/Inventory/` for runs, snapshots, and canonical standard (including latest-run query on `(OperatorKey, FinishedAtUtc DESC)` and supersede flag updates)
- [X] T010 Create initial EF migration in `src/SoloDevBoard.Inventory.Migrations/` reflecting [data-model.md](./data-model.md) (table prefix optional in Infrastructure configuration)
- [X] T011 Add `ProjectReference` to `SoloDevBoard.Inventory.Migrations` on AppHost; wire `Aspire.Hosting.EntityFrameworkCore`; `AddEFMigrations` on `app` with `.WithMigrationsProject<Projects.SoloDevBoard_Inventory_Migrations>()`, `.WithReference(inventory)`, `.WaitFor(inventory)`, `.RunDatabaseUpdateOnStart()`, publish bundle + ACA Job per [contracts/apphost-inventory-parameters.md](./contracts/apphost-inventory-parameters.md) in `src/SoloDevBoard.AppHost/AppHost.cs`; add `storage.AddQueues("inventory-queues")` and Web `WithReference`/`WaitFor` on that resource (confirm resource ordering against Aspire 13.6 docs; avoid circular wait with `app` child)
- [X] T012 Add `WaitForCompletion(inventory-migrations)` (or equivalent) on `app` and `functions` in `src/SoloDevBoard.AppHost/AppHost.cs` after migration resource is defined
- [X] T013 Register `AddSqlServerDbContext<InventoryDbContext>("inventory")` in `AddSoloDevBoardHost` in `src/SoloDevBoard.Composition/SoloDevBoardServiceCollectionExtensions.cs`; register `AddAzureQueueServiceClient("inventory-queues")` on Web via a Web-only extension on the same composition type; register repositories and Application inventory services in `AddSoloDevBoard` (constitution I and VI; Web MUST NOT reference Infrastructure)
- [X] T014 [P] Ensure `src/SoloDevBoard.Web/Program.cs` and `src/SoloDevBoard.Functions/Program.cs` call `AddSoloDevBoardHost()` (and Web calls queue client registration) only, with no direct `InventoryDbContext`, Infrastructure usings, or `GetConnectionString` for inventory or queues (**depends on T013**)

**Checkpoint**: `aspire start` applies migrations; `inventory` SQL healthy; Web and Functions start after migrations.

---

## Phase 3: User Story 1 - See funding hygiene without visiting each repository (Priority: P1) 🎯 MVP (read path)

**Goal**: Open Source funding section reads **stored** inventory only and surfaces missing / matches / differs / not scanned with run metadata and staleness (36 h).

**Independent Test**: Seed or complete one inventory run in SQL, open the funding view, confirm row statuses, last-finished time, stale banner when `FinishedAtUtc` is older than 36 hours, and no live GitHub calls on page load ([spec.md](./spec.md) US1).

### Implementation for User Story 1

- [X] T015 [P] [US1] Add Application DTO records alongside services in `src/SoloDevBoard.Application/Services/OpenSource/` (`OpenSourceFundingPageDto`, `InventoryRunSummaryDto`, `RepositoryFundingRowDto`, `FundingSummaryCountsDto`) per [contracts/application-services.md](./contracts/application-services.md)
- [X] T016 [US1] Implement `IOpenSourceFundingService` in `src/SoloDevBoard.Application/Services/OpenSource/OpenSourceFundingService.cs`: **displayed run header** = latest finished `Completed` or `Partial` (non-superseded) for the **current operator identity only** (FR-017; never surface another credential's snapshot as current); **latest attempt** for failed banner; **per-repo rows** = newest retained snapshot per catalogue full name (FR-004); catalogue via T030a; merge catalogue ids with no snapshot as `RowScanState.NotYetScanned` (distinct from run `NotScanned`); optional single catalogue cache refresh when cold (spec assumptions); hosted availability fields per contract
- [X] T017 [US1] Implement stale detection (`FinishedAtUtc` older than 36 hours) and partial-run presentation in `OpenSourceFundingService` (FR-004)
- [X] T018 [US1] Implement `FundingSummaryCountsDto` aggregation including `NotCompared` and excluding archived repositories from needs-attention count (counts only non-archived `Missing` and `Differs`; FR-008, FR-012) in `src/SoloDevBoard.Application/Services/OpenSource/`
- [X] T019 [P] [US1] Create Blazor components under `src/SoloDevBoard.Web/Components/Features/OpenSource/` (use `mudblazor` skill): summary chips, run metadata (scheduled vs manual, outcome, stale flag, **`AccessKind`** on displayed run per FR-016), repository table with archived/fork/private trait chips, **`FundingStatus.notCompared`** label when DTO indicates FR-003, **`FundingPlatforms`** when present (FR-013), **`WasRateLimited`** on displayed run metadata when true (FR-013, US3 scenario 4), GitHub link per row, `data-testid` on toolbar/table/scan-now, accessible names on icon-only links, `MudSkeleton` while loading, virtualised or paged table for ≤500 rows (SC-002)
- [X] T020 [US1] Implement empty, stale, failed-latest-attempt, standard-drift (`StandardDriftWarning` + compared-at labelling, FR-019), and hosted-unavailable banners using MudBlazor `MudAlert` per wireframe (one outcome → snackbar **or** inline alert, not both)
- [X] T021 [US1] Render `NotScanned` rows with short `FailureReason` and preserve successful rows on partial runs (FR-003 scenario 5) in Open Source funding table components

**Checkpoint**: With seeded SQL data, funding hygiene view is correct without scanner running.

---

## Phase 4: User Story 2 - Keep one standard funding file (Priority: P1)

**Goal**: Operator saves one canonical `FUNDING.yml`; inventory compares repository text with normalised line endings; page warns when displayed rows used an older standard hash.

**Independent Test**: Save standard, run inventory (or seed snapshots), confirm missing / matches / differs; change standard and confirm warning until next run finishes ([spec.md](./spec.md) US2).

### Implementation for User Story 2

- [X] T022 [P] [US2] Add `CanonicalFundingStandardDto`, `SaveCanonicalFundingStandardRequest` in `src/SoloDevBoard.Application/Services/OpenSource/`
- [X] T023 [US2] Implement `ICanonicalFundingStandardService` in `src/SoloDevBoard.Application/Services/OpenSource/CanonicalFundingStandardService.cs` with upsert on `OperatorKey`; reject empty or whitespace-only `Content` (FR-010); set `SavedAtUtc` and `ContentHash` (SHA-256 of normalised content)
- [X] T024 [US2] Implement `FundingFileComparer` in `src/SoloDevBoard.Application/Services/OpenSource/FundingFileComparer.cs`: CRLF→LF when line endings are uniform; mixed CRLF/LF or leading BOM is differs; ignore at most one trailing newline; 64 KiB max compare size (FR-010)
- [X] T025 [US2] Set `StandardDriftWarning` on `OpenSourceFundingPageDto` when current standard hash differs from hash used for displayed run (FR-019). Persist `StandardContentHashAtStart` on `InventoryRun` at promotion to `InProgress` (implemented with T034, not at `Queued` only)
- [X] T026 [US2] MudBlazor canonical editor component in `src/SoloDevBoard.Web/Components/Features/OpenSource/CanonicalFundingStandardEditor.razor` with validation message for empty standard; block treating repos as match/differ when no standard saved (US2 scenario 1)
- [X] T027 [US2] When no canonical standard exists, show prompt on Open Source page before funding match/differ labels (integrate editor + page DTO in `src/SoloDevBoard.Web/Components/Features/OpenSource/`)

**Checkpoint**: Standard save, comparison rules, and drift warning behave per US2 acceptance scenarios.

---

## Phase 5: User Story 3 - Inventory open-source repositories while the product is closed (Priority: P1)

**Goal**: Scheduled CRON and scan-now queue walk open-source-topic repos, persist funding + Community Standards facts, coalesce concurrent runs, support PAT and installation credentials.

**Independent Test**: Scan now ACK without blocking; scheduled timer without browser; duplicate scan coalesced; partial and rate-limited outcomes stored ([spec.md](./spec.md) US3, [quickstart.md](./quickstart.md) §4–§6).

### Implementation for User Story 3

- [X] T028 [P] [US3] Add AppHost parameters per [contracts/apphost-inventory-parameters.md](./contracts/apphost-inventory-parameters.md) (`inventory-scheduled-cron`, `gh-app-id`, `gh-app-private-key`, optional `gh-app-installation-id`, `hosted-inventory-enabled` on Web + Functions) in `src/SoloDevBoard.AppHost/AppHost.cs`; publish mode Key Vault `GetSecret` bindings for `gh-app-id` and `gh-app-private-key` per T051
- [X] T029 [US3] Implement `IGitHubInstallationTokenProvider` in `src/SoloDevBoard.Infrastructure/GitHub/` with async mint and in-memory cache until expiry (Functions only receives private key; FR-015)
- [X] T029a [US3] Implement `IInventoryOperatorCredentials` and register Functions-only per-target scope factory (one `OperatorKey` per queue/timer message); supply cached installation or PAT token to existing GitHub clients without sync `.Wait()` in `GetAccessToken()`
- [X] T030 [US3] Walk scope from T030a; per repo probe `GET /repos/{owner}/{repo}` and persist `NotVisible` on 404/403 before funding file fetches (FR-016); include archived, fork, private traits from catalogue (FR-008); write explicit `NotScanned` snapshot rows for unchecked tail on `Partial` runs (US3 scenario 5)
- [X] T031 [US3] Implement `IGitHubOpenSourceInventoryScanner` in `src/SoloDevBoard.Infrastructure/Inventory/` fetching `.github/FUNDING.yml` then root `FUNDING.yml` (precedence FR-009); note extra root path when both exist; optional organisation default note without counting as match (FR-011)
- [X] T032 [US3] Record `FundingPlatforms` from line-scan of `platforms:` keys (no YAML parser); when no standard saved at run start, persist `FundingStatus.NotCompared` for repos with a funding file (FR-003); empty/invalid text → differs when standard exists; unreadable or >64 KiB → `NotScanned` with reason
- [ ] T033 [US3] Persist Community Standards fields on `RepositoryInventorySnapshot` in `src/SoloDevBoard.Infrastructure/Inventory/` per [data-model.md](./data-model.md) (FR-014): `HasDescription` (non-empty description), `HasReadme` (project README only; no organisation-only column); for code of conduct, contributing, licence, security policy, issue templates, and pull request template record `*InRepository` and `*OrganisationOnly` (`OrganisationOnly` true only when absent from the project repository and present via owner `.github` default; `InRepository` true requires `OrganisationOnly` false); `LicenceName` is the project licence when `LicenceInRepository` else the owner-default name; all checklist columns null when `ScanState` is `NotScanned` or `NotVisible`; non-fork repos use community profile classification per [research.md](./research.md); forks skip profile and probe files via Contents API; security policy probes `SECURITY.md` in `.github/`, root, `docs/`, then owner `.github` when missing in project repo; do not store file bodies
- [X] T034 [US3] Implement `IInventoryScanOrchestrator` in `src/SoloDevBoard.Application/Services/OpenSource/InventoryScanOrchestrator.cs`: promote `Queued`→`InProgress` and set `StandardContentHashAtStart` at promotion; shared entry for timer and queue so scheduled and manual triggers do not both walk the catalogue (spec edge case: one completes, the other skipped or coalesced); set `WasRateLimited` on the run when throttling retried inside the same pass (US3 scenario 4, FR-013); respect `host.json` `functionTimeout` (default 10 min on Consumption) minus **120 s** safety margin for `Partial`; persist `ResumeAfterRepositoryFullName` and tail `NotScanned` rows on partial finish; resume next run after that cursor (rotate on wrap, US3 scenario 5); supersede prior run headers without deleting snapshots for repos not re-checked (data-model retention)
- [X] T034a [US3] Recover stuck `Queued`/`InProgress` runs older than timeout plus grace (mark `Failed` with reason) so filtered unique index does not block future runs; unit test in T048
- [X] T035 [US3] Implement `IInventoryScanQueue` in `src/SoloDevBoard.Infrastructure/Inventory/` enqueueing JSON matching [contracts/inventory-queue-message.json](./contracts/inventory-queue-message.json) to queue `inventory-scan-request` with `CreateIfNotExists` and default base64 message encoding
- [X] T036 [US3] Implement `IInventoryRunCommandService.RequestScanNowAsync`: insert or keep single `Queued`/`InProgress` per operator (DB filtered unique index); enqueue; on enqueue failure mark run `Failed` or delete `Queued` row so the index is not wedged; return `AlreadyUnderway` or `Unavailable` when hosted inventory not configured (FR-005, FR-015)
- [X] T037 [US3] Replace `InventoryPlaceholderTimerFunction.cs` with `InventoryTimerFunction.cs` using `[TimerTrigger("%Inventory__ScheduledCron%")]` enumerating operators from T007; when hosted credential missing or mint fails, write `Failed` run with reason for page banner (FR-015)
- [X] T038 [US3] Add shared `InventoryFunctionsWorker.cs` plus `InventoryQueueFunction.cs` (`[QueueTrigger("inventory-scan-request")]` with no `Connection`) and wire both timer and queue handlers to resolve one scope per message and invoke `IInventoryScanOrchestrator` in `src/SoloDevBoard.Functions/`
- [X] T039 [US3] Confirm Blazor uses `IInventoryRunCommandService` (T046), not queue types (constitution I); AppHost queue wiring lives in T011/T013 only

**Checkpoint**: Manual and scheduled runs persist snapshots; page shows new last-finished time after refresh; coalescing and partial runs match US3.

---

## Phase 6: User Story 4 - Find funding hygiene on a shared Open Source page (Priority: P2)

**Goal**: Main nav **Open Source** route with funding hygiene as first real section; no fake future sections; Repositories page unchanged for classification.

**Independent Test**: One nav step from any signed-in page; page describes open-source hygiene not a second catalogue ([spec.md](./spec.md) US4).

### Implementation for User Story 4

- [X] T042 [P] [US4] Add route `/open-source` (or `/open-source/funding` with redirect) in `src/SoloDevBoard.Web/Components/Features/OpenSource/OpenSourcePage.razor` per [research.md](./research.md)
- [X] T043 [US4] Add **Open Source** item to main navigation in `src/SoloDevBoard.Web/` (shared layout/nav component used by operational pages) pointing to funding section first (FR-001, SC-006)
- [X] T044 [US4] Apply MudBlazor page chrome per `plan/wireframes/open-source-wireframe.md` on `OpenSourcePage.razor` (depends on T003; narrow-viewport layout per constitution IV)
- [X] T045 [US4] Ensure copy states classification remains on Repositories (`open-source` topic); do not add stub Community Standards section (FR-018, US4 scenario 3)
- [X] T046 [US4] Connect scan-now toolbar button to `IInventoryRunCommandService` with `ISnackbar` acknowledgement only (no blocking wait) in `src/SoloDevBoard.Web/Components/Features/OpenSource/`

**Checkpoint**: Operator reaches funding hygiene in one nav step; chrome matches product UI language.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Tests, docs, secrets inventory, and validation gates.

- [X] T047 [P] Add Application unit tests for `FundingFileComparer` (uniform line endings, mixed endings, BOM, trailing newline, size limit) in `tests/SoloDevBoard.Application.Tests/OpenSource/`
- [ ] T048 [P] Add Application unit tests for scan-now and timer coalescing (`Queued`/`InProgress` races, timer firing while manual run in progress, T034a recovery), **FR-017** operator partition and credential-identity rules (wrong `OperatorKey` never returned as displayed run; PAT vs hosted snapshots do not bleed), catalogue scope via T030a (not installation list), partial-run resume index, and not-visible mapping in `tests/SoloDevBoard.Application.Tests/OpenSource/` (include `OpenSourceFundingService` display rules from T016)
- [ ] T049 [P] Add Infrastructure unit tests for funding path precedence, repository metadata `NotVisible` vs missing-file 404, and snapshot mapping in `tests/SoloDevBoard.Infrastructure.Tests/Inventory/`; assert Community Standards dual columns (`*InRepository` / `*OrganisationOnly`) and fork vs non-fork probe paths per T033 (FR-014, FR-016, SC-007 stored facts without live GitHub on page load)
- [ ] T050 [P] Add bUnit tests for empty, stale, and partial states in `tests/SoloDevBoard.Web.Tests/` under `OpenSource/`
- [ ] T051 Update `plan/KEY_VAULT_SECRET_INVENTORY.md` with `gh-app-id` and `gh-app-private-key` publish secrets per [research.md](./research.md) open dependency
- [ ] T052 [US4] Update user guide under `website/content/docs/`, `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`, and `tests/SoloDevBoard.E2E.Tests/CRITICAL_JOURNEYS.md` for Open Source funding journey with US4 (constitution III; not deferred polish)
- [ ] T052a [US4] Add Playwright test for Open Source nav → funding view (include axe pass for page chrome if tier allows per existing E2E conventions) in `tests/SoloDevBoard.E2E.Tests/`
- [ ] T053 Run validation steps in [quickstart.md](./quickstart.md) including seeded 500-row Open Source load timing (SC-002) and scan-now ACK timing with a large catalogue (SC-004); confirm `dotnet format`, Release build, and `SoloDevBoard.UnitTests.slnf` pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → User story phases (3–6) → **Polish (Phase 7)**
- **US1** read path (Phase 3) can start after Phase 2 using seeded data; production data requires **US3**
- **US2** comparison (Phase 4) is required before **US3** scanner classifies matches/differs
- **US4** (Phase 6) integrates US1 UI components into routed page and nav (after T019–T021 exist)

### User Story Dependencies

| Story | Depends on | Independent test notes |
|-------|------------|-------------------------|
| **US1** | Phase 2; data from US3 or SQL seed | Page load never calls GitHub |
| **US2** | Phase 2 | Standard save + comparer without full scan |
| **US3** | Phase 2, US2 comparer + standard hash | Timer/queue without Web |
| **US4** | US1 components, US3 scan now optional | Nav + chrome only |

### Parallel Opportunities

- Phase 1: T002, T003, T004 in parallel after T001
- Phase 2: T008 parallel with T006; T014 after T013 (parallel with T011/T012 once T010 completes)
- Phase 3: T015 parallel with T019
- Phase 7: T047–T052 largely parallel

---

## Parallel Example: User Story 1

```text
T015: DTOs in Application
T019: Blazor table/summary components (stub DTOs until T016 completes)
Then T016 → T017 → T018 wire service; T020–T021 complete states
```

---

## Parallel Example: User Story 3

```text
T028: AppHost parameters (parallel with T029 installation token provider)
T030a → T030: Catalogue adapter then scope walk
T031–T033: Scanner implementation
T037–T038: Functions triggers after T034 orchestrator exists
```

---

## Implementation Strategy

### MVP First (User Story 1 read path + minimal data)

1. Complete Phase 1–2 (schema + migrations).
2. Complete Phase 3 (US1) with **manual SQL seed** of one run and snapshots.
3. Validate US1 independent test before building scanner.
4. Add Phase 4 (US2) + Phase 5 (US3) for real inventory.
5. Phase 6 (US4) for nav and shipped UX.

### Incremental Delivery

1. Foundation → seeded US1 demo → US2 standard → US3 overnight inventory → US4 nav polish → Phase 7 tests/docs.

### Suggested MVP scope

**US1** after Phase 2 with seeded inventory proves the product promise from issue #439 UI; **US3** is the minimum to replace seeds in production.

---

## Notes

- All user-facing strings: UK English (FR-020).
- Do not call GitHub from Open Source page components (FR-002).
- Community Standards fields are stored on snapshots only; not rendered on this page (FR-014, FR-018).
- Wireframe in `plan/wireframes/open-source-wireframe.md` is a constitution gate before UI merge (plan constitution check IV).