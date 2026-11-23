---
description: "Task list for feature implementation"
---

# Tasks: Operator repository snapshot

**Input**: Design documents from `/specs/001-operator-snapshot-scanner/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md), [contracts/](contracts/)

**Tests**: Included per [quickstart.md](quickstart.md) and constitution Principle III (store contract, application behaviour, bUnit for Repositories). AppHost graph is not unit-tested.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Single project layout: `src/`, `tests/` at repository root (see [plan.md](plan.md)).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Binding decision and dependency approval before storage wiring or Azure client packages.

- [X] T001 Create and accept `adr/adr-0048-hosted-operator-snapshot-blob-storage.md` using the ADR skill (status Accepted before any AppHost storage edit)
- [X] T002 Update `adr/README.md` index row for ADR-0048 in the same change as T001
- [X] T003 Obtain explicit approval, then add `Aspire.Hosting.Azure.Storage` 17.0.0, `Aspire.Azure.Storage.Blobs` 17.0.0, and `Azure.Storage.Blobs` (version aligned to the client package) to `Directory.Packages.props` and reference them from `SoloDevBoard.AppHost`, `SoloDevBoard.Web`, and `SoloDevBoard.Infrastructure` per [research.md](research.md)
- [X] T004 Confirm feature branch `001-operator-snapshot-scanner` is current (`.specify/feature.json` or `speckit-git-validate` skill)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Document model, store port, adapters, GitHub catalogue fields, collection coordinator, and web registration. No user story UI work until this phase completes.

**Checkpoint**: Store contract tests pass with fakes; collection service can run a walk off the circuit in tests. New public Application and Infrastructure members include XML documentation per constitution Principle I.

- [X] T005 [P] Add `GitHubId` (`long`), `OwnerLogin`, and `IsFork` to `src/SoloDevBoard.Domain/Entities/Repositories/Repository.cs` (keep existing `Id` for current callers)
- [X] T006 Map `fork`, `owner.login`, and 64-bit id from the existing list DTO in `src/SoloDevBoard.Infrastructure/GitHub/GitHubService.cs` (no new GitHub client)
- [X] T007 [P] Add Application document and entry records matching [contracts/operator-snapshot-document.md](contracts/operator-snapshot-document.md) under `src/SoloDevBoard.Application/Services/Snapshots/` including `data-model.md` fields `schemaVersion`, `operatorLogin`, `revision`, `lastSuccessfulCollectionAt`, `run`, `repositories`, and `laterFacts`
- [X] T008 [P] Add `OperatorSnapshotStatusDto`, durability kind enum, run state enum, and short-reason mapping types in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T009 Add `IOperatorSnapshotStore` in `src/SoloDevBoard.Application/Services/Snapshots/` per [contracts/operator-snapshot-store.md](contracts/operator-snapshot-store.md) (`GetAsync`, `SaveAsync`, concurrency token)
- [X] T010 [P] Implement login sanitisation and unsafe-key rejection in `src/SoloDevBoard.Infrastructure/Snapshots/` (grammar per `data-model.md` Identity and names)
- [X] T011 Implement file adapter in `src/SoloDevBoard.Infrastructure/Snapshots/` with temp-then-replace and per-path lock using `data-model.md` `OperatorSnapshotDocument.revision`
- [X] T012 Implement blob adapter in `src/SoloDevBoard.Infrastructure/Snapshots/` with injected `BlobContainerClient`, `IfMatch` / `IfNoneMatch`, and `data-model.md` `OperatorSnapshotDocument.revision`
- [X] T013 [P] Add scoped background credential slot and scope factory so `ICurrentUserContext` serves captured login and token in a new scope in `src/SoloDevBoard.Infrastructure/Snapshots/` per [research.md](research.md)
- [X] T014 Implement `IOperatorSnapshotCollectionService` (`StartAsync`, `GetStatusAsync`) and process-local coordinator with `ConcurrentDictionary`, host stopping token, and reason mapping in `src/SoloDevBoard.Application/Services/Snapshots/` per [contracts/operator-snapshot-collection.md](contracts/operator-snapshot-collection.md); the walk MUST call `GetRepositoriesAsync(forceReload: true)` (list APIs only; spec FR-023, no GitHub writes); on start persist `data-model.md` `run.status` `inProgress`, `run.startedAt`, and `run.repositoriesRecorded` 0
- [X] T015 Wire store adapters, collection service, and coordinator singleton construction in `src/SoloDevBoard.Composition/SoloDevBoardServiceCollectionExtensions.cs` (no connection string lookup in Composition)
- [X] T016 Register conditional `AddAzureBlobContainerClient("operator-snapshots")`, durability options, and pass `IHostApplicationLifetime.ApplicationStopping` into the coordinator from `src/SoloDevBoard.Web/Program.cs` (blob adapter registered before `AddSoloDevBoard`; file adapter via `TryAdd` per [contracts/operator-snapshot-store.md](contracts/operator-snapshot-store.md))
- [X] T017 [P] Add store contract tests (isolation, unsafe keys, conditional save, never run vs failed-first, in-progress to interrupted on read; unknown `data-model.md` `schemaVersion` must not delete the body) in `tests/SoloDevBoard.Infrastructure.Tests/` using fake file system and fake blob per [quickstart.md](quickstart.md)
- [X] T018 [P] Add application tests for failed run keeping last success, interruption read path, and empty success vs never run in `tests/SoloDevBoard.Application.Tests/`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel.

---

## Phase 3: User Story 1 - Collect the current catalogue (Priority: P1) MVP

**Goal**: Operator starts collection from `/repositories`; successful run stores one JSON snapshot and shows success time and count without listing entries.

**Independent Test**: Sign in with a known catalogue, start collection, compare finished snapshot to that catalogue and page count (see [spec.md](spec.md) US1).

### Implementation for User Story 1

- [X] T019 [US1] Map successful walk to `succeeded` run with `data-model.md` `OperatorSnapshotDocument.lastSuccessfulCollectionAt`, `run.finishedAt`, `run.repositoriesRecorded` (final count), and replace `repositories` from catalogue in `src/SoloDevBoard.Application/Services/Snapshots/` (one entry per `data-model.md` `RepositoryEntry.githubId`; read-only against GitHub per spec FR-023)
- [X] T020 [US1] Serialize repository entries with `data-model.md` `RepositoryEntry` fields (`githubId`, `owner`, `name`, `visibility`, `archived`, `fork`, `htmlUrl`) in Infrastructure JSON writer used by both adapters
- [X] T021 [P] [US1] Add **Collect snapshot** control calling `StartAsync` in `src/SoloDevBoard.Web/Components/Features/Repositories/Pages/Repositories.razor` per [contracts/repositories-page.md](contracts/repositories-page.md)
- [X] T022 [US1] Ensure page open and **Reload from GitHub** do not call `StartAsync` in `src/SoloDevBoard.Web/Components/Features/Repositories/Pages/Repositories.razor`
- [X] T023 [US1] Render succeeded state copy (finish time, snapshot count, no entry list) in `MudAlert` on `Repositories.razor` per [contracts/repositories-page.md](contracts/repositories-page.md)
- [X] T024 [P] [US1] Add bUnit tests: open does not collect, reload does not collect, success status sentences (SC-003 finish time), and no run-history affordance (spec FR-013) in `tests/SoloDevBoard.Web.Tests/` for Repositories page

**Checkpoint**: User Story 1 deliverable - collect and read success status on one machine with local file adapter.

---

## Phase 4: User Story 2 - Include a private repository (Priority: P1)

**Goal**: Snapshot includes private, archived, and fork repositories; each entry records visibility and both flags independently.

**Independent Test**: Account with private, archived, fork, and both-archived-and-fork repos; one successful collection; verify count and entry fields ([spec.md](spec.md) US2).

### Implementation for User Story 2

- [X] T025 [US2] Map `IsPrivate` to `data-model.md` `RepositoryEntry.visibility` (`public` | `private`), `IsArchived` to `RepositoryEntry.archived`, and `IsFork` to `RepositoryEntry.fork` in the collection success path in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T026 [US2] Collapse duplicate `data-model.md` `RepositoryEntry.githubId` values to one entry during success write in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T027 [P] [US2] Add application or infrastructure test proving private, archived, fork, and both flags round-trip in JSON in `tests/SoloDevBoard.Application.Tests/` or `tests/SoloDevBoard.Infrastructure.Tests/`

**Checkpoint**: Full catalogue fidelity for later features reading the document.

---

## Phase 5: User Story 3 - See progress while still working (Priority: P1)

**Goal**: In-progress status with rising count on the page; operator can use other pages; no second concurrent run.

**Independent Test**: Multi-repo account, stay on page and see count rise; leave page and use another feature; cannot start second run ([spec.md](spec.md) US3).

### Implementation for User Story 3

- [X] T028 [US3] Overlay in-memory `data-model.md` `run.repositoriesRecorded` and `run.startedAt` on `GetStatusAsync` when coordinator owns live task in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T029 [US3] Return `AlreadyInProgress` from `StartAsync` when `data-model.md` `run.status` is `inProgress` and a live task exists in the coordinator dictionary in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T030 [US3] Add `PeriodicTimer` (~1s) polling `GetStatusAsync` while in progress and dispose on component teardown in `src/SoloDevBoard.Web/Components/Features/Repositories/Pages/Repositories.razor`
- [X] T031 [US3] Disable **Collect snapshot** while status is `InProgress` in `Repositories.razor`
- [X] T032 [US3] Render in-progress copy with start time and recorded count in `MudAlert` on `Repositories.razor` per [contracts/repositories-page.md](contracts/repositories-page.md)
- [X] T033 [P] [US3] Add tests in `tests/SoloDevBoard.Web.Tests/` or `tests/SoloDevBoard.Application.Tests/`: in-progress status refreshes without full reload; operator can use another in-product page while a walk runs (spec FR-008, SC-007)

**Checkpoint**: Progress visible without blocking the circuit or the rest of the product.

---

## Phase 6: User Story 4 - See that no snapshot exists yet (Priority: P2)

**Goal**: Distinguish never collected from succeeded with zero repositories.

**Independent Test**: Never-run operator vs zero-repo success ([spec.md](spec.md) US4).

### Implementation for User Story 4

- [X] T034 [US4] Map missing document to `NeverRun` DTO without inventing finish time or count in `GetStatusAsync` in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T035 [US4] Map success with zero `repositories` length to succeeded UI with count zero in `Repositories.razor` per [contracts/repositories-page.md](contracts/repositories-page.md)
- [X] T036 [P] [US4] Extend bUnit tests for never-run vs zero-success copy in `tests/SoloDevBoard.Web.Tests/`

**Checkpoint**: Operator and later features can tell absence from empty success.

---

## Phase 7: User Story 5 - Keep the last success when a run fails (Priority: P2)

**Goal**: Failed run shows short reason and time; previous snapshot unchanged; no run history.

**Independent Test**: Keep success, force sign-in, rate limit, or unexpected failure; verify reason and unchanged snapshot ([spec.md](spec.md) US5).

### Implementation for User Story 5

- [X] T037 [US5] Map exceptions to `data-model.md` `run.reason` values `signInOrToken`, `rateLimit`, and `unexpected` (optional `data-model.md` `run.detail` one line: exception type and status code only, no body, token, or secret) in collection task in `src/SoloDevBoard.Application/Services/Snapshots/` per [research.md](research.md)
- [X] T038 [US5] On failure, update `run` only and preserve `repositories` and `lastSuccessfulCollectionAt` in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T039 [US5] Render failed state with operator-facing sentences in `MudAlert` on `Repositories.razor` per [contracts/repositories-page.md](contracts/repositories-page.md); when `lastSuccessfulCollectionAt` exists, show last success finish time and count (spec FR-006)
- [X] T040 [P] [US5] Add application tests for each failure reason keeping last success in `tests/SoloDevBoard.Application.Tests/`

**Checkpoint**: Failed refresh does not wipe the kept snapshot.

---

## Phase 8: User Story 6 - Recover after the site stops mid-run (Priority: P2)

**Goal**: After process stop, next visit shows interrupted with site-stopped reason; new collection allowed; last success unchanged.

**Independent Test**: Start collection, stop AppHost, reopen page ([spec.md](spec.md) US6).

### Implementation for User Story 6

- [X] T041 [US6] On host stopping cancellation, attempt terminal save with `data-model.md` `run.status` `interrupted` and `run.reason` `siteStopped` in coordinator in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T042 [US6] Implement store `GetAsync` path that rewrites stale `inProgress` to `interrupted` when no live task in `src/SoloDevBoard.Infrastructure/Snapshots/` per [contracts/operator-snapshot-store.md](contracts/operator-snapshot-store.md)
- [X] T043 [US6] Render interrupted copy in `MudAlert` on `Repositories.razor` per [contracts/repositories-page.md](contracts/repositories-page.md); when `lastSuccessfulCollectionAt` exists, show last success finish time and count (spec FR-006)
- [X] T044 [P] [US6] Extend store and application tests for in-progress document without live task becoming `interrupted` and allowing new start in `tests/SoloDevBoard.Infrastructure.Tests/` and `tests/SoloDevBoard.Application.Tests/`

**Checkpoint**: No stuck in-progress after a stop.

---

## Phase 9: User Story 7 - Keep each operator's snapshot to themselves (Priority: P1)

**Goal**: Documents keyed by sanitised login; operator B never sees operator A's status or entries.

**Note**: P1 isolation must be complete (this phase or equivalent coverage from T010 and T017) before merge when the release includes private catalogues or multi-operator hosting.

**Independent Test**: Collect as A, sign in as B, confirm isolation ([spec.md](spec.md) US7).

### Implementation for User Story 7

- [X] T045 [US7] Ensure all store operations key only on sanitised `ICurrentUserContext.OwnerLogin` with no login argument from Web in `src/SoloDevBoard.Application/Services/Snapshots/` and Infrastructure adapters
- [X] T046 [P] [US7] Strengthen two-login isolation assertions in store contract tests in `tests/SoloDevBoard.Infrastructure.Tests/`
- [X] T047 [US7] Verify hosted claim and PAT owner login paths use distinct keys in integration-style unit test in `tests/SoloDevBoard.Infrastructure.Tests/` (no live GitHub)

**Checkpoint**: Private catalogue data stays per operator.

---

## Phase 10: User Story 8 - Collect on demand locally, and keep the snapshot when hosted (Priority: P2)

**Goal**: Durability copy on page; local file survives restart on this machine; hosted blob survives sign-out and restart.

**Independent Test**: Lean quickstart section 2 and 3; hosted sign-in after ADR ([spec.md](spec.md) US8).

### Implementation for User Story 8

- [X] T048 [US8] Expose `DurabilityKind` on status DTO from web options (`LocalMachine`, `EphemeralContainerDisk`, `HostedBlob`) in `src/SoloDevBoard.Web/Program.cs` and collection service
- [X] T049 [US8] Render schedule sentence and durability-specific second sentence in `Repositories.razor` per [contracts/repositories-page.md](contracts/repositories-page.md) (no "PAT-only" denial phrasing)
- [X] T050 [US8] Add hosted sign-in storage to `src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs` only after T001: `AddAzureStorage("operator-snapshot-storage").RunAsEmulator()`, `AddBlobContainer("operator-snapshots", "operator-snapshots")`, `WithReference` from web; publish omits `RunAsEmulator` per [research.md](research.md)
- [X] T051 [US8] Confirm `src/SoloDevBoard.AppHost/LeanAppHostGraph.cs` remains without storage, Azurite, SQL, or Functions
- [X] T052 [P] [US8] Update `docs/getting-started.md`, `docs/deployment.md`, and `src/SoloDevBoard.AppHost/README.md` for hosted storage in the same change as T050
- [X] T053 [P] [US8] Add bUnit assertions for durability copy variants in `tests/SoloDevBoard.Web.Tests/`

**Checkpoint**: Lean and hosted operator-facing durability matches spec.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: User guide, E2E alignment, chrome, and merge gates.

- [X] T054 [P] Update shipped behaviour in `website/content/docs/repositories.md` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`; **same change** as T055 (constitution Principle III)
- [ ] T055 Recapture `website` screenshot `repositories/overview.png` after command strip and status region ship; **same change** as T054 (do not merge Repositories UI without both)
- [X] T056 Update Playwright repositories command-strip and 390px overflow tests in `tests/SoloDevBoard.E2E.Tests/` if new control changes assertions (no live GitHub collection journey)
- [X] T057 Ensure **Collect snapshot** stays visible on narrow viewports (not overflow-only) in `Repositories.razor` per [contracts/repositories-page.md](contracts/repositories-page.md)
- [X] T058 Run `dotnet format SoloDevBoard.slnx --verify-no-changes`, `dotnet build SoloDevBoard.slnx -c Release -warnaserror`, and `dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build` per [plan.md](plan.md)
- [X] T059 Walk [quickstart.md](quickstart.md) lean sections 1-2 manually and record any gaps in the PR test plan

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start immediately. T001 blocks T050 (AppHost storage).
- **Foundational (Phase 2)**: Depends on T003 for blob types; file-only work can start after T001 without T003 for lean path.
- **User Stories (Phases 3-10)**: Depend on Phase 2 completion.
  - US1 (Phase 3) is MVP; US2-7 largely extend the same collection and UI with focused tests.
  - US8 (Phase 10) depends on T001 Accepted and T003 packages.
- **Polish (Phase 11)**: Depends on UI stories that change the Repositories page (at minimum Phases 3, 5, 6, 8, 9).

### User Story Dependencies

| Story | Priority | Depends on | Notes |
| --- | --- | --- | --- |
| US1 | P1 | Phase 2 | MVP: collect + success UI |
| US2 | P1 | Phase 2, US1 success path | Mapping and JSON shape |
| US3 | P1 | Phase 2, US1 start control | Polling and single-flight |
| US4 | P2 | Phase 2, US1 status UI | DTO and copy |
| US5 | P2 | Phase 2 | Failure writes |
| US6 | P2 | Phase 2 | Interrupt detection |
| US7 | P1 | Phase 2 | Store keys and tests; finish before P1 release if private repos or multi-operator |
| US8 | P2 | Phase 1, Phase 2, US1 | Hosted blob and docs |

Stories US4-US6 can proceed in parallel after Phase 2 if staffed separately; all touch `Repositories.razor` last-writer should coordinate.

### Within Each User Story

- Application and store behaviour before page copy for that state
- bUnit after corresponding Razor changes
- AppHost edits only after ADR-0048 Accepted

### Parallel Opportunities

- T005, T007, T008, T010, T013, T017, T018 in Phase 2
- T021, T024 in US1
- T027 in US2 after T025-T026
- T033, T036, T040, T044, T046, T053 marked [P]
- After Phase 2, US2, US5, US6, and US7 tests can run in parallel with UI work on US3-US4 if different owners

---

## Parallel Example: User Story 1

```bash
# After Phase 2, sequential core:
# T019 — success write logic
# T020 — JSON entry serialization
# T022 — reload does not start collection
# T023 — succeeded MudAlert copy

# Then in parallel when T023 is in place:
# T021 — Collect snapshot button in Repositories.razor
# T024 — bUnit open/reload/success tests (incl. SC-003 finish time on page)
```

---

## Parallel Example: Foundational store layer

```bash
# In parallel after T009:
# T010 — sanitisation
# T011 — file adapter
# T012 — blob adapter (needs T003)
# T017 — contract tests (against T011/T012 fakes)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (T001-T002 minimum for lean; defer T003 until hosted work).
2. Complete Phase 2 through file adapter and T017 file-side tests.
3. Complete Phase 3 (User Story 1).
4. **STOP and VALIDATE**: [quickstart.md](quickstart.md) section 2 on lean `aspire start`.
5. Add US2-US3 before merge if P1 stories are required for release.

### Incremental Delivery

1. Setup + Foundational → store and coordinator test-green.
2. US1 → demo local collect (MVP).
3. US2 + US3 → full P1 operator experience.
4. US4-US6 → failure and edge states (P2).
5. US7 → isolation hardening (can land with Phase 2 tests earlier).
6. US8 → hosted durability and AppHost (after ADR and packages).
7. Polish → guide, screenshots, CI gates.

### Parallel Team Strategy

1. Developer A: Phase 2 store and adapters (T010-T012, T017).
2. Developer B: Phase 2 collection and GitHub mapping (T005-T006, T014).
3. Developer C: Phase 2 Web registration (T016) then US1-US3 UI.
4. After MVP: Developer A takes US8 AppHost; Developer B US5-US6; Developer C guide and Playwright (Phase 11).

---

## Notes

- Do not edit `.aspire/modules/`. Do not add AppHost storage before ADR-0048 is Accepted (T001).
- Token and document body must not appear in logs or UI.
- UK English in operator-facing strings; no em dash.
- `[P]` tasks = different files, no incomplete upstream dependency.
- Suggested MVP scope: **Phase 1 (ADR + lean path)**, **Phase 2**, **Phase 3 (US1)**, plus **US3** if release requires progress UX.

---

## Extension Hooks

**Optional Pre-Hook**: git
Command: `/speckit-git-commit`
Description: Auto-commit before task generation

Prompt: Commit outstanding changes before task generation?
To execute: `/speckit-git-commit`

## Phase 12: Convergence

- [X] T060 Complete store contract tests promised by T017: conditional-save concurrency conflict, unsupported `schemaVersion` read without deleting the file body, never-run versus failed-first semantics, and blob adapter coverage with a fake container in `tests/SoloDevBoard.Infrastructure.Tests/` per `contracts/operator-snapshot-store.md` and `quickstart.md` (partial)
- [X] T061 Increment live `repositoriesRecorded` during the paged catalogue fetch in `OperatorSnapshotCollectionCoordinator` (or a narrow walk callback) so the in-progress count rises before the walk finishes for multi-repository accounts per FR-007, US3/AC2, and SC-004 (partial)

## Phase 13: Convergence

- [X] T062 Mirror remaining file-side store contract tests on the blob adapter in `tests/SoloDevBoard.Infrastructure.Tests/OperatorSnapshotBlobStoreTests.cs`: unsafe login rejection, missing document (never run), failed-first versus never-run, and unsupported `schemaVersion` read, per `contracts/operator-snapshot-store.md` shared adapter checklist (partial)