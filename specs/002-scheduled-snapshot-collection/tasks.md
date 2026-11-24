---

description: "Task list for scheduled snapshot collection implementation"
---

# Tasks: Scheduled snapshot collection

**Input**: Design documents from `/specs/002-scheduled-snapshot-collection/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Unit, store, and bUnit tests are required per plan.md and quickstart.md (not TDD-first; implement alongside behaviour).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Scheduling model (2026-11-24 spec)**: Queue **fill** is bounded by 05:00 UTC and the 15-minute grace window. **Processing** runs whenever the scheduled collection process is running, with no time-of-day gate on the queue consumer. **Run rules** (allowance and 30-day window at dequeue time) differ from **fill rules** (FR-021).

**Artifact vs code**: Phases 1–11 are marked complete on the feature branch. Phase 12 (T079–T095) is the open implementation backlog for the 2026-11-24 revision; FR/SC task coverage is complete, including `data-model.md` fields `SnapshotDocument.retainedRun` and `QueueMessage.notBefore` via Phase 12 tasks.

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

**Goal**: Hosted sign-in fills the scheduled scan queue once per UTC day (05:00 UTC default, 15-minute grace), then processes entries whenever the worker runs. One pass at a time. Run rules at dequeue may skip a queued person without recording failure.

**Independent Test**: Per spec.md US1: two eligible signed-out operators are queued at fill and collected when the queue is processed (any time); ineligible operators are not queued; snapshots match hand-started results; queued scan removed at run time when allowance or 30-day window fails.

### Tests for User Story 1

- [X] T019 [P] [US1] Unit tests for occasion window and single fill (`TryFillDailyOccasion`) in `tests/SoloDevBoard.Application.Tests/` per quickstart.md section 3 (05:00, 05:10 grace, 05:16 miss, no double fill)
- [X] T020 [P] [US1] Unit tests for fill eligibility: 30-day boundary (`HostedSignInRecord.completedAt`), `credentialUnusable`, admission lists in `tests/SoloDevBoard.Application.Tests/`
- [X] T069 [P] [US1] Store or Application test: daily success produces the same catalogue and snapshot facts as hand-started collection for the same operator fixture (FR-003, SC-002) in `tests/SoloDevBoard.Application.Tests/` or `tests/SoloDevBoard.Infrastructure.Tests/`

### Implementation for User Story 1

- [X] T021 [US1] Implement `IDailyOccasionService.TryFillDailyOccasion` with 05:00–05:15 UTC window, occasion marker create `IfNoneMatch *`, and enqueue each eligible login once in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T022 [US1] Implement occasion marker blob adapter (`collection-schedule/occasions/{yyyy-MM-dd}.json`) per data-model.md `OccasionMarker.occasionDate`, `OccasionMarker.filledAt`, `OccasionMarker.operators` in `src/SoloDevBoard.Infrastructure/Snapshots/`
- [X] T023 [US1] Implement queue publisher for `daily-snapshot-collection` messages per data-model.md `QueueMessage.occasionDate`, `QueueMessage.operatorLogin`, `QueueMessage.kind` in `src/SoloDevBoard.Infrastructure/Snapshots/` (no tokens in body)
- [X] T024 [US1] On fill, set snapshot `queuedOccasion` to today's UTC date per data-model.md `SnapshotDocument.queuedOccasion` without starting the pass
- [X] T025 [US1] Implement daily collection pass orchestration reusing existing snapshot walk in `src/SoloDevBoard.Application/Services/Snapshots/` (`trigger` = `daily`, `attemptsUsed` starts at 1 per data-model.md)
- [X] T026 [US1] Implement `runner` blob lease in `collection-schedule` so only one daily pass runs at a time across worker copies in `src/SoloDevBoard.Infrastructure/Snapshots/`
- [X] T027 [US1] Queue consumer: start pass (`passExecuting`, `passLeaseUntil` renewal ~10s), complete message after pass, per contracts/daily-collection-schedule.md in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T028 [US1] Timer trigger NCRONTAB `0 0-15 5 * * *` calling fill only in `src/SoloDevBoard.Functions/` (UTC, no `WEBSITE_TIME_ZONE`; queue trigger has no fill-clock gate per FR-021)
- [X] T029 [US1] Queue trigger with `batchSize` 1 wired to daily pass handler in `src/SoloDevBoard.Functions/`
- [X] T030 [US1] Register `AddAzureQueueServiceClient("operator-snapshot-queues")` and snapshot/sign-in clients in `src/SoloDevBoard.Functions/Program.cs` then `AddSoloDevBoard`
- [X] T031 [US1] Set `credentialUnusable` on sign-in record after daily `signInOrToken` failure per data-model.md `HostedSignInRecord.credentialUnusable` in Application/Infrastructure
- [X] T070 [US1] On daily pass token exchange, persist rotated `HostedSignInRecord.refreshToken` and `HostedSignInRecord.refreshTokenExpiresAt` under blob etag per contracts/hosted-sign-in-record.md (Functions worker path; shared port from T012)

**Checkpoint**: User Story 1 deliverable - unattended daily collection succeeds for eligible hosted operators

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

**Goal**: Repositories page states the next 05:00 UTC **queue fill** that will include them, or why no scheduled collection will run, including after a queued entry is removed at run rules. No settings for time, frequency, or 30 days.

**Independent Test**: Per spec.md US5: next occasion, credential failure copy, no edit controls; removed queue entry shows reason without in-progress copy.

### Tests for User Story 5

- [X] T046 [P] [US5] bUnit tests for schedule sentences and absence of settings/retry controls in `tests/SoloDevBoard.Web.Tests/` per quickstart.md section 5

### Implementation for User Story 5

- [X] T047 [US5] Implement `NextOccasionAtUtc` and `NoScheduleReason` rules (today vs following day, queued-before-start, missed window) in `src/SoloDevBoard.Application/Services/Snapshots/` per data-model.md status facts
- [X] T048 [US5] Add hosted schedule and run clause copy to status alert per contracts/repositories-schedule-copy.md in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [X] T049 [US5] Update `website/content/docs/repositories.md` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` in the same change

**Checkpoint**: Operator-readable schedule truth on `/repositories`

---

## Phase 8: User Story 6 - Keep the last success when the overnight run does not finish cleanly (Priority: P2)

**Goal**: Retries (up to 3 further tries with minimum wall-clock waits), interruptions, credential wait, no retry button; prior success unchanged on failure.

**Independent Test**: Per spec.md US6 and quickstart.md section 4, including staggered unexpected-error waits and solo-account minimum wait.

### Tests for User Story 6

- [X] T050 [P] [US6] Store tests for `scheduledCollectionStopped` interruption branch in `tests/SoloDevBoard.Infrastructure.Tests/`
- [X] T072 [P] [US6] Infrastructure tests: etag conditional writes on hosted sign-in and occasion marker blobs; fixtures assert no refresh token in snapshot JSON or queue message bodies (plan.md test scope) in `tests/SoloDevBoard.Infrastructure.Tests/`
- [X] T051 [P] [US6] Application tests: rate-limit retry to tail (max 4 attempts), sign-in failure no retry, queued-not-interrupted on worker stop in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 6

- [X] T052 [US6] On rate limit or unexpected with `attemptsUsed` &lt; 4, complete message, enqueue `kind` `retry`, clear `passExecuting`, keep `inProgress` per data-model.md transitions
- [X] T053 [US6] On redelivery with `passExecuting` true, write `interrupted` with `scheduledCollectionStopped` and complete message (no requeue) per contracts/daily-collection-schedule.md
- [X] T054 [US6] Web read path: when `passLeaseUntil` is past and `passExecuting`, write interruption same as worker redelivery in `src/SoloDevBoard.Infrastructure/Snapshots/BlobOperatorSnapshotStore.cs`
- [X] T055 [US6] Show failed/interrupted runs with last success finish time and daily trigger copy in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [X] T056 [US6] Confirm no separate retry control is added anywhere under `src/SoloDevBoard.Web/`

**Checkpoint**: Failure and recovery behaviour matches operator snapshot collection semantics

---

## Phase 9: User Story 7 - Skip a missed day, and follow recent sign-ins (Priority: P2)

**Goal**: 15-minute fill grace only; no catch-up same UTC day; sign-in after fill waits for next occasion; admission and 30-day window at **fill** for queuing (dequeue and page run rules are Phase 12).

**Independent Test**: Per spec.md US7 and quickstart.md occasion table rows for 05:16 miss, 05:10 fill, and queued work after 05:15.

### Tests for User Story 7

- [X] T057 [P] [US7] Application tests: no fill after 05:15; fill at 05:10 when marker absent; sign-in after fill does not enqueue same day in `tests/SoloDevBoard.Application.Tests/`
- [X] T058 [P] [US7] Application tests: organisation allowance from saved logins only (no GitHub membership call) in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 7

- [X] T059 [US7] Enforce no marker and no fill after 05:15:00 UTC same day in `TryFillDailyOccasion`
- [X] T060 [US7] Functions worker start repair (or first fill tick after restart): enqueue missing `initial` only for logins on existing marker still `queuedOccasion` without message per data-model.md occasion marker paragraph and contracts/daily-collection-schedule.md (not web; web does not reference `collection-schedule`)
- [X] T061 [US7] Apply `HostedAdmissionControl` lists at fill without full allow-list scan in Application eligibility
- [X] T062 [US7] Exclude operators outside 30-day window at fill (`completedAt` exactly 30 days inside, older outside per data-model.md sign-in window rules at occasion instant)

**Checkpoint**: Fill-path and occasion rules functional; Phase 12 closes dequeue run rules for full spec conformance

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: AppHost wiring, deployment docs, and PR validation

- [X] T063 Wire hosted sign-in graph only in `src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs`: `WithHostStorage(operator-snapshot-storage)`, blob containers `hosted-sign-ins` and `collection-schedule`, `AddQueues("operator-snapshot-queues")`, `AddAzureFunctionsProject`, worker `WithReference` and local `WaitFor` emulator per research.md (after ADR-0049 Accepted)
- [X] T064 Leave `src/SoloDevBoard.AppHost/LeanAppHostGraph.cs` unchanged (no Functions, no extra storage)
- [X] T065 Re-assert `web` `MinReplicas` 0 and `MaxReplicas` 1 in `src/SoloDevBoard.AppHost/SoloDevBoardAppHost.cs` after any publish callback changes
- [X] T066 [P] Assign Functions identity storage roles on publish (blob, queue, table, account contributor) per research.md on hosted graph only
- [X] T067 [P] Update `docs/getting-started.md`, `docs/deployment.md`, and `src/SoloDevBoard.AppHost/README.md` for new hosted resources
- [X] T073 [P] Application tests: two operators cannot read each other's snapshot status or blob paths (daily and hand-started runs) per FR-014 and SC-003 in `tests/SoloDevBoard.Application.Tests/` (extend feature 001 isolation fixtures where applicable)
- [X] T068 Run quickstart.md validation commands: `dotnet format`, `dotnet build`, `dotnet test` per specs/002-scheduled-snapshot-collection/quickstart.md section 6

---

## Phase 11: Prior convergence (delivered)

- [X] T074 Extend `HostedCollectionEligibilityEvaluator.IsEligibleAtFill` in `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs` to refuse fill when any collection is `inProgress` or when `queuedOccasion` is set (FR-008)
- [X] T075 Wire `DailySnapshotQueueInvocation.UpdateVisibilityAsync` in `src/SoloDevBoard.Functions/DailyCollectionFunctions.cs` to renew Azure queue visibility while `passExecuting`
- [X] T076 Exclude operators whose `HostedSignInRecord.completedAt` is after today's occasion start from that UTC day's fill and from naming today as `NextOccasionAtUtc` before the following day (FR-016)
- [X] T077 Keep daily retry-wait `inProgress` with `passExecuting` false from being rewritten to `siteStopped` on web read (FR-020)
- [X] T078 Present queue-waiting status (not in progress) when `queuedOccasion` is set and `passExecuting` is false (FR-006, US5)

---

## Phase 12: Spec revision 2026-11-24 (run rules, retainedRun, notBefore)

**Purpose**: Close gaps between delivered code and the updated spec, plan, and data-model (`retainedRun`, dequeue run rules, FR-021, FR-020 minimum waits).

**⚠️ CRITICAL**: Phase 12 blocks declaring the feature converged against the current spec.

### Tests

- [X] T079 [P] [US1] Application tests: `IsEligibleAtRun` at current UTC (not occasion instant); message processed after 05:15 when still allowed and inside 30 days; fill instant must not apply at dequeue in `tests/SoloDevBoard.Application.Tests/` per quickstart.md section 3
- [X] T080 [P] [US1] Application tests: run-rule withdrawal clears `queuedOccasion` only when pass never started; no `failed` run; `repositories` and `lastSuccessfulCollectionAt` unchanged in `tests/SoloDevBoard.Application.Tests/`
- [X] T081 [P] [US6] Application tests: further try respects `notBefore` when alone on queue (2 min rate limit default; 3/6/12 min stagger for unexpected); another visible account runs first; minimum still applies in `tests/SoloDevBoard.Application.Tests/` per SC-012 and quickstart.md section 4
- [X] T082 [P] [US6] Infrastructure or store tests: `retainedRun` restore when further try withdrawn by run rules in `tests/SoloDevBoard.Infrastructure.Tests/` per data-model.md `SnapshotDocument.retainedRun`
- [X] T083 [P] [US5] bUnit: after queued entry removed (`NoScheduleReason` `notAllowed` or `outsideWindow`), alert shows reason and does not show in progress, failed, or interrupted for that removal per contracts/repositories-schedule-copy.md schedule clause row

### Implementation

- [X] T084 Add `retainedRun` object to snapshot document types and JSON serialization in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotDocument.cs` and Infrastructure serializers per data-model.md `SnapshotDocument.retainedRun`
- [X] T085 [US1] Add `IsEligibleAtRun(HostedSignInRecord, DateTimeOffset nowUtc)` to `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs` using current allow lists and `completedAt` inside 30 days of now (dequeue, not fill instant)
- [X] T086 [US1] Before starting a pass in `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`, apply run rules; on failure complete message without GitHub walk and withdraw per contracts/daily-collection-schedule.md (clear `SnapshotDocument.queuedOccasion` when pass not started; do not set `trigger` daily until a pass starts)
- [X] T087 [US6] On first daily pass start, set `SnapshotDocument.retainedRun` to prior terminal `run` and `trigger` when replacing a terminal run; clear `retainedRun` on success, terminal failure, or interruption in `DailySnapshotCollectionPassService` and coordinator paths
- [X] T088 [US6] When a further try is withdrawn by run rules, restore from `SnapshotDocument.retainedRun`, clear attempt fields and daily in-progress presentation in `DailySnapshotCollectionPassService.cs`
- [X] T089 Add `NotBefore` (ISO-8601 UTC) to `DailySnapshotQueueMessage` in `src/SoloDevBoard.Application/Services/Snapshots/IDailySnapshotCollectionPassService.cs` per data-model.md `QueueMessage.notBefore`
- [X] T090 [US6] In `src/SoloDevBoard.Infrastructure/Snapshots/AzureDailySnapshotQueuePublisher.cs`, set initial visibility timeout to `notBefore - now` when positive (cap 7 days); serialize `notBefore` on `retry` messages only
- [X] T091 [US6] When enqueueing a `retry`, set `notBefore` from the failed attempt: GitHub retry delay from the failed HTTP response when present (`Retry-After` or `X-RateLimit-Reset`), else 2 minutes for rate limit; 3, 6, then 12 minutes for unexpected errors by `attemptsUsed` per research.md minimum-wait table (thread from existing rate-limit mapping in `OperatorSnapshotFailureMapper` or GitHub client, no extra GitHub call)
- [X] T092 [US6] If a message is received before `notBefore`, re-queue with remaining invisibility and complete the current delivery without starting a pass in `DailySnapshotCollectionPassService.cs`
- [X] T093 [US4] When re-sending to tail after losing `runner` lease, preserve `notBefore` visibility for `retry` messages in queue handler and publisher paths
- [X] T094 [US5] Ensure `OperatorSnapshotScheduleFactsService` sets `NoScheduleReason` `notAllowed` or `outsideWindow` when run rules would fail at page read time, including when `queuedOccasion` was just cleared, per data-model.md status facts (`QueuedForOccasion` does not override those reasons)
- [X] T095 Re-run quickstart.md section 6 commands after Phase 12 changes in `specs/002-scheduled-snapshot-collection/quickstart.md`
- [X] T096 [US5] Update `website/content/docs/repositories.md` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` in the same change as T083 and T094 per contracts/repositories-schedule-copy.md guide section

**Checkpoint**: Code matches FR-021, FR-020 minimum waits, and contracts/daily-collection-schedule.md worker outcome rows for run-rule removal

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start with T001 (ADR gate before T063 AppHost edits)
- **Foundational (Phase 2)**: Depends on Phase 1 packages and Functions project shell
- **User Stories (Phases 3–9)**: All depend on Foundational completion
- **Prior convergence (Phase 11)**: Delivered on branch; superseded in part by Phase 12
- **Spec revision (Phase 12)**: Depends on Phases 3–9 core queue path; **blocks** spec-complete validation
- **Polish (Phase 10)**: AppHost shipped; re-validate after Phase 12

### User Story Dependencies

- **User Story 1 (P1)**: After Foundational - MVP; T085–T086 extend US1 for dequeue run rules
- **User Story 2 (P1)**: After Foundational; integrates with US1 queue flags
- **User Story 3 (P1)**: After Foundational; parallel with US1 where posture gates are clear
- **User Story 4 (P1)**: After US1 queue and runner exist; T093 extends lease loser behaviour
- **User Story 5 (P2)**: T083, T094 after run-rule withdrawal (T086, T088)
- **User Story 6 (P2)**: T087–T092 after T089–T090 message shape
- **User Story 7 (P2)**: Fill rules delivered; dequeue 30-day re-check is T085 (run time, not fill)

### Parallel Opportunities

- T079–T083 test tasks in parallel once T084–T085 exist
- T089–T090 queue message shape before T091–T092 retry timing
- T084 `retainedRun` schema before T087–T088

---

## Parallel Example: Phase 12

```bash
# Schema and evaluator first:
src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotDocument.cs  # T084
src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs  # T085

# Then dequeue handler and publisher in parallel:
src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs  # T086–T088, T092
src/SoloDevBoard.Infrastructure/Snapshots/AzureDailySnapshotQueuePublisher.cs  # T090–T091
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (ADR-0049 Accepted before AppHost)
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (timer fill, queue, single pass, daily success)
4. Complete **Phase 12** for spec-complete US1 (run rules at dequeue, queue processing without fill clock)
5. **STOP and VALIDATE**: quickstart section 3 Application tests and hosted `aspire start` smoke (no production GitHub)

### Incremental Delivery

1. Delivered: Setup through Polish and Phase 11 convergence
2. **Next**: Phase 12 (spec revision 2026-11-24)
3. Re-run T095 and full PR checklist

### Suggested MVP scope

- **Shippable MVP**: User Story 1 with Phase 12 complete (unattended queue fill and process, run rules, minimum retry waits)
- **Already on branch**: US2–US7 and AppHost polish (Phases 4–10)

---

## Notes

- [P] tasks = different files, no dependencies on incomplete siblings
- [Story] label maps task to spec.md user scenarios for traceability
- data-model.md field citations in task descriptions are normative for constrained fields
- Do not unit-test AppHost graph (constitution); review graphs against research.md
- UK English in operator-facing strings; no em dash
- Tokens must not appear in snapshot JSON, queue messages, or logs