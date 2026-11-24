---

description: "Task list for scheduled snapshot collection implementation"
---

# Tasks: Scheduled snapshot collection

**Input**: Design documents from `/specs/002-scheduled-snapshot-collection/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Unit, store, and bUnit tests are required per plan.md and quickstart.md (not TDD-first; implement alongside behaviour).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Scheduling model (2026-11-24 spec and plan)**: This delivery **asks** for a queue fill once per UTC day at 05:00 UTC, plus catch-up when the worker is back after 05:00 with no occasion marker, and `unmetFill` 15 minutes after a terminal technical outcome. A fill may run at any UTC time; it is not limited to a 15-minute window and the occasion marker is not a once-per-day lock. **Queue processing** runs whenever the worker is up, with no time-of-day gate. **Fill rules** (30-day window at fill instant, admission, not queued or in progress, no `credentialUnusable`) differ from **run rules** at dequeue (attempt unless not allowed; do not re-check the 30-day window; `signInOrToken` when stored access cannot be used).

**Artifact vs code**: Phases 1–11 are marked complete on the feature branch (pre-revision behaviour). Phase 12 is the **open** backlog to align code and tests with the 2026-11-24 spec, plan, data-model, and quickstart. It supersedes the earlier Phase 12 pass that enforced a 05:00–05:15 fill window, marker skip, sign-in-after-05:00 fill exclusion, and 30-day checks at dequeue.

**Pre-implement analyze gate**: All FR and SC items map to at least one task. Unchecked Phase 12 rows are expected implementation work, not missing specification coverage.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Repository-root `src/` and `tests/` per plan.md

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Binding decision, packages, and Functions host shell before feature code

- [X] T001 Write and accept binding ADR for scheduled collection Functions worker in `adr/adr-0049-scheduled-collection-functions.md` (content per `specs/002-scheduled-snapshot-collection/research.md`; do not edit AppHost until Accepted)
- [X] T002 Add Aspire 17.0.0 packages to central versions in `Directory.Packages.props` per research (`Aspire.Hosting.Azure.Functions`, `Aspire.Azure.Storage.Queues`, Functions worker extensions resolved at implementation)
- [X] T003 [P] Wire `src/SoloDevBoard.Functions/SoloDevBoard.Functions.csproj` with isolated worker SDK, Composition and ServiceDefaults references, and queue/timer extensions (no HTTP trigger)
- [X] T004 [P] Add `src/SoloDevBoard.Functions` to `SoloDevBoard.slnx` and ensure `dotnet build SoloDevBoard.slnx -c Release -warnaserror` includes the new project
- [X] T005 [P] Configure `src/SoloDevBoard.Functions/host.json` with `functionTimeout` `02:00:00` per research.md
- [X] T006 Replace placeholder timer in `src/SoloDevBoard.Functions/InventoryPlaceholderTimerFunction.cs` with thin trigger stubs only (delegate to Application; no GitHub calls in triggers)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Snapshot schema, ports, sign-in durability, and status facts that every user story depends on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 Extend snapshot JSON model with `trigger`, `queuedOccasion`, `passExecuting`, `passLeaseUntil`, and `attemptsUsed` per data-model.md (`SnapshotDocument.trigger`, `SnapshotDocument.queuedOccasion`, `SnapshotDocument.passExecuting`, `SnapshotDocument.passLeaseUntil`, `SnapshotDocument.attemptsUsed`) in `src/SoloDevBoard.Application/` snapshot types and serializers
- [X] T008 Add `scheduledCollectionStopped` to run reason enum and parsing in `src/SoloDevBoard.Application/Services/Snapshots/` (data-model.md `run.reason`)
- [X] T009 Update `src/SoloDevBoard.Infrastructure/Snapshots/BlobOperatorSnapshotStore.MaybeRewriteInterruptedAsync` so daily passes with `passExecuting` are not rewritten to `siteStopped` when the web process has no live task (research.md interruption table)
- [X] T010 [P] Introduce hosted sign-in record model matching data-model.md (`HostedSignInRecord.schemaVersion`, `HostedSignInRecord.operatorLogin`, `HostedSignInRecord.completedAt`, `HostedSignInRecord.organisationLogins`, `HostedSignInRecord.refreshToken`, `HostedSignInRecord.refreshTokenExpiresAt`, `HostedSignInRecord.credentialUnusable`) in `src/SoloDevBoard.Application/Authentication/`
- [X] T011 [P] Add `IHostedSignInRecordStore` port and blob adapter for container `hosted-sign-ins` in `src/SoloDevBoard.Infrastructure/Identity/` with etag conditional writes per contracts/hosted-sign-in-record.md
- [X] T012 Extract shared GitHub refresh token exchange behind an Application port in `src/SoloDevBoard.Application/Authentication/` and move existing web gateway usage to `src/SoloDevBoard.Infrastructure/Identity/`
- [X] T013 Write hosted sign-in blob on completed sign-in from `src/SoloDevBoard.Web/Authentication/` (set `schemaVersion`, `operatorLogin`, clear `credentialUnusable`, set `completedAt`, `organisationLogins`, `refreshToken`, and `refreshTokenExpiresAt` per data-model.md and contracts/hosted-sign-in-record.md write table); sign-out does not delete the blob
- [X] T014 Register blob container client for `hosted-sign-ins` in `src/SoloDevBoard.Web/Program.cs` via Aspire reference (no queue or `collection-schedule` on web)
- [X] T015 Extend `OperatorSnapshotStatusDto` with `Trigger`, `SchedulePosture`, `QueuedForOccasion`, `NextOccasionAtUtc`, and `NoScheduleReason` per data-model.md status facts table in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T016 Implement status fact computation (hosted vs lean, next occasion rules) in `src/SoloDevBoard.Application/Services/Snapshots/` using `TimeProvider` and admission options
- [X] T017 Register new ports and hosted-only adapters in `src/SoloDevBoard.Composition/` without connection-string lookup
- [X] T018 [P] Application tests for additive snapshot field defaults and reason parsing in `tests/SoloDevBoard.Application.Tests/`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Collect overnight without the operator waiting (Priority: P1) 🎯 MVP

**Goal**: Hosted sign-in fills the scheduled scan queue on each ask (default 05:00 UTC, catch-up, and `unmetFill`), then processes entries whenever the worker runs. One pass at a time. Run rules at dequeue attempt the scan unless the person is not allowed; stored-access failure records `signInOrToken`.

**Independent Test**: Per spec.md US1: two eligible signed-out operators are queued at fill and collected when the queue is processed; ineligible operators are not queued; snapshots match hand-started results; not-allowed removal has no failed collection; queued work still runs when sign-in is older than 30 days at dequeue.

### Tests for User Story 1

- [X] T019 [P] [US1] Unit tests for occasion window and single fill (`TryFillDailyOccasion`) in `tests/SoloDevBoard.Application.Tests/` (superseded by Phase 12 T097–T099)
- [X] T020 [P] [US1] Unit tests for fill eligibility: 30-day boundary (`HostedSignInRecord.completedAt`), `credentialUnusable`, admission lists in `tests/SoloDevBoard.Application.Tests/`
- [X] T069 [P] [US1] Store or Application test: daily success produces the same catalogue and snapshot facts as hand-started collection for the same operator fixture (FR-003, SC-002) in `tests/SoloDevBoard.Application.Tests/` or `tests/SoloDevBoard.Infrastructure.Tests/`

### Implementation for User Story 1

- [X] T021 [US1] Implement `IDailyOccasionService.TryFillDailyOccasion` with 05:00–05:15 UTC window, occasion marker create `IfNoneMatch *`, and enqueue each eligible login once in `src/SoloDevBoard.Application/Services/Snapshots/` (revised in Phase 12)
- [X] T022 [US1] Implement occasion marker blob adapter (`collection-schedule/occasions/{yyyy-MM-dd}.json`) per data-model.md `OccasionMarker.occasionDate`, `OccasionMarker.filledAt`, `OccasionMarker.operators` in `src/SoloDevBoard.Infrastructure/Snapshots/`
- [X] T023 [US1] Implement queue publisher for `daily-snapshot-collection` messages per data-model.md `QueueMessage.occasionDate`, `QueueMessage.operatorLogin`, `QueueMessage.kind` in `src/SoloDevBoard.Infrastructure/Snapshots/` (no tokens in body)
- [X] T024 [US1] On fill, set snapshot `queuedOccasion` to today's UTC date per data-model.md `SnapshotDocument.queuedOccasion` without starting the pass
- [X] T025 [US1] Implement daily collection pass orchestration reusing existing snapshot walk in `src/SoloDevBoard.Application/Services/Snapshots/` (`trigger` = `daily`, `attemptsUsed` starts at 1 per data-model.md)
- [X] T026 [US1] Implement `runner` blob lease in `collection-schedule` so only one daily pass runs at a time across worker copies in `src/SoloDevBoard.Infrastructure/Snapshots/`
- [X] T027 [US1] Queue consumer: start pass (`passExecuting`, `passLeaseUntil` renewal ~10s), complete message after pass, per contracts/daily-collection-schedule.md in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T028 [US1] Timer trigger NCRONTAB `0 0-15 5 * * *` calling fill only in `src/SoloDevBoard.Functions/` (revised to `0 0 5 * * *` in Phase 12)
- [X] T029 [US1] Queue trigger with `batchSize` 1 wired to daily pass handler in `src/SoloDevBoard.Functions/`
- [X] T030 [US1] Register `AddAzureQueueServiceClient("operator-snapshot-queues")` and snapshot/sign-in clients in `src/SoloDevBoard.Functions/Program.cs` then `AddSoloDevBoard`
- [X] T031 [US1] Set `credentialUnusable` on sign-in record after daily `signInOrToken` failure per data-model.md `HostedSignInRecord.credentialUnusable` in Application/Infrastructure
- [X] T070 [US1] On daily pass token exchange, persist rotated `HostedSignInRecord.refreshToken` and `HostedSignInRecord.refreshTokenExpiresAt` under blob etag per contracts/hosted-sign-in-record.md (Functions worker path; shared port from T012)

**Checkpoint**: User Story 1 deliverable - unattended daily collection succeeds for eligible hosted operators (spec-complete after Phase 12)

---

## Phase 4: User Story 2 - Keep starting a collection by hand (Priority: P1)

**Goal**: **Collect snapshot** remains on `/repositories`; hand start replaces snapshots like daily success; blocked while queued or in progress.

**Independent Test**: Per spec.md US2: after daily success, hand start replaces snapshot; hand start before daily run still works when not queued.

### Tests for User Story 2

- [X] T032 [P] [US2] Application tests that `StartAsync` refuses when `queuedOccasion` is set or daily `inProgress` in `tests/SoloDevBoard.Application.Tests/`
- [X] T071 [P] [US2] Application or bUnit tests that loading `/repositories` and live catalogue reload do not enqueue or start a collection (FR-010) in `tests/SoloDevBoard.Application.Tests/` or `tests/SoloDevBoard.Web.Tests/`

### Implementation for User Story 2

- [X] T033 [US2] Enforce hand-start refusal in `src/SoloDevBoard.Application/Services/Snapshots/` when data-model.md queue flags apply (`queuedOccasion` or `run.status` `inProgress` including retry wait)
- [X] T034 [US2] Disable **Collect snapshot** in `src/SoloDevBoard.Web/Components/Features/Repositories/` when status facts show queued or in-progress (contracts/repositories-schedule-copy.md actions table)
- [X] T035 [US2] Ensure hand-started runs keep `trigger` `operator` and do not enqueue or use `runner` lease in Application snapshot start path

**Checkpoint**: User Stories 1 and 2 both work independently

---

## Phase 5: User Story 3 - Leave the operator's own machine unscheduled (Priority: P1)

**Goal**: Lean posture has no worker, no 05:00 fill, and truthful schedule copy on `/repositories`.

**Independent Test**: Per spec.md US3: no run at 05:00 UTC; explanation shown; hand collect still works.

### Tests for User Story 3

- [X] T036 [P] [US3] Application tests with lean posture: no fill and no queue at 05:00 UTC in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 3

- [X] T037 [US3] Gate fill, queue, and sign-in blob writes behind hosted posture detection in Composition/Application (lean uses file snapshot only per data-model.md lean paragraph)
- [X] T038 [US3] Return `SchedulePosture = lean` and `NoScheduleReason = lean` from status service in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T039 [US3] Render lean schedule sentences in status `MudAlert` per contracts/repositories-schedule-copy.md in `src/SoloDevBoard.Web/Components/Features/Repositories/`

**Checkpoint**: Lean and hosted posture behaviours diverge correctly

---

## Phase 6: User Story 4 - Run one collection at a time (Priority: P1)

**Goal**: One in-progress collection per operator; one daily pass globally; live page updates; daily vs operator trigger copy.

**Independent Test**: Per spec.md US4: no second queue entry; start control blocked; two operators serialized; page shows daily trigger and rising count.

### Tests for User Story 4

- [X] T040 [P] [US4] Application tests for single flight: two eligible operators, one pass at a time, second starts after first in `tests/SoloDevBoard.Application.Tests/`
- [X] T041 [P] [US4] Application tests: fill does not add second entry when `queuedOccasion` or daily `inProgress` already set in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 4

- [X] T042 [US4] When `runner` lease is held by another copy, re-send same queue payload to tail and complete current message per research.md in queue handler
- [X] T043 [US4] Prevent duplicate enqueue for same occasion and operator in fill and consumer paths
- [X] T044 [US4] Expose current run trigger (`operator` vs `daily`) in status DTO and in-progress poll in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [X] T045 [US4] Keep ~1s in-progress poll showing recorded count and trigger without full reload per contracts/repositories-schedule-copy.md; bUnit or Web test that navigation away from `/repositories` during an in-progress daily run still works (FR-011)

**Checkpoint**: Concurrency and UI concurrency rules satisfied

---

## Phase 7: User Story 5 - See when the next scheduled queue fill is (Priority: P2)

**Goal**: Repositories page states the next 05:00 UTC **ask** that will queue them, or one schedule refusal sentence (`notAllowed`, then `credentialUnusable`, then `outsideWindow`). After today's ask has passed, name the following day at 05:00 UTC when they would be eligible, including when today's scan has not succeeded, unless still queued for today.

**Independent Test**: Per spec.md US5 and quickstart.md section 5.

### Tests for User Story 5

- [X] T046 [P] [US5] bUnit tests for schedule sentences and absence of settings/retry controls in `tests/SoloDevBoard.Web.Tests/` per quickstart.md section 5 (extend in Phase 12 T110)

### Implementation for User Story 5

- [X] T047 [US5] Implement `NextOccasionAtUtc` and `NoScheduleReason` rules in `src/SoloDevBoard.Application/Services/Snapshots/` per data-model.md status facts (revised in Phase 12)
- [X] T048 [US5] Add hosted schedule and run clause copy to status alert per contracts/repositories-schedule-copy.md in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [X] T049 [US5] Update `website/content/docs/repositories.md` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` in the same change (refresh in Phase 12 with T119 and T110)

**Checkpoint**: Operator-readable schedule truth on `/repositories`

---

## Phase 8: User Story 6 - Keep the last success when the overnight run does not finish cleanly (Priority: P2)

**Goal**: Retries (up to 3 further tries with minimum wall-clock waits), interruptions, credential wait, no retry button; `unmetFill` after terminal technical outcomes; prior success unchanged on failure.

**Independent Test**: Per spec.md US6 and quickstart.md section 4.

### Tests for User Story 6

- [X] T050 [P] [US6] Store tests for `scheduledCollectionStopped` interruption branch in `tests/SoloDevBoard.Infrastructure.Tests/`
- [X] T072 [P] [US6] Infrastructure tests: etag conditional writes on hosted sign-in and occasion marker blobs; fixtures assert no refresh token in snapshot JSON or queue message bodies in `tests/SoloDevBoard.Infrastructure.Tests/`
- [X] T051 [P] [US6] Application tests: rate-limit retry to tail (max 4 attempts), sign-in failure no retry, queued-not-interrupted on worker stop in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 6

- [X] T052 [US6] On rate limit or unexpected with `attemptsUsed` < 4, complete message, enqueue `kind` `retry`, clear `passExecuting`, keep `inProgress` per data-model.md transitions
- [X] T053 [US6] On redelivery with `passExecuting` true, write `interrupted` with `scheduledCollectionStopped` and complete message (no requeue) per contracts/daily-collection-schedule.md
- [X] T054 [US6] Web read path: when `passLeaseUntil` is past and `passExecuting`, write interruption same as worker redelivery in `src/SoloDevBoard.Infrastructure/Snapshots/BlobOperatorSnapshotStore.cs`
- [X] T055 [US6] Show failed/interrupted runs with last success finish time and daily trigger copy in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [X] T056 [US6] Confirm no separate retry control is added anywhere under `src/SoloDevBoard.Web/`
- [X] T084 Add `retainedRun` object to snapshot document types and JSON serialization in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotDocument.cs` and Infrastructure serializers per data-model.md `SnapshotDocument.retainedRun`
- [X] T087 [US6] On first daily pass start, set `SnapshotDocument.retainedRun` to prior terminal `run` and `trigger` when replacing a terminal run; clear `retainedRun` on success, terminal failure, or interruption in `DailySnapshotCollectionPassService` and coordinator paths
- [X] T088 [US6] When a further try is withdrawn by run rules, restore from `SnapshotDocument.retainedRun`, clear attempt fields and daily in-progress presentation in `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`
- [X] T089 Add `NotBefore` (ISO-8601 UTC) to `DailySnapshotQueueMessage` in `src/SoloDevBoard.Application/Services/Snapshots/` per data-model.md `QueueMessage.notBefore`
- [X] T090 [US6] In `src/SoloDevBoard.Infrastructure/Snapshots/AzureDailySnapshotQueuePublisher.cs`, set initial visibility timeout to `notBefore - now` when positive (cap 7 days); serialize `notBefore` on `retry` messages
- [X] T091 [US6] When enqueueing a `retry`, set `notBefore` from the failed attempt per research.md minimum-wait table in `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`
- [X] T092 [US6] If a message is received before `notBefore`, re-queue with remaining invisibility and complete the current delivery without starting a pass in `DailySnapshotCollectionPassService.cs`
- [X] T093 [US4] When re-sending to tail after losing `runner` lease, preserve `notBefore` visibility for `retry` messages in queue handler and publisher paths

**Checkpoint**: Failure and recovery behaviour matches operator snapshot collection semantics (`unmetFill` added in Phase 12)

---

## Phase 9: User Story 7 - Run the fill whenever it is asked, and follow recent sign-ins (Priority: P2)

**Goal**: One timer ask at 05:00 UTC; catch-up fill when the worker returns after 05:00 with no marker; no 05:15 miss; sign-in after 05:00 included on the next fill; re-queue after success when idle; admission and 30-day window at **fill instant** only.

**Independent Test**: Per spec.md US7 and quickstart.md section 3 (revised table: catch-up after 05:15, second fill after success, sign-in at 06:00 queued by 06:30 fill).

### Tests for User Story 7

- [X] T057 [P] [US7] Application tests: no fill after 05:15; fill at 05:10 when marker absent in `tests/SoloDevBoard.Application.Tests/` (superseded by Phase 12 T097–T099)
- [X] T058 [P] [US7] Application tests: organisation allowance from saved logins only in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 7

- [X] T059 [US7] Enforce no marker and no fill after 05:15:00 UTC same day in `TryFillDailyOccasion` (removed in Phase 12)
- [X] T060 [US7] Functions worker start repair: enqueue missing `initial` for logins still `queuedOccasion` in `src/SoloDevBoard.Application/Services/Snapshots/DailyOccasionService.cs` (extend startup fill in Phase 12)
- [X] T061 [US7] Apply `HostedAdmissionControl` lists at fill without GitHub membership call in `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs`
- [X] T062 [US7] Exclude operators outside 30-day window at fill (`HostedSignInRecord.completedAt` exactly 30 days inside per data-model.md) at fill instant in `HostedCollectionEligibilityEvaluator.cs`

**Checkpoint**: Fill-path rules functional; Phase 12 completes ask paths and fill instant behaviour

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: AppHost wiring, deployment docs, and PR validation

- [X] T063 Wire hosted sign-in graph only in `src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs` per research.md (after ADR-0049 Accepted)
- [X] T064 Leave `src/SoloDevBoard.AppHost/LeanAppHostGraph.cs` unchanged (no Functions, no extra storage)
- [X] T065 Re-assert `web` `MinReplicas` 0 and `MaxReplicas` 1 in `src/SoloDevBoard.AppHost/SoloDevBoardAppHost.cs` after any publish callback changes
- [X] T066 [P] Assign Functions identity storage roles on publish per research.md on hosted graph only
- [X] T067 [P] Update `docs/getting-started.md`, `docs/deployment.md`, and `src/SoloDevBoard.AppHost/README.md` for new hosted resources
- [X] T073 [P] Application tests: two operators cannot read each other's snapshot status or blob paths per FR-014 in `tests/SoloDevBoard.Application.Tests/`
- [X] T068 Run quickstart.md validation commands per specs/002-scheduled-snapshot-collection/quickstart.md section 6 (re-run as T112 after Phase 12)

---

## Phase 11: Prior convergence (delivered; partly superseded)

- [X] T074 [US4] Extend `HostedCollectionEligibilityEvaluator.IsEligibleAtFill` to refuse fill for **this operator** when **their** collection is `inProgress` or when **their** `queuedOccasion` is set; other operators may still be queued on the same fill (FR-008, per-operator gate)
- [X] T075 Wire `DailySnapshotQueueInvocation.UpdateVisibilityAsync` in `src/SoloDevBoard.Functions/DailyCollectionFunctions.cs` to renew Azure queue visibility while `passExecuting`
- [X] T076 Exclude operators whose `HostedSignInRecord.completedAt` is after today's occasion start from that UTC day's fill and from naming today as `NextOccasionAtUtc` (reversed in Phase 12 via T101, T104, T117–T119)
- [X] T077 Keep daily retry-wait `inProgress` with `passExecuting` false from being rewritten to `siteStopped` on web read (FR-020)
- [X] T078 Present queue-waiting status when `queuedOccasion` is set and `passExecuting` is false (FR-006, US5)

---

## Phase 12: Spec and plan revision 2026-11-24 (open delta)

**Purpose**: Align implementation with [plan.md](plan.md) implementation notes, updated [spec.md](spec.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md), and contracts. **Blocks** declaring the feature converged against the current spec.

**⚠️ CRITICAL**: Complete Phase 12 before merge-readiness review against the updated spec.

### Tests for Phase 12

- [ ] T097 [P] [US7] Replace fill-window tests in `tests/SoloDevBoard.Application.Tests/DailyOccasionServiceTests.cs`: no fill before 05:00 on worker start; fill at 05:00 when marker absent; catch-up at 05:10 when marker absent; later fill when marker present queues after success; no refusal after 05:15 per quickstart.md section 3
- [ ] T098 [P] [US7] Application tests: sign-in at 06:00 UTC queued by fill at 06:30; fill uses `TimeProvider` instant not capped 05:00 for `HostedSignInRecord.completedAt` window in `tests/SoloDevBoard.Application.Tests/HostedCollectionEligibilityEvaluatorTests.cs`
- [ ] T099 [P] [US1] Application tests: queued message still runs when `completedAt` is older than 30 days at dequeue; not-allowed still withdraws without failed collection in `tests/SoloDevBoard.Application.Tests/` per quickstart.md section 3
- [ ] T100 [P] [US6] Application tests: after fourth failed attempt or interruption, one `unmetFill` with 15-minute hide then fill includes idle eligible operators in `tests/SoloDevBoard.Application.Tests/`; no `unmetFill` on `signInOrToken`
- [ ] T082 [P] [US6] Infrastructure tests: occasion marker replace under etag; `unmetFill` and `retry` visibility per data-model.md `QueueMessage.notBefore` in `tests/SoloDevBoard.Infrastructure.Tests/`
- [ ] T101 [P] [US5] Application tests: `NextOccasionAtUtc` and `NoScheduleReason` order (`notAllowed`, `credentialUnusable`, `outsideWindow`); following day at 05:00 after ask passed whether or not today's scan succeeded; queued keeps today after 05:00 in `tests/SoloDevBoard.Application.Tests/OperatorSnapshotScheduleFactsServiceTests.cs` (add file if missing)
- [ ] T110 [P] [US5] bUnit: schedule copy per contracts/repositories-schedule-copy.md after today's ask passed and when queued in `tests/SoloDevBoard.Web.Tests/`

### Fill and occasion marker (US1, US7)

- [ ] T103 [US1] Remove `IsInFillWindow` and `AlreadyFilled` when marker exists from `src/SoloDevBoard.Application/Services/Snapshots/DailyOccasionService.cs`; use fill instant `utcNow` for eligibility; always run fill logic when hosted schedule enabled and caller invokes the method
- [ ] T104 [US7] Remove `HostedSignInRecord.completedAt` after 05:00 UTC exclusion from `IsEligibleAtFill` in `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs` (data-model.md hosted sign-in window at fill instant)
- [ ] T105 [US1] Add `ICollectionOccasionMarkerStore.TryReplaceAsync` (etag) and implement in `src/SoloDevBoard.Infrastructure/Snapshots/BlobCollectionOccasionMarkerStore.cs` so later fills the same UTC date union `OccasionMarker.operators` and update `OccasionMarker.filledAt` per data-model.md
- [ ] T106 [US1] On fill, queue every eligible person not already queued or in progress, including after a same-day success, in `DailyOccasionService.cs`; lost fill lease retries fill without duplicating queue entries

### Timer, startup, and `unmetFill` (US7, US6)

- [ ] T107 [US7] Change timer NCRONTAB to `0 0 5 * * *` in `src/SoloDevBoard.Functions/DailyCollectionFunctions.cs`
- [ ] T108 [US7] On Functions host start in `src/SoloDevBoard.Functions/Program.cs` or a dedicated startup service: run `RepairMissingInitialMessagesAsync`, then call `TryFillDailyOccasionAsync` only when clock is at or after 05:00 UTC and today's occasion marker is absent; do not fill before 05:00; do not use timer `RunOnStartup` to fill overnight per research.md
- [ ] T109 [US6] Extend `DailySnapshotQueueMessage` and publisher for `kind` `unmetFill` with required `QueueMessage.notBefore` (15 minutes) per data-model.md in `src/SoloDevBoard.Application/Services/Snapshots/` and `src/SoloDevBoard.Infrastructure/Snapshots/AzureDailySnapshotQueuePublisher.cs`
- [ ] T111 [US6] Enqueue `unmetFill` after terminal `rateLimit`/`unexpected` with no tries left and after `scheduledCollectionStopped` interruption in `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`; never on `signInOrToken`
- [ ] T113 [US7] Handle `unmetFill` in `DailySnapshotCollectionPassService.HandleQueueMessageAsync` or thin branch in `DailyCollectionFunctions.cs`: complete message and call `IDailyOccasionService.TryFillDailyOccasionAsync` without walking GitHub per contracts/daily-collection-schedule.md

### Dequeue run rules (US1)

- [ ] T114 [US1] Replace dequeue gating in `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`: use admission-only check at dequeue (not 30-day window); attempt scan when allowed even if `HostedSignInRecord.completedAt` is outside 30 days; remove silent skip on `credentialUnusable` and missing record in favour of `signInOrToken` failed run per contracts/daily-collection-schedule.md
- [ ] T115 [US1] Refactor or narrow `HostedCollectionEligibilityEvaluator.IsEligibleAtRun` in `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs` to admission lists only; keep fill-time rules in `IsEligibleAtFill`
- [ ] T116 [US1] Stop using `IsEligibleAtRun` for schedule facts in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotScheduleFactsService.cs`; use `WouldBeEligibleForSchedule` at the instant the page names (today at 05:00 before ask, else following day at 05:00) per data-model.md `NoScheduleReason` and `NextOccasionAtUtc`

### Schedule facts and copy (US5)

- [ ] T117 [US5] Rewrite `TryComputeNextOccasion` and remove 15-minute grace / `completedAt <= 05:00` rules in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotScheduleFactsService.cs` per data-model.md and contracts/repositories-schedule-copy.md
- [ ] T118 [US5] Align `src/SoloDevBoard.Web/Components/Features/Repositories/OperatorSnapshotStatusCopy.cs` with updated facts (following UTC day sentence, queued-today row, refusal precedence); confirm no occasion list, run history, or try list is added (FR-018)
- [ ] T119 [US5] Update `website/content/docs/repositories.md` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` with T110 in the same change

### Validation

- [ ] T112 Re-run quickstart.md section 6 commands after Phase 12 in `specs/002-scheduled-snapshot-collection/quickstart.md`

**Checkpoint**: Code matches FR-001, FR-006, FR-012, FR-015, FR-016, FR-017, FR-020, FR-021, and worker outcome rows in contracts/daily-collection-schedule.md and quickstart.md sections 3–6

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Phase 1
- **User Stories (Phases 3–9)**: Delivered on branch; behaviour corrected in Phase 12
- **Polish (Phase 10)**: Delivered; re-validate after Phase 12 (T112)
- **Prior convergence (Phase 11)**: Superseded in part by Phase 12 (T076, fill/dequeue split)
- **Spec revision (Phase 12)**: Depends on Phases 1–11 baseline; **blocks** spec-complete validation

### User Story Dependencies (Phase 12)

- **US1**: T103–T106 fill; T114–T115 dequeue; tests T099
- **US5**: T116–T119 after T117 facts; tests T101, T110
- **US6**: T109–T111 `unmetFill` after T089 message shape; test T100
- **US7**: T103–T108 ask paths; tests T097–T098

### Parallel Opportunities (Phase 12)

- **Order**: Complete fill and dequeue implementation (T103–T106, T114–T115) before T097–T099 dequeue/fill tests; complete T117 schedule facts before T101 and T110
- T097–T098 may run in parallel once T103–T106 behaviour is stable; T099 needs T114–T115
- T082 may run in parallel with T109 once queue message shape is fixed in T089
- T107 timer and T105 marker store in parallel after T103 fill contract is clear
- T118 copy after T117 schedule facts

---

## Parallel Example: Phase 12

```bash
# Fill behaviour first:
src/SoloDevBoard.Application/Services/Snapshots/DailyOccasionService.cs           # T103, T106
src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs  # T104, T115

# Infrastructure marker replace:
src/SoloDevBoard.Infrastructure/Snapshots/BlobCollectionOccasionMarkerStore.cs    # T105

# Worker asks and later fill:
src/SoloDevBoard.Functions/DailyCollectionFunctions.cs                            # T107
src/SoloDevBoard.Functions/Program.cs                                             # T108
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phases 1–2 (already on branch)
2. Complete Phase 12 fill and dequeue tasks (T103–T106, T114–T115) with T097–T099
3. Complete timer and startup (T107–T108)
4. **STOP and VALIDATE**: quickstart section 3 Application tests

### Incremental Delivery

1. **Delivered**: Phases 1–11 infrastructure and hosted schedule (pre-revision semantics)
2. **Next**: Phase 12 entire backlog (T097–T119, T112)
3. Re-run T112 and full PR checklist

### Suggested MVP scope

- **Spec-complete MVP**: User Story 1 with Phase 12 fill, dequeue, timer, and catch-up (T103–T108, T114–T115, T097–T099)
- **Full feature**: All Phase 12 tasks including `unmetFill`, schedule copy, and docs

---

## Notes

- [P] tasks = different files, no dependencies on incomplete siblings
- [Story] label maps task to spec.md user scenarios for traceability
- data-model.md field citations in task descriptions are normative for constrained fields
- Do not unit-test AppHost graph (constitution); review graphs against research.md
- UK English in operator-facing strings; no em dash
- Tokens must not appear in snapshot JSON, queue messages, or logs