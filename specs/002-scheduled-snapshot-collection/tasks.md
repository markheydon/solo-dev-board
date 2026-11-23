---

description: "Task list for scheduled snapshot collection implementation"
---

# Tasks: Scheduled snapshot collection

**Input**: Design documents from `/specs/002-scheduled-snapshot-collection/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Unit, store, and bUnit tests are required per plan.md and quickstart.md (not TDD-first; implement alongside behaviour).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

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
- [ ] T018 [P] Application tests for additive snapshot field defaults and reason parsing in `tests/SoloDevBoard.Application.Tests/`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Collect overnight without the operator waiting (Priority: P1) 🎯 MVP

**Goal**: Hosted sign-in queues and runs the existing snapshot collection once per UTC day at 05:00 UTC, one account at a time, while the operator is away and `web` is scaled to zero.

**Independent Test**: Per spec.md US1 independent test: two eligible signed-out operators receive queued daily collections; ineligible operators do not; snapshots match hand-started results.

### Tests for User Story 1

- [ ] T019 [P] [US1] Unit tests for occasion window and single fill (`TryFillDailyOccasion`) in `tests/SoloDevBoard.Application.Tests/` per quickstart.md section 3 (05:00, 05:10 grace, 05:16 miss, no double fill)
- [ ] T020 [P] [US1] Unit tests for eligibility: 30-day boundary (`HostedSignInRecord.completedAt`), `credentialUnusable`, admission lists (including both lists empty while admission is on; user or sole admitting organisation removed from lists at fill while inside 30-day window per SC-010), never-signed-in excluded in `tests/SoloDevBoard.Application.Tests/`
- [ ] T069 [P] [US1] Store or Application test: daily success produces the same catalogue and snapshot facts as hand-started collection for the same operator fixture (FR-003, SC-002) in `tests/SoloDevBoard.Application.Tests/` or `tests/SoloDevBoard.Infrastructure.Tests/`

### Implementation for User Story 1

- [X] T021 [US1] Implement `IDailyOccasionService.TryFillDailyOccasion` with 05:00–05:15 UTC window, occasion marker create `IfNoneMatch *`, and enqueue each eligible login once in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T022 [US1] Implement occasion marker blob adapter (`collection-schedule/occasions/{yyyy-MM-dd}.json`) per data-model.md `OccasionMarker.occasionDate`, `OccasionMarker.filledAt`, `OccasionMarker.operators` in `src/SoloDevBoard.Infrastructure/Snapshots/`
- [X] T023 [US1] Implement queue publisher for `daily-snapshot-collection` messages per data-model.md `QueueMessage.occasionDate`, `QueueMessage.operatorLogin`, `QueueMessage.kind` in `src/SoloDevBoard.Infrastructure/Snapshots/` (no tokens in body)
- [X] T024 [US1] On fill, set snapshot `queuedOccasion` to today's UTC date per data-model.md `SnapshotDocument.queuedOccasion` without starting the pass
- [X] T025 [US1] Implement daily collection pass orchestration reusing existing snapshot walk in `src/SoloDevBoard.Application/Services/Snapshots/` (`trigger` = `daily`, `attemptsUsed` starts at 1 per data-model.md)
- [X] T026 [US1] Implement `runner` blob lease in `collection-schedule` so only one daily pass runs at a time across worker copies in `src/SoloDevBoard.Infrastructure/Snapshots/`
- [X] T027 [US1] Queue consumer: start pass (`passExecuting`, `passLeaseUntil` renewal ~10s), complete message after pass, per contracts/daily-collection-schedule.md in `src/SoloDevBoard.Application/Services/Snapshots/`
- [X] T028 [US1] Timer trigger NCRONTAB `0 0-15 5 * * *` calling fill in `src/SoloDevBoard.Functions/` (UTC, no `WEBSITE_TIME_ZONE`)
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

- [ ] T032 [P] [US2] Application tests that `StartAsync` refuses when `queuedOccasion` is set or daily `inProgress` in `tests/SoloDevBoard.Application.Tests/`
- [ ] T071 [P] [US2] Application or bUnit tests that loading `/repositories` and live catalogue reload do not enqueue or start a collection (FR-010) in `tests/SoloDevBoard.Application.Tests/` or `tests/SoloDevBoard.Web.Tests/`

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

- [ ] T036 [P] [US3] Application tests with lean posture: no fill and no queue at 05:00 UTC in `tests/SoloDevBoard.Application.Tests/`

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

- [ ] T040 [P] [US4] Application tests for single flight: two eligible operators, one pass at a time, second starts after first in `tests/SoloDevBoard.Application.Tests/`
- [ ] T041 [P] [US4] Application tests: fill does not add second entry when `queuedOccasion` or daily `inProgress` already set in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 4

- [X] T042 [US4] When `runner` lease is held by another copy, re-send same queue payload to tail and complete current message per research.md in queue handler
- [X] T043 [US4] Prevent duplicate enqueue for same occasion and operator in fill and consumer paths
- [X] T044 [US4] Expose current run trigger (`operator` vs `daily`) in status DTO and in-progress poll in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [X] T045 [US4] Keep ~1s in-progress poll showing recorded count and trigger without full reload per contracts/repositories-schedule-copy.md; bUnit or Web test that navigation away from `/repositories` during an in-progress daily run still works (FR-011)

**Checkpoint**: Concurrency and UI concurrency rules satisfied

---

## Phase 7: User Story 5 - See that the day has a fixed collection time (Priority: P2)

**Goal**: Repositories page states next 05:00 UTC occasion or why no schedule runs; no settings for time, frequency, or 30 days.

**Independent Test**: Per spec.md US5: next occasion, credential failure copy, no edit controls.

### Tests for User Story 5

- [ ] T046 [P] [US5] bUnit tests for schedule sentences and absence of settings/retry controls in `tests/SoloDevBoard.Web.Tests/` per quickstart.md section 5

### Implementation for User Story 5

- [X] T047 [US5] Implement `NextOccasionAtUtc` and `NoScheduleReason` rules (today vs following day, queued-before-start, missed window) in `src/SoloDevBoard.Application/Services/Snapshots/` per data-model.md status facts
- [X] T048 [US5] Add hosted schedule and run clause copy to status alert per contracts/repositories-schedule-copy.md in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [ ] T049 [US5] Update `website/content/docs/repositories.md` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` in the same change

**Checkpoint**: Operator-readable schedule truth on `/repositories`

---

## Phase 8: User Story 6 - Keep the last success when the overnight run does not finish cleanly (Priority: P2)

**Goal**: Retries (up to 3 further tries), interruptions, credential wait, no retry button; prior success unchanged on failure.

**Independent Test**: Per spec.md US6 independent test and quickstart.md section 4.

### Tests for User Story 6

- [ ] T050 [P] [US6] Store tests for `scheduledCollectionStopped` interruption branch in `tests/SoloDevBoard.Infrastructure.Tests/`
- [ ] T072 [P] [US6] Infrastructure tests: etag conditional writes on hosted sign-in and occasion marker blobs; fixtures assert no refresh token in snapshot JSON or queue message bodies (plan.md test scope) in `tests/SoloDevBoard.Infrastructure.Tests/`
- [ ] T051 [P] [US6] Application tests: rate-limit retry to tail (max 4 attempts), sign-in failure no retry, queued-not-interrupted on worker stop in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 6

- [X] T052 [US6] On rate limit or unexpected with `attemptsUsed` &lt; 4, complete message, enqueue `kind` `retry`, clear `passExecuting`, keep `inProgress` per data-model.md transitions
- [X] T053 [US6] On redelivery with `passExecuting` true, write `interrupted` with `scheduledCollectionStopped` and complete message (no requeue) per contracts/daily-collection-schedule.md
- [X] T054 [US6] Web read path: when `passLeaseUntil` is past and `passExecuting`, write interruption same as worker redelivery in `src/SoloDevBoard.Infrastructure/Snapshots/BlobOperatorSnapshotStore.cs`
- [X] T055 [US6] Show failed/interrupted runs with last success finish time and daily trigger copy in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [X] T056 [US6] Confirm no separate retry control is added anywhere under `src/SoloDevBoard.Web/`

**Checkpoint**: Failure and recovery behaviour matches operator snapshot collection semantics

---

## Phase 9: User Story 7 - Skip a missed day, and follow recent sign-ins (Priority: P2)

**Goal**: 15-minute fill grace only; no catch-up same UTC day; sign-in after 05:00 waits for next occasion; admission and window rules at fill.

**Independent Test**: Per spec.md US7 and quickstart.md occasion table rows for 05:16 miss and 05:10 fill.

### Tests for User Story 7

- [ ] T057 [P] [US7] Application tests: no fill after 05:15; fill at 05:10 when marker absent; sign-in after fill does not enqueue same day in `tests/SoloDevBoard.Application.Tests/`
- [ ] T058 [P] [US7] Application tests: organisation allowance from saved logins only (no GitHub membership call) in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 7

- [X] T059 [US7] Enforce no marker and no fill after 05:15:00 UTC same day in `TryFillDailyOccasion`
- [X] T060 [US7] Functions worker start repair (or first fill tick after restart): enqueue missing `initial` only for logins on existing marker still `queuedOccasion` without message per data-model.md occasion marker paragraph and contracts/daily-collection-schedule.md (not web; web does not reference `collection-schedule`)
- [X] T061 [US7] Apply `HostedAdmissionControl` lists at fill without full allow-list scan in Application eligibility
- [X] T062 [US7] Exclude operators outside 30-day window (`completedAt` exactly 30 days inside, older outside per data-model.md sign-in window rules)

**Checkpoint**: All user stories independently functional

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: AppHost wiring, deployment docs, and PR validation

- [X] T063 Wire hosted sign-in graph only in `src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs`: `WithHostStorage(operator-snapshot-storage)`, blob containers `hosted-sign-ins` and `collection-schedule`, `AddQueues("operator-snapshot-queues")`, `AddAzureFunctionsProject`, worker `WithReference` and local `WaitFor` emulator per research.md (after ADR-0049 Accepted)
- [X] T064 Leave `src/SoloDevBoard.AppHost/LeanAppHostGraph.cs` unchanged (no Functions, no extra storage)
- [X] T065 Re-assert `web` `MinReplicas` 0 and `MaxReplicas` 1 in `src/SoloDevBoard.AppHost/SoloDevBoardAppHost.cs` after any publish callback changes
- [X] T066 [P] Assign Functions identity storage roles on publish (blob, queue, table, account contributor) per research.md on hosted graph only
- [ ] T067 [P] Update `docs/getting-started.md`, `docs/deployment.md`, and `src/SoloDevBoard.AppHost/README.md` for new hosted resources
- [ ] T073 [P] Application tests: two operators cannot read each other's snapshot status or blob paths (daily and hand-started runs) per FR-014 and SC-003 in `tests/SoloDevBoard.Application.Tests/` (extend feature 001 isolation fixtures where applicable)
- [ ] T068 Run quickstart.md validation commands: `dotnet format`, `dotnet build`, `dotnet test` per specs/002-scheduled-snapshot-collection/quickstart.md section 6

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start with T001 (ADR gate before T063 AppHost edits)
- **Foundational (Phase 2)**: Depends on Phase 1 packages and Functions project shell
- **User Stories (Phases 3–9)**: All depend on Foundational completion
  - US1 (Phase 3) is the MVP and blocks meaningful validation of US4–US7 worker behaviour
  - US2–US4 can proceed soon after US1 core queue path exists
  - US5–US7 layer on status, failure, and eligibility rules
- **Polish (Phase 10)**: Depends on ADR-0049 and core Application/Functions code; T063 can run after T028–T030 are sketched but must ship before hosted manual validation

### User Story Dependencies

- **User Story 1 (P1)**: After Foundational - no dependency on other stories (MVP)
- **User Story 2 (P1)**: After Foundational; integrates with US1 queue flags
- **User Story 3 (P1)**: After Foundational; parallel with US1 implementation where posture gates are clear
- **User Story 4 (P1)**: After US1 queue and runner exist
- **User Story 5 (P2)**: After Foundational status facts (T015–T016); full copy after US1–US4 behaviour stable
- **User Story 6 (P2)**: After US1 pass and queue consumer
- **User Story 7 (P2)**: Overlaps US1 fill; dedicated tests after eligibility service exists

### Within Each User Story

- Tests marked [P] can be written against interfaces as implementations land; quickstart scenarios are the acceptance checklists
- Application services before Functions triggers
- Infrastructure adapters before end-to-end queue tests
- Page copy after status DTO fields exist

### Parallel Opportunities

- T003, T004, T005, T006 in Phase 1 after T002
- T010, T011, T018 in Phase 2 in parallel after T007 starts
- T019, T020 parallel in US1
- T032–T041 test tasks parallel with sibling implementation where files differ
- T046–T051 bUnit and store tests parallel with Application retry work
- T066, T067 parallel in Polish

---

## Parallel Example: User Story 1

```bash
# Eligibility and occasion window tests together:
tests/SoloDevBoard.Application.Tests/   # T019, T020

# Storage adapters in parallel after Application contracts exist:
src/SoloDevBoard.Infrastructure/Snapshots/   # T022 occasion marker, T023 queue publisher, T026 runner lease
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (ADR-0049 Accepted before AppHost)
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (timer, fill, queue, single pass, daily success)
4. **STOP and VALIDATE**: quickstart section 3 Application tests and manual hosted `aspire start` smoke (no production GitHub)
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → schema and sign-in blob ready
2. US1 → unattended collection → MVP
3. US2 + US3 + US4 → hand start, lean copy, concurrency
4. US5 → operator-visible schedule sentences
5. US6 + US7 → failures, retries, missed day, admission
6. Polish → AppHost, docs, CI commands

### Parallel Team Strategy

1. Team completes Setup + Foundational together
2. After Foundational:
   - Developer A: US1 Functions triggers and queue consumer
   - Developer B: US2–US4 Web start control and poll
   - Developer C: US5–US7 status, copy, eligibility edge tests
3. One developer owns AppHost polish (T063–T066) after ADR-0049

---

## Notes

- [P] tasks = different files, no dependencies on incomplete siblings
- [Story] label maps task to spec.md user scenarios for traceability
- data-model.md field citations in task descriptions are normative for constrained fields
- Do not unit-test AppHost graph (constitution); review graphs against research.md
- UK English in operator-facing strings; no em dash
- Tokens must not appear in snapshot JSON, queue messages, or logs

## Phase 11: Convergence

- [X] T074 Extend `HostedCollectionEligibilityEvaluator.IsEligibleAtFill` in `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs` to refuse fill when any collection is `inProgress` (not only `trigger` daily) or when `queuedOccasion` is set for any date (previous occasion still queued) per FR-008, US4/AC1, and `data-model.md` fill rule (contradicts)
- [X] T075 Wire `DailySnapshotQueueInvocation.UpdateVisibilityAsync` (and explicit completion if required) in `src/SoloDevBoard.Functions/DailyCollectionFunctions.cs` to renew Azure queue visibility while `passExecuting` per `specs/002-scheduled-snapshot-collection/research.md` queue table and `contracts/daily-collection-schedule.md`; stop using no-op delegates so long passes are not misclassified as redelivery interruptions (partial)

## Phase 12: Convergence

- [X] T076 Exclude operators whose `HostedSignInRecord.completedAt` is after today's 05:00 UTC occasion from that UTC day's fill (`HostedCollectionEligibilityEvaluator.IsEligibleAtFill`) and from naming today as `NextOccasionAtUtc` before the following day (`OperatorSnapshotScheduleFactsService.TryComputeNextOccasion`); add Application unit tests for late fill (e.g. 05:10) and in-window page facts per FR-016, US7/AC4, and SC-008 (partial)