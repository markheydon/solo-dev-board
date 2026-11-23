---
description: "Task list template for feature implementation"
---

# Tasks: Scheduled snapshot collection

**Input**: Design documents from `/specs/002-scheduled-snapshot-collection/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Unit and bUnit tasks are included because the constitution and quickstart.md require behaviour to be locked in the same change as user-visible updates.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Single ASP.NET solution under `src/` and `tests/` at repository root (see plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Security gates and dependency approval before secret-bearing code

- [X] T001 Confirm active branch is `002-scheduled-snapshot-collection` and feature directory is `specs/002-scheduled-snapshot-collection/`
- [X] T002 Obtain explicit user approval to add `Azure.Extensions.AspNetCore.DataProtection.Blobs` to `Directory.Packages.props` per `AGENTS.md` (block implementation tasks that write refresh tokens until approved)
- [X] T003 [P] Read `specs/002-scheduled-snapshot-collection/quickstart.md` and note filter targets for incremental `dotnet test` during implementation

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: ADR, persistence contracts, and additive snapshot fields that every user story depends on

**CRITICAL**: No user story work can begin until this phase is complete (except T002 approval and T004 ADR may precede package add)

- [X] T004 Accept binding ADR `adr/adr-0049-hosted-snapshot-schedule-store.md` (extends ADR-0048; no new AppHost resource; documents `schedule/` prefix and data-protection key blob per research.md) before any refresh-token write
- [X] T005 Add central package version and project references for `Azure.Extensions.AspNetCore.DataProtection.Blobs` in `Directory.Packages.props` and `src/SoloDevBoard.Web/SoloDevBoard.Web.csproj` after T002 approval
- [X] T006 [P] Add Application records and enums for sign-in record and occasion queue aligned with `data-model.md` (`Sign-in record` and `Occasion queue` entities) in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T007 [P] Add `OperatorSnapshotRunStartedBy` enum and optional `StartedBy` on `OperatorSnapshotDocument` per `data-model.md` `Snapshot run.startedBy` in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotDocument.cs`
- [X] T008 [P] Persist and read `startedBy` on `run` in `src/SoloDevBoard.Infrastructure/Snapshots/OperatorSnapshotJson.cs` (absent on read maps to operator)
- [X] T009 Define `IOperatorSignInRecordStore` with upsert, list, get-by-login, and conditional hold update in `src/SoloDevBoard.Application/Services/Snapshots/IOperatorSignInRecordStore.cs`
- [X] T010 Define `IDailyOccasionQueueStore` with conditional create, read, lease update, entry mutate, and remove entry in `src/SoloDevBoard.Application/Services/Snapshots/IDailyOccasionQueueStore.cs`
- [X] T011 Implement blob sign-in store at `schedule/sign-ins/{sanitised}.json` with fields per `data-model.md` `Sign-in record.operatorLogin`, `completedAt`, `organisationLogins`, `protectedRefreshToken`, `refreshTokenExpiresAt`, and `scheduleHold` in `src/SoloDevBoard.Infrastructure/Snapshots/OperatorSignInRecordBlobStore.cs`
- [X] T012 Implement blob occasion store at `schedule/occasion.json` with `data-model.md` `Occasion queue.occasionDate`, `filledAt`, `entries`, `activeLogin`, and `leaseUntil` in `src/SoloDevBoard.Infrastructure/Snapshots/DailyOccasionQueueBlobStore.cs`
- [X] T013 Register sign-in and queue stores in hosted blob posture only via `src/SoloDevBoard.Infrastructure/Snapshots/OperatorSnapshotServiceCollectionExtensions.cs` and `src/SoloDevBoard.Composition/OperatorSnapshotServiceCollectionExtensions.cs`
- [X] T014 Register ASP.NET Core Data Protection key ring to `schedule/data-protection-keys.xml` when hosted blob connection is present in `src/SoloDevBoard.Web/Program.cs` (lean startup must not require the connection per contracts/hosted-sign-in-record.md)
- [X] T015 Add `IHostedSignInRecordService.RecordCompletedSignInAsync` (or equivalent) on Application that upserts a sign-in record, clears `data-model.md` `Sign-in record.scheduleHold`, and never returns tokens on DTOs in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T016 Implement `OperatorSnapshotEligibility` at occasion instant (30-day window, `data-model.md` `Sign-in record.completedAt` not after occasion instant, hold, in-progress snapshot exclusion, admission rules) per `data-model.md` Eligibility section and contracts/hosted-sign-in-record.md in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotEligibility.cs`
- [X] T017 Wire `TimeProvider` through new Application schedule APIs (no `DateTime.UtcNow` in new code) in schedule-related services under `src/SoloDevBoard.Application/Services/Snapshots/`

**Checkpoint**: ADR accepted, stores registered, `startedBy` round-trips, eligibility pure logic callable from tests

---

## Phase 3: User Story 1 - Collect overnight without the operator waiting (Priority: P1) MVP

**Goal**: Hosted sign-in queues eligible operators once per UTC day at 05:00 (with grace) and runs one daily collection at a time using stored refresh tokens.

**Independent Test**: Two eligible sign-ins, both signed out at 05:00 UTC: each queued once; collections run one at a time; each success matches a hand-started snapshot (quickstart.md row 1).

### Implementation for User Story 1

- [X] T018 [US1] Define `IDailyCollectionScheduler` (fill + drain orchestration) and constants (05:00:00, 05:15:00 grace, 15s tick, 3 further tries cap) in `src/SoloDevBoard.Application/Services/Snapshots/IDailyCollectionScheduler.cs`
- [X] T019 [US1] Implement fill: conditional create of occasion document once per UTC date, list sign-in records, apply eligibility (including in-progress snapshot and post-occasion-instant `completedAt` exclusions), no GitHub membership call per contracts/daily-collection-schedule.md in `src/SoloDevBoard.Application/Services/Snapshots/DailyCollectionScheduler.cs`
- [X] T020 [US1] Implement drain step: take lease on head entry, unprotect refresh token, exchange via existing hosted token path, rotate ciphertext only on `data-model.md` `Sign-in record.protectedRefreshToken` without changing `completedAt` or `organisationLogins` in `DailyCollectionScheduler.cs`
- [X] T021 [US1] Start existing snapshot walk with `startedBy = dailyOccasion` and captured access token; do not cancel on open page token per contracts/daily-collection-schedule.md in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotCollectionCoordinator.cs`
- [X] T022 [US1] On daily success remove queue entry and persist snapshot as today’s hand start would in `DailyCollectionScheduler.cs`
- [X] T023 [US1] Add `DailyCollectionBackgroundService` (15-second loop, `TimeProvider`, host stopping token) in `src/SoloDevBoard.Web/Snapshots/DailyCollectionBackgroundService.cs`
- [X] T024 [US1] Register `DailyCollectionBackgroundService` only when hosted sign-in is enabled in `src/SoloDevBoard.Web/Program.cs` (not registered in PAT-only local trusted mode per contracts/daily-collection-schedule.md)

**Checkpoint**: Fake clock tests can queue two logins and drain sequentially without operator present

---

## Phase 4: User Story 2 - Keep starting a collection by hand (Priority: P1)

**Goal**: **Collect snapshot** remains; hand start refuses while queued; success still replaces the kept snapshot with `startedBy = operator`.

**Independent Test**: Hand `StartAsync` after daily success replaces snapshot; while queued returns `Queued` and does not start (quickstart.md rows 57–58).

### Implementation for User Story 2

- [X] T025 [US2] Add `Queued` to `OperatorSnapshotStartResult` in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotStartResult.cs`
- [X] T026 [US2] In `OperatorSnapshotCollectionService.StartAsync`, return `Queued` when today's occasion entry is `data-model.md` `Queue entry.state` `waiting` or `retryWaiting` for this login; lean posture never returns `Queued` per contracts/daily-collection-schedule.md in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotCollectionService.cs`
- [X] T027 [US2] Set `startedBy = operator` on hand-started runs in `OperatorSnapshotCollectionCoordinator.cs`; hand runs do not enqueue further tries
- [X] T028 [US2] Disable **Collect snapshot** when status is in progress or `QueuedBeforeStart` is true in `src/SoloDevBoard.Web/Components/Features/Repositories/Pages/Repositories.razor` and code-behind (no new retry button per contracts/repositories-schedule-copy.md)

**Checkpoint**: Hand and daily runs share one snapshot; operator can still collect when not queued or in progress

---

## Phase 5: User Story 3 - Leave the operator's own machine unscheduled (Priority: P1)

**Goal**: Lean posture has no scheduler, no schedule blobs, and explains on-machine-only snapshots.

**Independent Test**: Lean posture across 05:00 UTC: scheduler absent; page explains no schedule (quickstart.md row 59).

### Implementation for User Story 3

- [X] T029 [US3] Assert in composition tests that PAT-only registration does not register `DailyCollectionBackgroundService` or blob sign-in stores in `tests/SoloDevBoard.Application.Tests/` or `tests/SoloDevBoard.Infrastructure.Tests/` as appropriate
- [X] T030 [US3] Extend `OperatorSnapshotStatusDto` with schedule fields where lean returns `Schedule = NoneLean` in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotStatusDto.cs`
- [X] T031 [US3] Add lean alert copy per contracts/repositories-schedule-copy.md in `src/SoloDevBoard.Web/Components/Features/Repositories/OperatorSnapshotStatusCopy.cs`
- [X] T032 [US3] Ensure `OperatorSnapshotDurabilityKind` copy on Repositories page still distinguishes file vs blob without implying a schedule in `Repositories.razor`

**Checkpoint**: Lean deploy has no 05:00 behaviour; operator can still collect on demand

---

## Phase 6: User Story 4 - Run one collection at a time (Priority: P1)

**Goal**: One active daily pass across replicas; no duplicate queue entries; live count updates on Repositories page.

**Independent Test**: Two queued operators, only one in progress; start control blocked while daily run in progress (quickstart.md rows 48–49, 57).

### Implementation for User Story 4

- [X] T033 [US4] Enforce at most one entry per login and skip eligibility when login is on the occasion queue, holds `activeLogin`, or has snapshot run `inProgress` per `data-model.md` Eligibility and `Queue entry.login` rules in `DailyCollectionScheduler.cs`
- [X] T034 [US4] Implement conditional lease write on `data-model.md` `Occasion queue.activeLogin` and `Occasion queue.leaseUntil`; losing replica skips tick per research.md in `DailyOccasionQueueBlobStore.cs` and drain logic
- [X] T035 [US4] Renew lease until daily pass completes in `DailyCollectionBackgroundService.cs`
- [X] T036 [US4] Do not add a second queue entry when a pass is already in progress or operator still on previous occasion queue at fill time per spec US4 scenario 1; when `occasionDate` advances at 05:00 UTC, drain remaining `data-model.md` `Queue entry.state` values on the prior document before treating the operator as free for the new date in `DailyCollectionScheduler.cs`
- [X] T037 [US4] Expose `StartedBy` on status DTO and render in-progress daily copy (start time, count, daily occasion) per contracts/repositories-schedule-copy.md in `OperatorSnapshotStatusCopy.cs` and `Repositories.razor.cs`

**Checkpoint**: Multi-replica tests show one runner; UI shows who started the run and rising count while in progress

---

## Phase 7: User Story 5 - See that the day has a fixed collection time (Priority: P2)

**Goal**: Repositories alert states next 05:00 UTC occasion or why no scheduled collection will run; no settings to change time or window.

**Independent Test**: Hosted page before any run names today at 05:00 UTC when eligible; hold state shows no false “tonight” promise (quickstart.md rows 60–61).

### Implementation for User Story 5

- [X] T038 [US5] Add `OperatorSnapshotScheduleKind`, `StartedBy`, `QueuedBeforeStart`, `Schedule`, and `NextOccasionAt` to `OperatorSnapshotStatusDto` per contracts/daily-collection-schedule.md in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T039 [US5] Implement `GetStatusAsync` schedule statement rules (next occasion, queued-before-start, hold, window, admission, missed day naming) in `OperatorSnapshotCollectionService.cs`
- [X] T040 [US5] Map all hosted alert rows from contracts/repositories-schedule-copy.md in `OperatorSnapshotStatusCopy.cs` (literal `05:00 UTC`; UK English; no em dash)
- [X] T041 [US5] While `QueuedBeforeStart`, keep naming today's 05:00 UTC and do not show run as in progress in `OperatorSnapshotStatusCopy.cs`
- [X] T042 [US5] Confirm no new settings page or controls for time, cadence, or 30-day window on `Repositories.razor`

**Checkpoint**: bUnit can assert copy matrix for eligible, hold, lean, and not-allowed cases

---

## Phase 8: User Story 6 - Keep the last success when the overnight run does not finish cleanly (Priority: P2)

**Goal**: Same failure reasons as feature 001; rate-limit/unexpected retries to queue back (max 3 further tries); sign-in/token hold; interruption on shutdown; retry wait stays in progress.

**Independent Test**: Rate limit with another account queued runs other first; fourth failure terminal; token failure sets hold; stop during pass interrupts; stop during retry wait does not interrupt (quickstart.md rows 50–56).

### Implementation for User Story 6

- [X] T043 [US6] On `rateLimit` or `unexpected`, increment `data-model.md` `Queue entry.attemptsCompleted`, set `data-model.md` `Queue entry.state` to `retryWaiting` and move entry to back, keep snapshot `inProgress` until success, final fail, or interrupted pass in `DailyCollectionScheduler.cs`
- [X] T044 [US6] On `signInOrToken`, remove entry, set `data-model.md` `Sign-in record.scheduleHold` true, store failed run, no requeue in `DailyCollectionScheduler.cs`
- [X] T045 [US6] On host stopping while lease holder and pass running, save `interrupted` / `siteStopped`, remove entry; leave `waiting` and `retryWaiting` entries in place per contracts/daily-collection-schedule.md in `DailyCollectionBackgroundService.cs` and existing `OperatorSnapshotApplicationStopping.cs`
- [X] T046 [US6] On expired `leaseUntil`, treat active pass as interrupted, remove entry, do not requeue in `DailyCollectionScheduler.cs`
- [X] T047 [US6] Carve out: `GetStatusAsync` must not map stored `inProgress` to interrupted while occasion entry is `retryWaiting` for this login per research.md in `OperatorSnapshotCollectionService.cs`
- [X] T048 [US6] Ensure failed and interrupted runs do not replace `lastSuccessfulCollectionAt` or snapshot repository entries per existing feature 001 rules in coordinator and store layers
- [X] T049 [US6] Render failure, hold, and interrupted copy rows in `OperatorSnapshotStatusCopy.cs` without listing tries or implying a collection tonight when on hold

**Checkpoint**: Retry-backoff visible as one in-progress daily run; prior success finish time still shown beside failure

---

## Phase 9: User Story 7 - Skip a missed day, and follow recent sign-ins (Priority: P2)

**Goal**: 15-minute fill grace; no fill after 05:15; sign-in record drives eligibility not allow-list scan; OAuth completion upserts record.

**Independent Test**: 05:16 no fill; 05:10 fill; incomplete sign-in leaves record unchanged; admission empty lists queue nobody (quickstart.md rows 38–46, 54).

### Implementation for User Story 7

- [X] T050 [US7] Implement missed-day rule: after 05:15:00 UTC do not create occasion document for that date; work already queued still drains in `DailyCollectionScheduler.cs`
- [X] T051 [US7] Second fill attempt same date is no-op after conditional-create conflict per quickstart in `DailyCollectionScheduler.cs`
- [X] T052 [US7] After successful `ExchangeCodeForSessionAsync` in the `/auth/callback` minimal route in `src/SoloDevBoard.Web/Program.cs`, call `RecordCompletedSignInAsync` with organisation logins from that session; do not write on incomplete sign-in, error redirect, or token exchange failure per contracts/hosted-sign-in-record.md
- [X] T053 [US7] Do not treat silent token refresh or daily collection token rotation as new `completedAt` or organisation set replacement in sign-in store implementation
- [X] T054 [US7] Exclude sign-in older than 30 days at occasion instant using `data-model.md` `Sign-in record.completedAt` boundary (exactly 30 days included) in `OperatorSnapshotEligibility.cs`
- [X] T055 [US7] Apply `HostedAdmissionControlOptions` at fill instant per contracts/hosted-sign-in-record.md in `OperatorSnapshotEligibility.cs` (empty lists when admission on allows nobody)

**Checkpoint**: Eligibility and grace rules covered by Application unit tests without GitHub calls

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Tests, docs, OAuth security, and merge gates

- [X] T056 [P] Application unit tests for eligibility, fill grace, missed day, single fill, drain order, retries, hold, and `Queued` start in `tests/SoloDevBoard.Application.Tests/` following quickstart.md scenario table; include FR-014 / SC-003 cross-operator isolation, FR-010 (scheduler tick and status load do not start a collection), FR-008 (no queue entry when hand run already `inProgress` at fill), and FR-016 (sign-in after occasion instant not queued until next day), and FR-018 (status exposes no past-occasion log or queue history)
- [X] T057 [P] Infrastructure tests for conditional occasion create and lease races in `tests/SoloDevBoard.Infrastructure.Tests/OperatorSignInRecordBlobStoreTests.cs` or new companion files
- [X] T058 [P] bUnit tests for schedule copy and disabled **Collect snapshot** in `tests/SoloDevBoard.Web.Tests/RepositoriesTests.cs` and `OperatorSnapshotStatusCopyTests.cs`; include FR-010 (opening or reloading Repositories does not start collection), FR-011 (catalogue reload remains available while a daily run is in progress), and SC-006 (in-progress daily status polling still updates the displayed recorded count via unchanged feature 001 polling)
- [X] T059 Update `website/content/docs/repositories.md` for hosted schedule and lean on-machine wording per contracts/repositories-schedule-copy.md
- [X] T060 Update `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` if the repositories guide row changes; recapture `repositories/overview.png` only if command strip or alert text in that shot changes
- [X] T061 Verify logs and status DTOs never emit refresh token, access token, or ciphertext per contracts/hosted-sign-in-record.md
- [X] T062 Run `dotnet format SoloDevBoard.slnx --verify-no-changes`, `dotnet build SoloDevBoard.slnx -c Release -warnaserror`, and `dotnet test SoloDevBoard.UnitTests.slnf -c Release` per `AGENTS.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T002 blocks T005 and token-writing tasks
- **Foundational (Phase 2)**: Depends on T002/T004; blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2; MVP core
- **User Stories 2–7 (Phases 4–9)**: Depend on Phase 2; US1 drain should exist before US4/US6 lease and retry tasks fully integrate
- **Polish (Phase 10)**: Depends on desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: After Foundational; no dependency on other stories
- **US2 (P1)**: After Foundational; integrates with queue state from US1/US4
- **US3 (P1)**: After Foundational; parallel with US1 registration checks
- **US4 (P1)**: After US1 drain skeleton; tight coupling to queue store
- **US5 (P2)**: After status DTO groundwork; T030 adds lean-only schedule fields, T038 completes the hosted contract on the same DTO (sequence T030 then T038 on one assignee)
- **US6 (P2)**: After US1 drain; extends scheduler and `GetStatusAsync`
- **US7 (P2)**: After sign-in store (T011, T052); eligibility used by US1 fill

### Within Each User Story

- Application services before Web registration
- Store implementations before scheduler drain
- Status DTO fields before Repositories copy
- Implementation before Phase 10 tests where tests lock the same behaviour

### Parallel Opportunities

- T006, T007, T008 (models and JSON) in parallel after T004
- T011 and T012 (two blob stores) in parallel after T009–T010
- US3 copy (T031) and US5 copy (T040) touch same file; sequence T031 then T040 or one assignee
- T056, T057, T058 in parallel in Phase 10
- After Phase 2, US3 lean copy can proceed in parallel with US1 scheduler if staffed separately (merge conflict risk on `OperatorSnapshotStatusDto`)

---

## Parallel Example: User Story 1

```bash
# After Phase 2, one developer can implement store drain (T020–T022) while another wires BackgroundService shell (T023–T024),
# then integrate in DailyCollectionScheduler.cs.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2 (including ADR and package approval)
2. Complete Phase 3 (US1)
3. Add minimal US2 refusal (T025–T026) so hand start does not fight the queue
4. **STOP and VALIDATE**: Application tests for two-operator queue from quickstart.md
5. Deploy/demo hosted path when ready

### Incremental Delivery

1. Foundational → US1 → US2 → US4 (concurrency) → US6 (failures) → US7 (grace/admission) → US5 (full copy) → US3 (lean copy if not done earlier) → Polish

### Parallel Team Strategy

1. Team completes Phase 2 together
2. Developer A: US1 + US4 scheduler
3. Developer B: US7 sign-in callback + eligibility tests
4. Developer C: US5/US3 Repositories copy and bUnit
5. Integrate US6 retry and interruption rules before merge

---

## Notes

- Do not wire `SoloDevBoard.Functions` or add AppHost resources (plan.md, research.md)
- Do not add Playwright collection journeys (quickstart.md, constitution)
- `[P]` tasks = different files, no dependencies on incomplete tasks in the same file
- Cite `data-model.md` field names in code and tests when enforcing constraints
- UK English in operator-facing strings