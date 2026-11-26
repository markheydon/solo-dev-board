---

description: "Task list for scheduled snapshot collection implementation"
---

# Tasks: Scheduled snapshot collection

**Input**: Design documents from `/specs/002-scheduled-snapshot-collection/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Unit, store, and bUnit tests are required per plan.md and quickstart.md (not TDD-first; implement alongside behaviour).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Scheduling model (2026-11-24 spec and plan)**: This delivery **asks** for a queue fill once per UTC day at 05:00 UTC, plus catch-up when the worker is back after 05:00 with no occasion marker, and `unmetFill` 15 minutes after a terminal technical outcome. A fill may run at any UTC time; it is not limited to a 15-minute window and the occasion marker is not a once-per-day lock. **Queue processing** runs whenever the worker is up, with no time-of-day gate. **Fill rules** (30-day window at fill instant, admission, not queued or in progress, no `credentialUnusable`) differ from **run rules** at dequeue (attempt unless not allowed; do not re-check the 30-day window; `signInOrToken` when stored access cannot be used).

**Account scan status (2026-11-25 spec and plan)**: One stored record per hosted account in container `account-scan-status`. The page reads that record. `stalled` is reported only. A reported `stalled` account is not queued and is not in progress. Phases 13–22 are delivered on this branch. Do not treat `queuedOccasion`, `passExecuting`, or `run.status` as the implementation of FR-022.

**Worker credential and sentences (2026-11-26 spec and plan)**: Phases 23–28 are the open delta for FR-024 and FR-025. The overnight scan uses only `workerRefreshToken` from a second GitHub authorisation on the existing GitHub App, after the browser refresh token is already in the auth cookie. One GitHub response is never both tokens. Schema 1 `refreshToken` is not the worker token. The page renders Application sentences and does not name 05:00 UTC or who started the run. Do not treat a single `refreshToken` on the sign-in blob as the worker credential.

**Artifact vs code**: Phases 1–22 are marked complete. Phase 12 aligned the 2026-11-24 ask, catch-up, dequeue, and `unmetFill` rules. Phases 13–22 aligned code and tests with FR-022 and FR-023. Phases 23–28 align code and tests with FR-024, FR-025, and the 2026-11-26 plan, data-model, contracts, and quickstart. Leave the delivered status machine in place except where a Phase 23–28 task changes a token outcome or the sentences.

**Pre-implement analyze gate**: All FR and SC items map to at least one task. Unchecked rows in Phases 23–28 are expected implementation work, not missing specification coverage.

**Tests**: xUnit v3, Microsoft.Testing.Platform, built-in asserts, NSubstitute only where a port double is required. Names follow `MethodUnderTest_Scenario_ExpectedOutcome`. Use `TimeProvider`. The exactly-5-minutes case and the more-than-5-minutes case stay separate tests. For Phases 23–28, write the listed tests first and see them fail, then implement. Do not unit-test the AppHost graph. No Playwright journey that waits for 05:00 UTC or collects against GitHub.

**Aspire**: The hosted graph is already wired, including `account-scan-status`. This delta adds no container, no NuGet package, and no third sign-in mode. Do not edit `.aspire/modules/`. Do not pass endpoint URLs through `WithEnvironment`. Do not add `WaitFor` between `web` and the Functions project. Leave `src/SoloDevBoard.AppHost/LeanAppHostGraph.cs` unchanged. Amend `adr/adr-0015-github-app-first-hosted-authentication.md` in the same change as the schema 2 write. Leave `adr/adr-0049-scheduled-collection-functions.md` as the worker binding record.

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

- [X] T007 Extend snapshot JSON model with `trigger` and `attemptsUsed` per data-model.md `SnapshotDocument.trigger` and `SnapshotDocument.attemptsUsed` in `src/SoloDevBoard.Application/` snapshot types and serializers. data-model.md `SnapshotDocument.schemaVersion` stays `1`. Historical code also stored `queuedOccasion`, `passExecuting`, and `passLeaseUntil`. Those names are not fields to write for new behaviour (data-model.md). T133 and T143 replace them.
- [X] T008 Add `scheduledCollectionStopped` to run reason enum and parsing in `src/SoloDevBoard.Application/Services/Snapshots/`. Superseded for new behaviour by T143: do not add `scheduledCollectionStopped`. Data-model.md forbids that reason.
- [X] T009 Update `src/SoloDevBoard.Infrastructure/Snapshots/BlobOperatorSnapshotStore.MaybeRewriteInterruptedAsync` so daily passes with `passExecuting` are not rewritten to `siteStopped` when the web process has no live task (research.md interruption table). Superseded by T143: do not read `passExecuting` for new behaviour.
- [X] T010 [P] Introduce hosted sign-in record model in `src/SoloDevBoard.Application/Authentication/` for data-model.md `HostedSignInRecord.schemaVersion`, `HostedSignInRecord.operatorLogin`, `HostedSignInRecord.completedAt`, `HostedSignInRecord.organisationLogins`, and `HostedSignInRecord.credentialUnusable`. A browser refresh token on this blob is historical only. It is not a current field. T158 stores the worker token fields instead.
- [X] T011 [P] Add `IHostedSignInRecordStore` port and blob adapter for container `hosted-sign-ins` in `src/SoloDevBoard.Infrastructure/Identity/` with etag conditional writes per contracts/hosted-sign-in-record.md
- [X] T012 Extract shared GitHub refresh token exchange behind an Application port in `src/SoloDevBoard.Application/Authentication/` and move existing web gateway usage to `src/SoloDevBoard.Infrastructure/Identity/`
- [X] T013 Write hosted sign-in blob on completed sign-in from `src/SoloDevBoard.Web/Authentication/`; sign-out does not delete the blob. Superseded for new behaviour by T159 and T160: the first authorisation does not clear `credentialUnusable` and does not write a worker token. The browser refresh token stays in the auth cookie.
- [X] T014 Register blob container client for `hosted-sign-ins` in `src/SoloDevBoard.Web/Program.cs` via Aspire reference (no queue or `collection-schedule` on web)
- [X] T015 Extend `OperatorSnapshotStatusDto` with schedule posture and refusal facts in `src/SoloDevBoard.Application/Services/Snapshots/`. A historical clock field and a historical queue-occasion flag are not current data-model fields. Current facts are T123 and T167, including data-model.md `OperatorSnapshotStatusDto.Trigger`.
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
- [X] T024 [US1] On fill, set a snapshot queue-occasion flag to today's UTC date without starting the pass. Superseded for new behaviour by T133: do not write that flag. It is not a current snapshot field. Queued and in progress for new work are `AccountScanStatus.status` plus the reported-status table.
- [X] T025 [US1] Implement daily collection pass orchestration reusing existing snapshot walk in `src/SoloDevBoard.Application/Services/Snapshots/` (`trigger` = `daily`, `attemptsUsed` starts at 1 per data-model.md)
- [X] T026 [US1] Implement `runner` blob lease in `collection-schedule` so only one daily pass runs at a time across worker copies in `src/SoloDevBoard.Infrastructure/Snapshots/`
- [X] T027 [US1] Queue consumer: start pass (`passExecuting`, `passLeaseUntil` renewal ~10s), complete message after pass, per contracts/daily-collection-schedule.md in `src/SoloDevBoard.Application/Services/Snapshots/`. Superseded for new behaviour by T142 and T143: do not write `passExecuting` or `passLeaseUntil`.
- [X] T028 [US1] Timer trigger NCRONTAB `0 0-15 5 * * *` calling fill only in `src/SoloDevBoard.Functions/` (revised to `0 0 5 * * *` in Phase 12)
- [X] T029 [US1] Queue trigger with `batchSize` 1 wired to daily pass handler in `src/SoloDevBoard.Functions/`
- [X] T030 [US1] Register `AddAzureQueueServiceClient("operator-snapshot-queues")` and snapshot/sign-in clients in `src/SoloDevBoard.Functions/Program.cs` then `AddSoloDevBoard`
- [X] T031 [US1] Set `credentialUnusable` on sign-in record after daily `signInOrToken` failure per data-model.md `HostedSignInRecord.credentialUnusable` in Application/Infrastructure. Superseded for the newer-token case by T134 and T162: set the flag only when that failure is recorded and no newer worker token is stored. When a newer worker token is already stored, T162 leaves `HostedSignInRecord.credentialUnusable` false and sets `AccountScanStatus.status` to `finished`.
- [X] T070 [US1] On daily pass token exchange, persist a rotated refresh token under the sign-in blob etag (Functions worker path; shared port from T012). Superseded for new behaviour by T162: rotate data-model.md `HostedSignInRecord.workerRefreshToken` only, and do not write the auth-cookie token.

**Checkpoint**: User Story 1 deliverable - unattended daily collection succeeds for eligible hosted operators (spec-complete after Phase 12)

---

## Phase 4: User Story 2 - Keep starting a collection by hand (Priority: P1)

**Goal**: **Collect snapshot** remains on `/repositories`; hand start replaces snapshots like daily success; blocked while queued or in progress.

**Independent Test**: Per spec.md US2: after daily success, hand start replaces snapshot; hand start before daily run still works when not queued.

### Tests for User Story 2

- [X] T032 [P] [US2] Application tests that `StartAsync` refuses when `queuedOccasion` is set or daily `inProgress` in `tests/SoloDevBoard.Application.Tests/`. Superseded by T136: refuse only while callers are told `scheduled` or `running`. A retry wait is `scheduled`. `queuedOccasion` and `run.status` `inProgress` are not the hand-start gate.
- [X] T071 [P] [US2] Application or bUnit tests that loading `/repositories` and live catalogue reload do not enqueue or start a collection (FR-010) in `tests/SoloDevBoard.Application.Tests/` or `tests/SoloDevBoard.Web.Tests/`

### Implementation for User Story 2

- [X] T033 [US2] Enforce hand-start refusal in `src/SoloDevBoard.Application/Services/Snapshots/` when data-model.md queue flags apply (`queuedOccasion` or `run.status` `inProgress` including retry wait). Superseded by T136. T136 replaces this gate.
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
- [X] T041 [P] [US4] Application tests: fill does not add second entry when `queuedOccasion` or daily `inProgress` already set in `tests/SoloDevBoard.Application.Tests/`. Superseded for the duplicate gate by T131 and T133: already queued is told `scheduled`, and in progress is told `running`.

### Implementation for User Story 4

- [X] T042 [US4] When `runner` lease is held by another copy, re-send same queue payload to tail and complete current message per research.md in queue handler
- [X] T043 [US4] Prevent duplicate enqueue for same occasion and operator in fill and consumer paths
- [X] T044 [US4] Expose the current run trigger in the status DTO and in-progress poll in `src/SoloDevBoard.Web/Components/Features/Repositories/`, per data-model.md `OperatorSnapshotStatusDto.Trigger` (`operator`, `daily`, or none). Set it when a scan is `running` or has `finished`. None otherwise, including `never run`, `scheduled`, and `suspended`. T123 keeps this fact on the DTO. This is not data-model.md `SnapshotDocument.trigger`.
- [X] T045 [US4] Keep ~1s in-progress poll showing recorded count and trigger without full reload per contracts/repositories-schedule-copy.md; bUnit or Web test that navigation away from `/repositories` during an in-progress daily run still works (FR-011)

**Checkpoint**: Concurrency and UI concurrency rules satisfied

---

## Phase 7: User Story 5 - See when the next scheduled queue fill is (Priority: P2)

**Goal**: Repositories page states the next 05:00 UTC **ask** that will queue them, or one schedule refusal sentence (`notAllowed`, then `credentialUnusable`, then `outsideWindow`). After today's ask has passed, name the following day at 05:00 UTC when they would be eligible, including when today's scan has not succeeded, unless still queued for today. Superseded for new behaviour by T165-T168: the page renders data-model.md `OperatorSnapshotStatusDto.ScheduleSentence` and does not name 05:00 UTC or the following day. data-model.md `OperatorSnapshotStatusDto.NextAskIncludesAccount` is that judgement, and it is not a clock time.

**Independent Test**: Per spec.md US5 and quickstart.md section 5.

### Tests for User Story 5

- [X] T046 [P] [US5] bUnit tests for schedule sentences and absence of settings/retry controls in `tests/SoloDevBoard.Web.Tests/` per quickstart.md section 5 (extend in Phase 12 T110)

### Implementation for User Story 5

- [X] T047 [US5] Implement `NextOccasionAtUtc` and `NoScheduleReason` rules in `src/SoloDevBoard.Application/Services/Snapshots/` per data-model.md status facts (revised in Phase 12). Superseded for new behaviour by T167: remove `NextOccasionAtUtc`. The page renders data-model.md `OperatorSnapshotStatusDto.ScheduleSentence` and does not name 05:00 UTC or the following day.
- [X] T048 [US5] Add hosted schedule and run clause copy to status alert per contracts/repositories-schedule-copy.md in `src/SoloDevBoard.Web/Components/Features/Repositories/`
- [X] T049 [US5] Update `website/content/docs/repositories.md` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` in the same change (refresh in Phase 12 with T119 and T110)

**Checkpoint**: Operator-readable schedule truth on `/repositories`

---

## Phase 8: User Story 6 - Keep the last success when the overnight run does not finish cleanly (Priority: P2)

**Goal**: Retries (up to 3 further tries with minimum wall-clock waits), interruptions, credential wait, no retry button; `unmetFill` after terminal technical outcomes; prior success unchanged on failure.

**Independent Test**: Per spec.md US6 and quickstart.md section 4.

### Tests for User Story 6

- [X] T050 [P] [US6] Store tests for `scheduledCollectionStopped` interruption branch in `tests/SoloDevBoard.Infrastructure.Tests/`. Superseded by T141 and T143: do not write `scheduledCollectionStopped` or `interrupted` when the worker dies.
- [X] T072 [P] [US6] Infrastructure tests: etag conditional writes on hosted sign-in and occasion marker blobs; fixtures assert no refresh token in snapshot JSON or queue message bodies in `tests/SoloDevBoard.Infrastructure.Tests/`
- [X] T051 [P] [US6] Application tests: rate-limit retry to tail (max 4 attempts), sign-in failure no retry, queued-not-interrupted on worker stop in `tests/SoloDevBoard.Application.Tests/`

### Implementation for User Story 6

- [X] T052 [US6] On rate limit or unexpected with `attemptsUsed` < 4, complete message, enqueue `kind` `retry`, clear `passExecuting`, keep `inProgress` per data-model.md transitions. Superseded by T142: a waiting further try is `scheduled`, not `running` and not `finished`.
- [X] T053 [US6] On redelivery with `passExecuting` true, write `interrupted` with `scheduledCollectionStopped` and complete message (no requeue) per contracts/daily-collection-schedule.md. Superseded by T143: redelivery while `running` completes the message and leaves stored status `running`. Do not write `interrupted` or `scheduledCollectionStopped`.
- [X] T054 [US6] Web read path: when `passLeaseUntil` is past and `passExecuting`, write interruption same as worker redelivery in `src/SoloDevBoard.Infrastructure/Snapshots/BlobOperatorSnapshotStore.cs`. Superseded by T143: that read path must not write the interruption.
- [X] T055 [US6] Show failed/interrupted runs with last success finish time and daily trigger copy in `src/SoloDevBoard.Web/Components/Features/Repositories/`. Superseded by T147: show the reported status words. Do not show an interrupted run for a worker death.
- [X] T056 [US6] Confirm no separate retry control is added anywhere under `src/SoloDevBoard.Web/`
- [X] T084 Add `retainedRun` object to snapshot document types and JSON serialization in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotDocument.cs` and Infrastructure serializers per data-model.md `SnapshotDocument.retainedRun`
- [X] T087 [US6] On first daily pass start, set `SnapshotDocument.retainedRun` to prior terminal `run` and `trigger` when replacing a terminal run; clear `retainedRun` on success or terminal failure in `DailySnapshotCollectionPassService` and coordinator paths. Do not clear it on worker death. That case is T143, which must not write `interrupted`.
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
- [X] T060 [US7] Functions worker start repair: enqueue missing `initial` for logins still `queuedOccasion` in `src/SoloDevBoard.Application/Services/Snapshots/DailyOccasionService.cs` (extend startup fill in Phase 12). The `queuedOccasion` clause is superseded by T153.
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

- [X] T074 [US4] Extend `HostedCollectionEligibilityEvaluator.IsEligibleAtFill` to refuse fill for **this operator** when **their** collection is `inProgress` or when **their** `queuedOccasion` is set; other operators may still be queued on the same fill (FR-008, per-operator gate). Superseded by T145: already queued is told `scheduled`, and in progress is told `running`. Do not read `queuedOccasion`.
- [X] T075 Wire `DailySnapshotQueueInvocation.UpdateVisibilityAsync` in `src/SoloDevBoard.Functions/DailyCollectionFunctions.cs` to renew Azure queue visibility while `passExecuting`. Superseded by T154: renew visibility while stored status is `running` for that generation. Do not key the renewal on `passExecuting` or `passLeaseUntil`.
- [X] T076 Exclude operators whose `HostedSignInRecord.completedAt` is after today's occasion start from that UTC day's fill and from naming today as `NextOccasionAtUtc` (reversed in Phase 12 via T101, T104, T117–T119)
- [X] T077 Keep daily retry-wait `inProgress` with `passExecuting` false from being rewritten to `siteStopped` on web read (FR-020). Superseded by T143: a retry wait is `scheduled`, and the web read path must not rewrite it from `passExecuting`.
- [X] T078 Present queue-waiting status when `queuedOccasion` is set and `passExecuting` is false (FR-006, US5). Superseded by T147: the page reads reported status. A waiting entry is `scheduled`. Do not read `queuedOccasion` or `passExecuting`.

---

## Phase 12: Spec and plan revision 2026-11-24 (delivered)

**Purpose**: Align implementation with the 2026-11-24 ask, catch-up, dequeue, and `unmetFill` rules. Delivered on this branch. FR-022 and FR-023 are Phases 13–22.

**⚠️ CRITICAL**: Phase 12 is the baseline for the account scan status delta. Do not reopen its checked tasks unless a Phase 13–22 task names that replacement.

### Tests for Phase 12

- [X] T097 [P] [US7] Replace fill-window tests in `tests/SoloDevBoard.Application.Tests/DailyOccasionServiceTests.cs`: no fill before 05:00 on worker start; fill at 05:00 when marker absent; catch-up at 05:10 when marker absent; later fill when marker present queues after success; no refusal after 05:15 per quickstart.md section 3
- [X] T098 [P] [US7] Application tests: sign-in at 06:00 UTC queued by fill at 06:30; fill uses `TimeProvider` instant not capped 05:00 for `HostedSignInRecord.completedAt` window in `tests/SoloDevBoard.Application.Tests/HostedCollectionEligibilityEvaluatorTests.cs`
- [X] T099 [P] [US1] Application tests: queued message still runs when `completedAt` is older than 30 days at dequeue; not-allowed still withdraws without failed collection in `tests/SoloDevBoard.Application.Tests/` per quickstart.md section 3
- [X] T100 [P] [US6] Application tests: after fourth failed attempt or interruption, one `unmetFill` with 15-minute hide then fill includes idle eligible operators in `tests/SoloDevBoard.Application.Tests/`; no `unmetFill` on `signInOrToken`. The interruption branch is superseded by T141 and T142. `unmetFill` follows only a finished `rateLimit` or `unexpected` with no tries left, never a lapse, redelivery, or `scheduledCollectionStopped`.
- [X] T082 [P] [US6] Infrastructure tests: occasion marker replace under etag; `unmetFill` and `retry` visibility per data-model.md `QueueMessage.notBefore` in `tests/SoloDevBoard.Infrastructure.Tests/`
- [X] T101 [P] [US5] Application tests: `NextOccasionAtUtc` and `NoScheduleReason` order (`notAllowed`, `credentialUnusable`, `outsideWindow`); following day at 05:00 after ask passed whether or not today's scan succeeded; queued keeps today after 05:00 in `tests/SoloDevBoard.Application.Tests/OperatorSnapshotScheduleFactsServiceTests.cs` (add file if missing). Superseded for new behaviour by T165 and T167: do not assert a clock field named `NextOccasionAtUtc`. Assert data-model.md `OperatorSnapshotStatusDto.NextAskIncludesAccount` and data-model.md `OperatorSnapshotStatusDto.ScheduleSentence`. The page does not name 05:00 UTC or the following day.
- [X] T110 [P] [US5] bUnit: schedule copy per contracts/repositories-schedule-copy.md after today's ask passed and when queued in `tests/SoloDevBoard.Web.Tests/`

### Fill and occasion marker (US1, US7)

- [X] T103 [US1] Remove `IsInFillWindow` and `AlreadyFilled` when marker exists from `src/SoloDevBoard.Application/Services/Snapshots/DailyOccasionService.cs`; use fill instant `utcNow` for eligibility; always run fill logic when hosted schedule enabled and caller invokes the method
- [X] T104 [US7] Remove `HostedSignInRecord.completedAt` after 05:00 UTC exclusion from `IsEligibleAtFill` in `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs` (data-model.md hosted sign-in window at fill instant)
- [X] T105 [US1] Add `ICollectionOccasionMarkerStore.TryReplaceAsync` (etag) and implement in `src/SoloDevBoard.Infrastructure/Snapshots/BlobCollectionOccasionMarkerStore.cs` so later fills the same UTC date union `OccasionMarker.operators` and update `OccasionMarker.filledAt` per data-model.md
- [X] T106 [US1] On fill, queue every eligible person not already queued or in progress, including after a same-day success, in `DailyOccasionService.cs`; lost fill lease retries fill without duplicating queue entries

### Timer, startup, and `unmetFill` (US7, US6)

- [X] T107 [US7] Change timer NCRONTAB to `0 0 5 * * *` in `src/SoloDevBoard.Functions/DailyCollectionFunctions.cs`
- [X] T108 [US7] On Functions host start in `src/SoloDevBoard.Functions/Program.cs` or a dedicated startup service: run `RepairMissingInitialMessagesAsync`, then call `TryFillDailyOccasionAsync` only when clock is at or after 05:00 UTC and today's occasion marker is absent; do not fill before 05:00; do not use timer `RunOnStartup` to fill overnight per research.md. Repair from `queuedOccasion` is superseded by T153. The fill-when-marker-absent rule stays.
- [X] T109 [US6] Extend `DailySnapshotQueueMessage` and publisher for `kind` `unmetFill` with required `QueueMessage.notBefore` (15 minutes) per data-model.md in `src/SoloDevBoard.Application/Services/Snapshots/` and `src/SoloDevBoard.Infrastructure/Snapshots/AzureDailySnapshotQueuePublisher.cs`
- [X] T111 [US6] Enqueue `unmetFill` after terminal `rateLimit`/`unexpected` with no tries left and after `scheduledCollectionStopped` interruption in `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`; never on `signInOrToken`. Superseded in part by T142: enqueue `unmetFill` only after a finished `rateLimit` or `unexpected` with no tries left. Do not enqueue it after `scheduledCollectionStopped`, a liveness lapse, or redelivery.
- [X] T113 [US7] Handle `unmetFill` in `DailySnapshotCollectionPassService.HandleQueueMessageAsync` or thin branch in `DailyCollectionFunctions.cs`: complete message and call `IDailyOccasionService.TryFillDailyOccasionAsync` without walking GitHub per contracts/daily-collection-schedule.md

### Dequeue run rules (US1)

- [X] T114 [US1] Replace dequeue gating in `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`: use admission-only check at dequeue (not 30-day window); attempt scan when allowed even if `HostedSignInRecord.completedAt` is outside 30 days; remove silent skip on `credentialUnusable` and missing record in favour of `signInOrToken` failed run per contracts/daily-collection-schedule.md
- [X] T115 [US1] Refactor or narrow `HostedCollectionEligibilityEvaluator.IsEligibleAtRun` in `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs` to admission lists only; keep fill-time rules in `IsEligibleAtFill`
- [X] T116 [US1] Stop using `IsEligibleAtRun` for schedule facts in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotScheduleFactsService.cs`; use `WouldBeEligibleForSchedule` at the instant the page names (today at 05:00 before ask, else following day at 05:00). That judgement is data-model.md `OperatorSnapshotStatusDto.NextAskIncludesAccount` in T167, not a rendered clock time. Refusal order stays data-model.md `OperatorSnapshotStatusDto.NoScheduleReason`.

### Schedule facts and copy (US5)

- [X] T117 [US5] Rewrite `TryComputeNextOccasion` and remove 15-minute grace / `completedAt <= 05:00` rules in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotScheduleFactsService.cs` per data-model.md and contracts/repositories-schedule-copy.md
- [X] T118 [US5] Align `src/SoloDevBoard.Web/Components/Features/Repositories/OperatorSnapshotStatusCopy.cs` with updated facts (following UTC day sentence, queued-today row, refusal precedence); confirm no occasion list, run history, or try list is added (FR-018)
- [X] T119 [US5] Update `website/content/docs/repositories.md` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` with T110 in the same change

### Validation

- [X] T112 Re-run quickstart.md section 6 commands after Phase 12 in `specs/002-scheduled-snapshot-collection/quickstart.md`

**Checkpoint**: 2026-11-24 ask and dequeue rules are on the branch. FR-022 and FR-023 are not done until Phases 13–22 complete.

---

## Phase 13: Foundational account scan status (Blocking)

**Purpose**: Container, model, blob adapter, and reported-status reader that every 2026-11-25 story uses.

**⚠️ CRITICAL**: No Phase 14+ work until this phase is complete.

- [X] T120 Amend `adr/adr-0049-scheduled-collection-functions.md` in the same change as the graph edit (addendum or status history; do not add a new ADR file): name container `account-scan-status`, the `web` reference, and timer `0 0 5 * * *`. In `src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs`, add `storage.AddBlobContainer("account-scan-status", "account-scan-status")` in both `ConfigureLocalRun` and `ConfigurePublishInfrastructure`, then `WithReference` from `web` and from the Functions project. Do not add `WaitFor` between `web` and the worker. Do not pass an endpoint URL through `WithEnvironment`. `web` still does not reference `operator-snapshot-queues` or `collection-schedule`. Do not edit `src/SoloDevBoard.AppHost/LeanAppHostGraph.cs` or `.aspire/modules/`. Do not add a NuGet package. Do not unit-test the AppHost graph. Re-assert `web` `MinReplicas` 0 and `MaxReplicas` 1 in `src/SoloDevBoard.AppHost/SoloDevBoardAppHost.cs`.
- [X] T121 [P] Add `AspireHostedStorageReferenceNames.AccountScanStatus` (`account-scan-status`) in `src/SoloDevBoard.Composition/AspireHostedStorageReferenceNames.cs`.
- [X] T122 [P] Add the account scan status model and JSON serializer in `src/SoloDevBoard.Application/Services/Snapshots/` for data-model.md `AccountScanStatus.schemaVersion` (1), `AccountScanStatus.operatorLogin`, `AccountScanStatus.status` (`neverRun`, `scheduled`, `running`, `finished`, `suspended`; `stalled` is not stored), `AccountScanStatus.lastScanAt`, `AccountScanStatus.lastResult` (`success`, `failure`, or null), `AccountScanStatus.resultComment`, `AccountScanStatus.suspensionReason` (`notAllowed` or null), `AccountScanStatus.livenessAt`, `AccountScanStatus.runningSince`, `AccountScanStatus.scanGeneration` (starts at 0), `AccountScanStatus.queueMessagePending`, and `AccountScanStatus.queueVisibleAfter`. The blob must not contain a refresh token.
- [X] T123 [P] Extend `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotStatusDto.cs` with data-model.md `OperatorSnapshotStatusDto.AccountScanStatus`, `OperatorSnapshotStatusDto.ReportedScanStatus`, `OperatorSnapshotStatusDto.LastScanAt`, `OperatorSnapshotStatusDto.LastScanResult`, `OperatorSnapshotStatusDto.ResultComment`, `OperatorSnapshotStatusDto.SuspensionReason` (`notAllowed` or none; from data-model.md `AccountScanStatus.suspensionReason`), and data-model.md `OperatorSnapshotStatusDto.Trigger` (`operator`, `daily`, or none; set when a scan is `running` or has `finished`; none otherwise). Sentences are T167. Do not use `OperatorSnapshotStatusDto.Trigger` to say who started the run. Lean posture leaves the account scan facts empty.
- [X] T124 Add `IAccountScanStatusStore` and an etag blob adapter for container `account-scan-status` in `src/SoloDevBoard.Infrastructure/Snapshots/` per `specs/002-scheduled-snapshot-collection/contracts/account-scan-status.md` (depends on T122).
- [X] T125 Register `AddKeyedAzureBlobContainerClient(AspireHostedStorageReferenceNames.AccountScanStatus)` in `src/SoloDevBoard.Web/Program.cs` and `src/SoloDevBoard.Functions/Program.cs` before `AddSoloDevBoard`, and register the store from that keyed client in `src/SoloDevBoard.Composition/ScheduledCollectionHostExtensions.cs` for both hosts. No connection-string lookup (depends on T121, T124).
- [X] T126 Implement the reported-status reader in `src/SoloDevBoard.Application/Services/Snapshots/AccountScanStatusReporter.cs` using `TimeProvider.GetUtcNow()` per the data-model.md reported status table. Exactly 5 minutes is `running`. Strictly more than 5 minutes is `stalled`. When `AccountScanStatus.livenessAt` is null, use `AccountScanStatus.runningSince`. Do not persist `stalled`. When a hosted sign-in record exists and the status blob is absent, report `never run` and do not write the blob (spec.md FR-022) (depends on T122, T123).

**Checkpoint**: Hosted hosts can read and write the status blob. Callers can be told `stalled` without a stored `stalled` value.

---

## Phase 14: User Story 8 - Read the real scan status at any time (Priority: P1)

**Goal**: The stored record is created when hosted access is stored, the page can show `never run`, and a hosted scan that is still working refreshes `AccountScanStatus.livenessAt` at least every 60 seconds for the generation captured when that scan began.

**Independent Test**: Per spec.md US8 for the record itself: a new hosted account is `never run` with empty last-run time, result, and comment before any queue entry; a later sign-in does not reset a blob that already exists and does not enqueue a scan; a mark exactly 5 minutes old is told `running` and a mark strictly older than 5 minutes is told `stalled` while stored status stays `running` (separate tests); a hosted scan that is still working writes another mark before 60 seconds of fake time elapse. Queue, hand-start, and crash outcomes in that same spec test are completed in Phases 15, 16, and 18.

### Tests for User Story 8

- [X] T127 [P] [US8] Add separate tests `AccountScanStatusReporter_MarkAgeExactlyFiveMinutes_ReportsRunning` and `AccountScanStatusReporter_MarkAgeMoreThanFiveMinutes_ReportsStalled`, plus `AccountScanStatusReporter_MissingMarkUsesRunningSinceOlderThanFiveMinutes_ReportsStalled` and `AccountScanStatusReporter_SignInRecordWithoutStatusBlob_ReportsNeverRunAndDoesNotWrite`, in `tests/SoloDevBoard.Application.Tests/AccountScanStatusReporterTests.cs`. Assert the stored status remains `running` and `stalled` is not written. The missing-blob test asserts callers are told `never run` and the store is not written.
- [X] T128 [P] [US8] Add `AccountScanStatusTransitionService_SignInWhenBlobAbsent_InsertsNeverRun`, `AccountScanStatusTransitionService_SignInWhenBlobPresent_LeavesBlobUnchanged`, and `AccountScanStatusTransitionService_StillWorkingPastSixtySeconds_WritesAnotherLivenessMark` in `tests/SoloDevBoard.Application.Tests/AccountScanStatusTransitionServiceTests.cs`. The absent case matches the `neverRun` example in `specs/002-scheduled-snapshot-collection/contracts/account-scan-status.md`. The cadence test uses `TimeProvider` and asserts another `AccountScanStatus.livenessAt` before 60 seconds elapse, without incrementing `AccountScanStatus.scanGeneration`.

### Implementation for User Story 8

- [X] T129 [US8] Add `AccountScanStatusTransitionService` in `src/SoloDevBoard.Application/Services/Snapshots/AccountScanStatusTransitionService.cs`. On completed hosted sign-in, insert `AccountScanStatus.status` `neverRun` with empty `AccountScanStatus.lastScanAt`, `AccountScanStatus.lastResult`, and `AccountScanStatus.resultComment`, `AccountScanStatus.suspensionReason` null, `AccountScanStatus.scanGeneration` 0, and `AccountScanStatus.queueMessagePending` false when the blob is absent. Do not change an existing blob. Do not call fill. Call that Application port from the completed hosted sign-in path in `src/SoloDevBoard.Web/Authentication/`. `src/SoloDevBoard.Web/Program.cs` only registers the keyed blob client (depends on T124, T125).
- [X] T130 [US8] Add liveness writes on that transition service: while the scan is still working, set `AccountScanStatus.livenessAt` at least once every 60 seconds without incrementing `AccountScanStatus.scanGeneration`. Ignore the write when the stored generation is newer. Clear `AccountScanStatus.livenessAt` and `AccountScanStatus.runningSince` when status is not `running`. Wire the cadence into the hosted pass in `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs` and the hand-started hosted scan in `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotCollectionService.cs` (depends on T129).

**Checkpoint**: A hosted account can be read as `never run`, and a live scan can refresh its own liveness mark.

---

## Phase 15: User Story 1 - Collect overnight without the operator waiting (Priority: P1)

**Goal**: Fill restores an orphan `scheduled` row, then queues eligible people by setting `scheduled` and enqueueing. Dequeue records `running`, success, not-allowed, or unusable stored access on that same blob. New behaviour does not write `SnapshotDocument.queuedOccasion`, `SnapshotDocument.passExecuting`, or `SnapshotDocument.passLeaseUntil`.

**Independent Test**: Per spec.md US1, using the status blob: two eligible signed-out people are `scheduled` at fill and each run finishes as `finished` with result success when the queue is processed; a person who is not allowed is not queued; a reported `stalled` account can be queued; a person told `running` is not queued again.

### Tests for User Story 1

- [X] T131 [P] [US1] Extend `tests/SoloDevBoard.Application.Tests/DailyOccasionServiceTests.cs` with `DailyOccasionService_OrphanScheduledWithNullLastResult_RestoresNeverRunBeforeEligibility`, `DailyOccasionService_OrphanScheduledWithLastResult_RestoresFinishedWithoutFailureComment`, `DailyOccasionService_ReportedStalledAndEligible_QueuesAndSetsScheduled`, `DailyOccasionService_ToldRunning_DoesNotQueue`, and `DailyOccasionService_LostEtag_DoesNotEnqueueOrChangeSnapshot`, and `DailyOccasionService_SignInRecordWithoutStatusBlob_InsertsNeverRunBeforeEligibility`.
- [X] T132 [P] [US1] Extend `tests/SoloDevBoard.Application.Tests/DailySnapshotCollectionPassServiceTests.cs` with `DailySnapshotCollectionPassService_DequeueAllowed_SetsRunningAndLivenessMark`, `DailySnapshotCollectionPassService_NotAllowed_SetsSuspendedWithoutRunningAndClearsQueueMessagePending`, and `DailySnapshotCollectionPassService_StoredAccessUnusable_SetsSuspendedFailureAndCredentialUnusable`.

### Implementation for User Story 1

- [X] T152 [US1] In `src/SoloDevBoard.Application/Services/Snapshots/DailyOccasionService.cs`, before eligibility, if a hosted sign-in record has no `account-scan-status` blob, insert the `neverRun` shape from `specs/002-scheduled-snapshot-collection/contracts/account-scan-status.md`: data-model.md `AccountScanStatus.status` `neverRun`, empty `AccountScanStatus.lastScanAt`, `AccountScanStatus.lastResult`, and `AccountScanStatus.resultComment`, `AccountScanStatus.suspensionReason` null, `AccountScanStatus.scanGeneration` 0, and `AccountScanStatus.queueMessagePending` false. Do not reset a blob that already exists. Do not enqueue because of that insert alone (depends on T129).
- [X] T133 [US1] In `src/SoloDevBoard.Application/Services/Snapshots/DailyOccasionService.cs`, before eligibility, restore each account whose `AccountScanStatus.status` is `scheduled` and whose `AccountScanStatus.queueMessagePending` is false: null `AccountScanStatus.lastResult` becomes `neverRun`; any set `AccountScanStatus.lastResult` becomes `finished`. Do not change `AccountScanStatus.lastScanAt`, `AccountScanStatus.lastResult`, or `AccountScanStatus.resultComment` on that restore, and do not add a failure comment. `scheduled` is already queued. Told `running` is in progress. A reported `stalled` account is neither. The winning write sets `scheduled`, clears `AccountScanStatus.suspensionReason`, sets `AccountScanStatus.queueVisibleAfter` null for that `initial`, increments `AccountScanStatus.scanGeneration`, then enqueues. Set `AccountScanStatus.queueMessagePending` true only after the queue accepts the message. If enqueue fails, leave that flag false. A lost etag does not enqueue and does not change the snapshot. Do not write `SnapshotDocument.queuedOccasion` for this path. This replaces T024 (depends on T152).
- [X] T134 [US1] In `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`, when the worker takes an entry for an account that is still allowed, set `AccountScanStatus.status` `running` and set `AccountScanStatus.livenessAt` and `AccountScanStatus.runningSince` to now in that same update. Do not set `running` on a not-allowed pickup. Not allowed, scan not started: one write to `suspended`, set data-model.md `AccountScanStatus.suspensionReason` to `notAllowed`, set `AccountScanStatus.queueMessagePending` false, and leave `AccountScanStatus.lastScanAt`, `AccountScanStatus.lastResult`, `AccountScanStatus.resultComment`, and the successful snapshot unchanged. Stored access cannot be used, token exchange failed before the catalogue walk, and no newer worker token is already stored: `suspended`, `AccountScanStatus.lastResult` `failure`, sign-in or token `AccountScanStatus.resultComment`, `AccountScanStatus.suspensionReason` null, set `AccountScanStatus.lastScanAt`, clear `AccountScanStatus.queueMessagePending`, and set data-model.md `HostedSignInRecord.credentialUnusable`, with no further try and no `unmetFill`. When a newer worker token is already stored, T162 supersedes this outcome: data-model.md `AccountScanStatus.status` `finished`, the same failure comment, `AccountScanStatus.suspensionReason` empty, and `HostedSignInRecord.credentialUnusable` stays false. Taking the entry for an allowed account clears `AccountScanStatus.suspensionReason`. Success: `finished`, `AccountScanStatus.lastResult` `success`, `AccountScanStatus.resultComment` `Success`, set `AccountScanStatus.lastScanAt`, and clear the mark and `AccountScanStatus.queueMessagePending`. Do not re-check the 30-day window at dequeue (depends on T130, T133).

**Checkpoint**: Eligible hosted accounts are queued and collected through the status blob. User Story 1 can be validated without the hand-start race.

---

## Phase 16: User Story 2 - Keep starting a collection by hand (Priority: P1)

**Goal**: A hand-started hosted collection updates the same status blob. It does not use the queue. **Collect snapshot** stays available when callers are told `stalled`.

**Independent Test**: Per spec.md US2, with status facts: hand start is refused while callers are told `scheduled` or `running`; hand start while told `stalled` sets `running` and writes a liveness mark; a hand-started success replaces the snapshot and does not get three further tries.

### Tests for User Story 2

- [X] T135 [P] [US2] Extend `tests/SoloDevBoard.Application.Tests/OperatorSnapshotCollectionServiceTests.cs` with `OperatorSnapshotCollectionService_StartWhileScheduledOrRunning_DoesNotStart`, `OperatorSnapshotCollectionService_StartWhileStalled_SetsRunningAndLivenessMark`, and `OperatorSnapshotCollectionService_HandStartedScanPastSixtySeconds_WritesAnotherLivenessMark`.

### Implementation for User Story 2

- [X] T136 [US2] In `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotCollectionService.cs`, a winning hand start sets `AccountScanStatus.status` `running`, clears `AccountScanStatus.suspensionReason`, increments `AccountScanStatus.scanGeneration`, writes a new liveness mark, and leaves `AccountScanStatus.queueMessagePending` false. This replaces T032 and T033. Finish sets `finished` or `suspended` on that same record. Refuse `StartAsync` while callers are told `scheduled` or `running`. Allow it while they are told `stalled`, `finished`, `suspended`, or `never run`, subject to permission to use the product. Do not enqueue. In `src/SoloDevBoard.Web/Components/Features/Repositories/Pages/Repositories.razor`, keep **Collect snapshot** disabled only while callers are told `scheduled` or `running` (depends on T129, T130).

**Checkpoint**: Hand-started hosted collection and the daily pass share one status record.

---

## Phase 17: User Story 4 - Run one collection at a time (Priority: P1)

**Goal**: When a fill and a hand start both meet a reported `stalled` account, one conditional write wins and one scan starts.

**Independent Test**: Per spec.md US4 and quickstart.md section 4: two eligible people still run one at a time; a stalled race starts exactly one scan; the loser does not enqueue and does not change the snapshot.

### Tests for User Story 4

- [X] T137 [P] [US4] Add `OperatorSnapshotCollectionService_HandStartAndFillRaceOnStalled_ExactlyOneScanStarts` in `tests/SoloDevBoard.Application.Tests/OperatorSnapshotCollectionServiceTests.cs`, using the fill path from `src/SoloDevBoard.Application/Services/Snapshots/DailyOccasionService.cs`.

### Implementation for User Story 4

- [X] T138 [US4] In `src/SoloDevBoard.Application/Services/Snapshots/AccountScanStatusTransitionService.cs`, make the first etag that sets `scheduled` or a new `running` the only winner. Every other attempt must not start a second scan, must not add a second queue entry, and must not change the stored status, the liveness mark, or the snapshot. After a newer `AccountScanStatus.scanGeneration` has won, the earlier pass must not change the status, the mark, or the snapshot. Until that newer scan wins, the earlier pass may still record its real success or failure (depends on T133, T136).

**Checkpoint**: One new scan wins when a fill and a hand start race on a stalled account.

---

## Phase 18: User Story 3 - Leave the operator's own machine unscheduled (Priority: P1)

**Goal**: Lean posture still has no schedule, no status blob, and no worker status words.

**Independent Test**: Per spec.md US3 and quickstart.md section 1: no fill at 05:00 UTC, the two lean sentences, no `scheduled`, `running`, `stalled`, or `suspended` from a worker, and **Collect snapshot** still works.

### Tests for User Story 3

- [X] T139 [US3] Extend lean cases in `tests/SoloDevBoard.Application.Tests/OperatorSnapshotScheduleFactsServiceTests.cs` and `tests/SoloDevBoard.Web.Tests/RepositoriesTests.cs` so a lean posture writes no account scan status, does not fill at 05:00 UTC, and does not show worker status words.

### Implementation for User Story 3

- [X] T140 [US3] Keep status blob writes and the reporter off the lean posture in `src/SoloDevBoard.Composition/` and `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotScheduleFactsService.cs`. Lean facts stay data-model.md `OperatorSnapshotStatusDto.SchedulePosture` `lean` and `OperatorSnapshotStatusDto.NoScheduleReason` `lean`, with no account scan status and no rendered clock time (depends on T125, T126). T172 keeps data-model.md `OperatorSnapshotStatusDto.WorkerTokenRefreshOffered` false here.

**Checkpoint**: Lean local behaviour is unchanged apart from the absence of the new status record.

---

## Phase 19: User Story 6 - Keep the last success when the overnight run does not finish cleanly (Priority: P2)

**Goal**: Further tries wait as `scheduled`. A liveness lapse or a dead worker does not write `finished`, does not enqueue `unmetFill`, and does not write `scheduledCollectionStopped` or `siteStopped`. Exhausted rate-limit or unexpected error still finishes, then asks again with `unmetFill`.

**Independent Test**: Per spec.md US6 and quickstart.md section 4: a waiting retry is `scheduled`; rate-limit and unexpected waits stay 2 minutes (or the GitHub delay) and 3, 6, and 12 minutes; worker death while `running` completes the message and leaves stored status `running`; after more than 5 minutes callers are told `stalled`; `web` stopping does not rewrite that blob to `siteStopped`.

### Tests for User Story 6

- [X] T141 [US6] Extend `tests/SoloDevBoard.Application.Tests/DailySnapshotCollectionPassServiceTests.cs` with `DailySnapshotCollectionPassService_RetryWaiting_SetsScheduledNotRunning`, `DailySnapshotCollectionPassService_AttemptsExhausted_SetsFinishedThenUnmetFill`, `DailySnapshotCollectionPassService_WorkerRedeliveryWhileRunning_CompletesMessageAndLeavesRunning`, `DailySnapshotCollectionPassService_LivenessLapse_DoesNotEnqueueUnmetFill`, `DailySnapshotCollectionPassService_EarlierGenerationAfterNewerScan_DoesNotOverwriteStatusMarkOrSnapshot`, and `DailySnapshotCollectionPassService_WebStopDuringRunningPass_DoesNotWriteSiteStopped`.

### Implementation for User Story 6

- [X] T142 [US6] In `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`, a further try sets `AccountScanStatus.status` `scheduled`, sets `AccountScanStatus.queueMessagePending` true, and sets `AccountScanStatus.queueVisibleAfter` from data-model.md `QueueMessage.notBefore`. Leave `AccountScanStatus.lastResult` unchanged until the try sequence ends. Restore the previous terminal snapshot run from data-model.md `SnapshotDocument.retainedRun` so the wait is not shown as a failure, and keep `SnapshotDocument.attemptsUsed` until the try sequence ends or the try is removed because they are not allowed. Do not present that wait as `running` or `finished`. When no tries remain, set `finished`, `AccountScanStatus.lastResult` `failure`, the failure `AccountScanStatus.resultComment`, and `AccountScanStatus.lastScanAt`, clear `AccountScanStatus.queueMessagePending`, then enqueue one `unmetFill` hidden for 15 minutes. Do not enqueue `unmetFill` because a liveness mark lapsed. This replaces T052 and the Phase 8 path that wrote `scheduledCollectionStopped` on redelivery. `unmetFill` follows only that finished technical failure (depends on T134).
- [X] T154 [US6] While a hosted pass is stored as `running` for the `AccountScanStatus.scanGeneration` that started it, renew Azure queue visibility about every 10 seconds (visibility about 30 seconds) from `src/SoloDevBoard.Functions/DailyCollectionFunctions.cs` so a live pass is not redelivered. Do not key that renewal on `passExecuting` or `passLeaseUntil`. Stop renewing when the pass records `finished` or `suspended`, or when a newer generation has won. This replaces T075 (depends on T134).
- [X] T143 [US6] On redelivery after the worker died while status is `running`: complete the message, leave `AccountScanStatus.status` `running`, and do not write `finished`, `interrupted`, `scheduledCollectionStopped`, or `siteStopped`, and do not clear data-model.md `SnapshotDocument.retainedRun`. Clear `AccountScanStatus.queueMessagePending` for that same generation only. Do not change `AccountScanStatus.status`, `AccountScanStatus.lastResult`, or the snapshot in that clear. A stop while status is only `scheduled` leaves it `scheduled`. In `src/SoloDevBoard.Infrastructure/Snapshots/BlobOperatorSnapshotStore.cs`, a hosted pass must not be rewritten to `siteStopped` because `web` has no live task. Do not write `SnapshotDocument.passExecuting` or `SnapshotDocument.passLeaseUntil` for this behaviour (depends on T142).

**Checkpoint**: Failure, retry, and worker death match FR-022 and FR-023. The earlier success stays in place.

---

## Phase 20: User Story 7 - Run the fill whenever it is asked, and follow recent sign-ins (Priority: P2)

**Goal**: Sign-in does not start a scan. A later fill includes a reported `stalled` account and a person whose scan already succeeded, using the same eligibility rules at any UTC time.

**Independent Test**: Per spec.md US7: completing sign-in does not enqueue; a fill after success queues an idle eligible person again; a reported `stalled` account is queued when the other rules allow. An account with `HostedSignInRecord.credentialUnusable` set stays out of the queue while that flag is set. The flag clears when a worker token is kept, including a refresh while the operator stays signed in. A completed sign-in that stores no worker token leaves the flag set.

### Tests for User Story 7

- [X] T144 [P] [US7] Extend `tests/SoloDevBoard.Application.Tests/DailyOccasionServiceTests.cs` and `tests/SoloDevBoard.Application.Tests/HostedCollectionEligibilityEvaluatorTests.cs` so a completed sign-in does not enqueue, a second fill after success queues an idle eligible person, a reported `stalled` account is eligible, `HostedSignInRecord.credentialUnusable` is not queued, and `DailyOccasionService_HostStartRepair_ReenqueuesMissingInitialOnlyWhenDue` covers T153.

### Implementation for User Story 7

- [X] T145 [US7] In `src/SoloDevBoard.Application/Services/Snapshots/HostedCollectionEligibilityEvaluator.cs`, treat told `scheduled` as already queued and told `running` as in progress. Treat a reported `stalled` account as neither. Keep the 30-day comparison on `HostedSignInRecord.completedAt` at the fill instant only. Do not call fill from the sign-in path in `src/SoloDevBoard.Web/Authentication/` (depends on T126, T133).
- [X] T153 [US7] Replace the `queuedOccasion` repair from T060 and T108. On Functions host start, re-enqueue a missing `initial` only when data-model.md `AccountScanStatus.status` is `scheduled`, `AccountScanStatus.queueMessagePending` is true, `AccountScanStatus.queueVisibleAfter` is null or already passed, and a peek finds no visible message for that login. Do not re-enqueue when `AccountScanStatus.queueVisibleAfter` is still ahead. Do not add a second message for someone already queued or told `running`. Do not read `queuedOccasion` (depends on T133).

**Checkpoint**: Later fills follow the status record and recent sign-ins without starting a scan at sign-in.

---

## Phase 21: User Story 5 - See when the next scheduled queue fill is (Priority: P2)

**Goal**: `/repositories` shows the reported status word and one schedule sentence from the status facts. It does not infer the scan from the clock, from a missing visit, or from whether the worker seems absent.

**Independent Test**: Per spec.md US5 and quickstart.md section 5, including `never run`, `scheduled`, `running`, retry waiting, `stalled`, `suspended`, and the refusal order `notAllowed`, then `credentialUnusable`, then `outsideWindow`.

### Tests for User Story 5

- [X] T146 [US5] Extend `tests/SoloDevBoard.Web.Tests/RepositoriesTests.cs` for the rows in `specs/002-scheduled-snapshot-collection/contracts/repositories-schedule-copy.md`: `Repositories_HostedNeverRun_ShowsNeverRunWithoutLastRunFacts`, `Repositories_HostedScheduled_ShowsScheduledNotRunning`, `Repositories_HostedRunning_ShowsWhoStartedTheRun`, `Repositories_HostedStalled_ShowsStalledAndCollectSnapshot`, and `Repositories_HostedNotAllowed_UsesNotAllowedSentenceOnly`. Keep `Repositories_InitialRender_DoesNotStartCollection`. Also cover a waiting retry as `scheduled` with no try list, and data-model.md `OperatorSnapshotStatusDto.NoScheduleReason` order `notAllowed`, then `credentialUnusable`, then `outsideWindow`. Sentence text and the refresh control are T166. The running copy must not say who started the run.

### Implementation for User Story 5

- [X] T147 [US5] Read `OperatorSnapshotStatusDto.ReportedScanStatus` in `src/SoloDevBoard.Web/Components/Features/Repositories/OperatorSnapshotStatusCopy.cs` and `src/SoloDevBoard.Web/Components/Features/Repositories/Pages/Repositories.razor`. Use the status words and the single schedule sentence in `specs/002-scheduled-snapshot-collection/contracts/repositories-schedule-copy.md`, including data-model.md `OperatorSnapshotStatusDto.NoScheduleReason` and data-model.md `OperatorSnapshotStatusDto.SuspensionReason`. Superseded for new behaviour by T168: render data-model.md `OperatorSnapshotStatusDto.StatusSentence` and data-model.md `OperatorSnapshotStatusDto.ScheduleSentence` from Application. Do not format 05:00 UTC in the page. Map `notAllowed` to the fixed comment. Do not read that comment from `OperatorSnapshotStatusDto.ResultComment`. Do not list tries, occasions, or the 3, 6, or 12 minute waits. Do not add a settings or retry control. UK English. No em dash (depends on T126, T136).
- [X] T148 [US5] Update `website/content/docs/repositories.md` and the locking row in `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` in the same change as T146 and T147. Describe shipped status words and schedule sentences only. Do not add a Playwright test that waits for 05:00 UTC or collects against GitHub.

**Checkpoint**: The Repositories page tells the truth from the stored record.

---

## Phase 22: Polish and cross-cutting validation

**Purpose**: Store safety, operator docs that name the container, and the pull-request commands.

- [X] T149 [P] Add `BlobAccountScanStatusStore_LostEtag_DoesNotOverwrite` and `BlobAccountScanStatusStore_SerialisedBlob_ContainsNoRefreshToken` in `tests/SoloDevBoard.Infrastructure.Tests/`.
- [X] T150 [P] Name container `account-scan-status` beside the existing hosted containers in `docs/getting-started.md`, `docs/deployment.md`, and `src/SoloDevBoard.AppHost/README.md`. UK English. No em dash.
- [X] T151 Run the commands in `specs/002-scheduled-snapshot-collection/quickstart.md` section 6. Review `HostedSignInAppHostGraph` and `LeanAppHostGraph` against `specs/002-scheduled-snapshot-collection/research.md` section "Account scan status (2026-11-25)". Do not unit-test the AppHost graph.

**Checkpoint**: FR-022 and FR-023 are covered by tests, and the hosted graph review matches research.md.

---

## Phase 23: Foundational - Worker credential split (blocking)

**Purpose**: Schema 2 sign-in blob and the four OAuth purposes. Every later story in this delta depends on this phase.

**⚠️ CRITICAL**: No Phase 24+ work until this phase is complete.

- [X] T155 [P] Amend `adr/adr-0015-github-app-first-hosted-authentication.md` with an addendum or status history: overnight collection uses a second user authorisation on the existing GitHub App, and the auth-cookie refresh token is not the worker token. Update the summary row in `adr/README.md` when that summary changes. Do not add a new ADR file. Do not edit `src/SoloDevBoard.AppHost/LeanAppHostGraph.cs` or `.aspire/modules/`. Leave `adr/adr-0049-scheduled-collection-functions.md` as the worker and `account-scan-status` binding record.

### Tests for the credential split

- [X] T156 [P] Add `HostedSignInRecord_Schema1RefreshToken_IsNotCopiedOrExchanged` in `tests/SoloDevBoard.Infrastructure.Tests/` (or `tests/SoloDevBoard.Application.Tests/` if the reader lives there). A schema 1 blob with `refreshToken` set leaves the worker token absent. The exchange port is not called with that value. quickstart.md section 6.
- [X] T157 [P] Add Application tests in `tests/SoloDevBoard.Application.Tests/HostedCredentialGrantServiceTests.cs` for the four purposes in `specs/002-scheduled-snapshot-collection/research.md`: `HostedCredentialGrantService_Browser_StoresWindowWithoutClearingWait`, `HostedCredentialGrantService_WorkerDuringSignIn_AcceptedBrowserToken_StoresWorkerToken`, `HostedCredentialGrantService_WorkerDuringSignIn_RejectedBrowserToken_DiscardsCandidateAndRestoresCookieOnly`, `HostedCredentialGrantService_WorkerRefresh_StoresWorkerTokenWithoutMovingWindow`, and `HostedCredentialGrantService_Cancel_LeavesSignInAndRecordsNoScanFailure`. Use a fake token exchange. No live GitHub call.

### Implementation for the credential split

- [X] T158 Move the sign-in document to schema 2 in `src/SoloDevBoard.Application/Authentication/HostedSignInRecord.cs`, `src/SoloDevBoard.Infrastructure/Identity/HostedSignInRecordJson.cs`, and `src/SoloDevBoard.Infrastructure/Identity/BlobHostedSignInRecordStore.cs`. On write, data-model.md `HostedSignInRecord.schemaVersion` is 2. Keep data-model.md `HostedSignInRecord.operatorLogin`. Read a stored schema 1 `refreshToken` as worker token absent: do not copy it and do not exchange it. data-model.md `HostedSignInRecord.workerRefreshToken` and data-model.md `HostedSignInRecord.workerRefreshTokenExpiresAt` stay absent until a second authorisation is kept. The browser refresh token is not a field on this blob (depends on T156).
- [X] T159 On the first GitHub authorisation, write the auth cookie and data-model.md `HostedSignInRecord.completedAt` and data-model.md `HostedSignInRecord.organisationLogins` from `src/SoloDevBoard.Web/Authentication/HostedSignInPersistence.cs`. Do not write data-model.md `HostedSignInRecord.workerRefreshToken`. Do not clear data-model.md `HostedSignInRecord.credentialUnusable`. If the account scan status blob is absent, create data-model.md `AccountScanStatus.status` `neverRun`. Do not reset a status blob that already exists. Sign-out still does not delete the sign-in blob (depends on T158).
- [X] T160 Add `src/SoloDevBoard.Application/Authentication/HostedCredentialGrantService.cs` and call it from `src/SoloDevBoard.Web/Program.cs` and `src/SoloDevBoard.Web/Authentication/HostedGitHubAuthGateway.cs`. Split the existing callback by OAuth state purpose: `browser`, `worker-during-sign-in`, `browser-restore`, and `worker-refresh`. One GitHub response is stored as only one token. `browser` completes sign-in through T159, then redirects to `worker-during-sign-in`. Keep data-model.md `HostedSignInRecord.workerRefreshToken` only when the browser access token already in the session is still accepted. That write clears data-model.md `HostedSignInRecord.credentialUnusable` and does not change data-model.md `HostedSignInRecord.completedAt` or data-model.md `HostedSignInRecord.organisationLogins` when this is the second leg of a sign-in that already wrote them. If status is `suspended` for the token failure, set data-model.md `AccountScanStatus.status` to `finished`, keep `AccountScanStatus.lastResult` and `AccountScanStatus.resultComment`, and leave `AccountScanStatus.suspensionReason` empty. If the browser access token is rejected, discard the candidate, do not replace a stored worker token, and run `browser-restore`, which replaces the cookie only. That restore does not store a worker token, does not change `completedAt` or `organisationLogins`, does not lift the wait, and does not record a scan failure. `worker-refresh` stores only the worker token, does not change the cookie, `completedAt`, or `organisationLogins`, and does not start a scan. Cancel or failure of `worker-during-sign-in` or `worker-refresh` leaves the operator signed in and does not record a scan failure. Do not create the worker token by exchanging the cookie token (depends on T157, T159).

**Checkpoint**: Schema 1 is not exchanged. Sign-in can complete without a worker token. A kept second authorisation stores only that token.

---

## Phase 24: User Story 1 - Collect overnight without the operator waiting (Priority: P1)

**Goal**: The daily pass exchanges only the worker refresh token. A token failure with no newer worker token sets the stored-access wait. A newer worker token already stored finishes the failure without that wait.

**Independent Test**: Per spec.md US1, FR-013, FR-024, and quickstart.md sections 3 and 6: schema 1 is not exchanged; a queued scan uses the token stored when it is taken; a running scan does not overwrite a newer worker token; the two token-failure outcomes are separate.

### Tests for User Story 1

- [X] T161 [P] [US1] Extend `tests/SoloDevBoard.Application.Tests/DailySnapshotCollectionPassServiceTests.cs` with `DailySnapshotCollectionPassService_MissingWorkerToken_SetsSuspendedWithoutCookieExchange`, `DailySnapshotCollectionPassService_QueuedScan_UsesWorkerTokenStoredWhenTaken`, `DailySnapshotCollectionPassService_RunningScan_DoesNotOverwriteNewerWorkerToken`, `DailySnapshotCollectionPassService_TokenFailureWithNewerToken_SetsFinishedWithoutWait`, and `DailySnapshotCollectionPassService_TokenFailureWithoutNewerToken_SetsSuspendedAndWait`.

### Implementation for User Story 1

- [X] T162 [US1] In `src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs`, exchange only data-model.md `HostedSignInRecord.workerRefreshToken`. A missing or unusable token fails with the sign-in or token reason before the catalogue walk and does not exchange the auth-cookie refresh token. A queued scan that has not started reads the token again when it is taken. A scan already running keeps the token it started with. Persist a rotated data-model.md `HostedSignInRecord.workerRefreshToken` and data-model.md `HostedSignInRecord.workerRefreshTokenExpiresAt` only while the stored value is still that captured token (etag). A newer stored token stays. When the captured token cannot be used and a newer worker token is already stored, set data-model.md `AccountScanStatus.status` `finished`, `AccountScanStatus.lastResult` `failure`, the sign-in or token `AccountScanStatus.resultComment`, and `AccountScanStatus.suspensionReason` empty, and do not set data-model.md `HostedSignInRecord.credentialUnusable`. When no newer token is stored, set `suspended`, that failure, `suspensionReason` empty, and `credentialUnusable` true. No further try and no `unmetFill` in either case. This replaces the token outcome in T134 for the newer-token case (depends on T158, T161).

**Checkpoint**: An overnight scan renews only the worker token. The browser session is not spent.

---

## Phase 25: User Story 2 - Keep starting a collection by hand (Priority: P1)

**Goal**: A hand-started hosted collection uses the signed-in session. A missing worker token does not refuse it.

**Independent Test**: Per spec.md US2 and quickstart.md section 6: `StartAsync` with no worker token uses the session access token and can finish, unless callers are told `scheduled` or `running`.

### Tests for User Story 2

- [X] T163 [P] [US2] Add `OperatorSnapshotCollectionService_StartAsync_MissingWorkerToken_UsesSessionAccessToken` in `tests/SoloDevBoard.Application.Tests/OperatorSnapshotCollectionServiceTests.cs`. Assert the worker token is not read, and that `scheduled` or told `running` still refuses a second walk.

### Implementation for User Story 2

- [X] T164 [P] [US2] In `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotCollectionService.cs`, obtain GitHub access for a hand-started hosted collection from the signed-in session access token. Do not read data-model.md `HostedSignInRecord.workerRefreshToken`. A missing or unusable worker token does not refuse `StartAsync` unless callers are told `scheduled` or `running` (depends on T160, T163).

**Checkpoint**: Hand collection still runs when the overnight credential is missing.

---

## Phase 26: User Story 5 - See when the next scheduled queue fill is (Priority: P2)

**Goal**: Callers receive one FR-025 status sentence and at most one schedule sentence. The Repositories page renders those strings and offers **Refresh overnight access** only when the worker token needs storing.

**Independent Test**: Per spec.md US5, FR-025, SC-009, and quickstart.md section 5: the sentences match `specs/002-scheduled-snapshot-collection/contracts/repositories-schedule-copy.md`; none name 05:00 UTC or who started the run; the refresh control is on `/repositories` only.

### Tests for User Story 5

- [X] T165 [P] [US5] Add `tests/SoloDevBoard.Application.Tests/AccountScanStatusSentenceFormatterTests.cs` for every status sentence and the one schedule sentence in `specs/002-scheduled-snapshot-collection/contracts/repositories-schedule-copy.md`. Assert none contain "05:00 UTC", "daily occasion", "today", "tonight", or "the following day", and none say who started the run. Cover data-model.md `OperatorSnapshotStatusDto.NextAskIncludesAccount` (true when the next 05:00 UTC ask would queue them; not a timestamp), data-model.md `OperatorSnapshotStatusDto.NoScheduleReason` order `notAllowed`, then `credentialUnusable`, then `outsideWindow`, and data-model.md `OperatorSnapshotStatusDto.WorkerTokenRefreshOffered`. When a successful snapshot exists, the status sentence ends with the last-success clause. When none exists, that clause is omitted. A newer worker token already stored uses the finished failure sentence, not the paused token sentence.
- [X] T166 [P] [US5] Extend `tests/SoloDevBoard.Web.Tests/RepositoriesTests.cs` for quickstart.md section 5. Replace the expectation in `Repositories_HostedRunning_ShowsWhoStartedTheRun` so the running sentence does not say who started the run, and rename that method so its name matches the FR-025 running sentence. Cover **Refresh overnight access** when the worker token is missing and the next ask will queue them, and when the schedule sentence is the refresh sentence and the operator is signed in. That control is not a retry and does not start a collection. Keep `Repositories_InitialRender_DoesNotStartCollection`.

### Implementation for User Story 5

- [X] T167 [US5] Add `src/SoloDevBoard.Application/Services/Snapshots/AccountScanStatusSentenceFormatter.cs` and call it from `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotScheduleFactsService.cs`. Set data-model.md `OperatorSnapshotStatusDto.StatusSentence` and data-model.md `OperatorSnapshotStatusDto.ScheduleSentence` from the reported status and the schedule facts. Do not read data-model.md `SnapshotDocument.trigger` or data-model.md `OperatorSnapshotStatusDto.Trigger` to build either sentence. Set data-model.md `OperatorSnapshotStatusDto.NextAskIncludesAccount` from the next 05:00 UTC ask. It is not rendered as a clock time. Set data-model.md `OperatorSnapshotStatusDto.WorkerTokenRefreshOffered` true in hosted sign-in when data-model.md `HostedSignInRecord.workerRefreshToken` is absent or data-model.md `HostedSignInRecord.credentialUnusable` is set, and false in lean posture. Remove any clock field that is not in the status facts table (depends on T165).
- [X] T168 [US5] Render data-model.md `OperatorSnapshotStatusDto.StatusSentence` and data-model.md `OperatorSnapshotStatusDto.ScheduleSentence` in `src/SoloDevBoard.Web/Components/Features/Repositories/Pages/Repositories.razor`. Stop assembling those sentences in `src/SoloDevBoard.Web/Components/Features/Repositories/OperatorSnapshotStatusCopy.cs`. Show **Refresh overnight access** in the existing status `MudAlert` only when the operator is signed in and `WorkerTokenRefreshOffered` is true. It starts the `worker-refresh` purpose from T160, does not start a scan, and is not a settings or retry control. When the schedule sentence is already the refresh sentence, do not repeat that sentence beside the control. UK English. No em dash. No second alert and no snackbar for the same outcome (depends on T160, T166, T167).
- [X] T169 [US5] Update `website/content/docs/repositories.md` and the locking row in `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` in the same change as T168. Describe the FR-025 sentences and **Refresh overnight access**. Update the overview screenshot only when the status region's visible text would make the published image wrong. Do not add a Playwright test that waits for 05:00 UTC or collects against GitHub (depends on T168).

**Checkpoint**: Hosted callers receive the same sentences. The page does not invent the wording.

---

## Phase 27: User Story 3 - Leave the operator's own machine unscheduled (Priority: P1)

**Goal**: Lean posture has no worker token, no FR-025 catalogue, and no **Refresh overnight access**.

**Independent Test**: Per spec.md US3 and quickstart.md section 1: no fill at 05:00 UTC, the two lean sentences, no worker status words, and no refresh control. **Collect snapshot** still works.

### Tests for User Story 3

- [X] T170 [US3] Extend lean cases in `tests/SoloDevBoard.Application.Tests/OperatorSnapshotScheduleFactsServiceTests.cs` so data-model.md `OperatorSnapshotStatusDto.SchedulePosture` is `lean`, data-model.md `OperatorSnapshotStatusDto.NoScheduleReason` is `lean`, data-model.md `OperatorSnapshotStatusDto.StatusSentence` and data-model.md `OperatorSnapshotStatusDto.ScheduleSentence` are none, and data-model.md `OperatorSnapshotStatusDto.WorkerTokenRefreshOffered` is false. No fill at 05:00 UTC (depends on T165).
- [X] T171 [US3] Extend lean cases in `tests/SoloDevBoard.Web.Tests/RepositoriesTests.cs` so **Refresh overnight access** is absent and the alert keeps the two lean sentences from `specs/002-scheduled-snapshot-collection/contracts/repositories-schedule-copy.md` (depends on T166).

### Implementation for User Story 3

- [X] T172 [US3] Keep worker-token writes, the refresh control, and the FR-025 catalogue off the lean posture in `src/SoloDevBoard.Composition/` and `src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotScheduleFactsService.cs`. Do not write a sign-in blob or a worker token in lean posture (depends on T167, T170, T171).

**Checkpoint**: PAT-only local use still has no schedule and no worker-token ask.

---

## Phase 28: Polish and cross-cutting validation

**Purpose**: Confirm secrets stay out of scan artefacts, then run the pull-request commands.

- [X] T173 [P] Extend `tests/SoloDevBoard.Infrastructure.Tests/` so a serialised account scan status blob, a queue message, and a snapshot document contain neither the auth-cookie refresh token nor data-model.md `HostedSignInRecord.workerRefreshToken`. Build on `BlobAccountScanStatusStore_SerialisedBlob_ContainsNoRefreshToken` if that test is still present.
- [X] T174 Run the commands in `specs/002-scheduled-snapshot-collection/quickstart.md` section 7. Confirm `src/SoloDevBoard.AppHost/LeanAppHostGraph.cs` is unchanged. Do not unit-test the AppHost graph.

**Checkpoint**: FR-024 and FR-025 are covered by tests. The lean graph is untouched.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phases 1–22**: Delivered on this branch. Do not redo them.
- **Phase 23 (worker credential split)**: Depends on that baseline. Blocks Phases 24–28.
- **Phase 24 (US1)**: After Phase 23. T162 edits `DailySnapshotCollectionPassService.cs` after T134, T142, and T143.
- **Phase 25 (US2)**: After Phase 23. T164 edits `OperatorSnapshotCollectionService.cs` and can run beside T162.
- **Phase 26 (US5)**: After Phase 23. T168 also needs the `worker-refresh` purpose from T160. T166 edits `RepositoriesTests.cs` before T171.
- **Phase 27 (US3)**: After T165 and T166, because those tests share files with T170 and T171. Implementation T172 follows T167.
- **Phase 28**: After the stories you intend to ship. T174 is the last check.

### User Story Dependencies (Phases 23–28)

- **Credential split (Phase 23)**: No story label. T156 and T157 fail before T158 and T160. T159 follows T158. T155 is a different file and can run beside the tests.
- **US1 (Phase 24)**: After T158. The pass reads the worker token and does not exchange the cookie token.
- **US2 (Phase 25)**: After T160 so the session token and the worker token are already different grants. Independently testable with a missing worker token.
- **US5 (Phase 26)**: After Phase 23. The formatter does not depend on the pass rewrite, so T165 and T167 can start once schema 2 exists. The refresh control waits for T160.
- **US3 (Phase 27)**: After the formatter tests and the hosted bUnit file edit. Lean behaviour stays independently testable: no schedule, no refresh control.

### Parallel Opportunities (Phases 23–28)

- T155, T156, and T157 can run together. They do not share files.
- T161 and T163 can run together after Phase 23. They are different test files.
- T162 and T164 can run together. They edit different services.
- T165 and T166 can run together. They are different test projects.
- T170 waits for T165. Both edit `OperatorSnapshotScheduleFactsServiceTests.cs`.
- T171 waits for T166. Both edit `RepositoriesTests.cs`.
- T173 can run beside the page work. It does not share those files.
- Do not run T162 beside an edit of `DailySnapshotCollectionPassService.cs`.

---

## Parallel Example: Phase 23

```bash
adr/adr-0015-github-app-first-hosted-authentication.md                         # T155
tests/SoloDevBoard.Infrastructure.Tests/                                       # T156
tests/SoloDevBoard.Application.Tests/HostedCredentialGrantServiceTests.cs      # T157
```

---

## Parallel Example: User Story 1 and User Story 2

```bash
tests/SoloDevBoard.Application.Tests/DailySnapshotCollectionPassServiceTests.cs          # T161
tests/SoloDevBoard.Application.Tests/OperatorSnapshotCollectionServiceTests.cs           # T163
src/SoloDevBoard.Application/Services/Snapshots/DailySnapshotCollectionPassService.cs    # T162
src/SoloDevBoard.Application/Services/Snapshots/OperatorSnapshotCollectionService.cs     # T164
```

---

## Parallel Example: User Story 5

```bash
tests/SoloDevBoard.Application.Tests/AccountScanStatusSentenceFormatterTests.cs   # T165
tests/SoloDevBoard.Web.Tests/RepositoriesTests.cs                                 # T166
```

---

## Implementation Strategy

### MVP First (worker credential)

1. Phases 1–22 are already on the branch.
2. Complete Phase 23 (T155–T160).
3. **STOP and VALIDATE**: schema 1 is not exchanged; the first authorisation completes sign-in without a worker token; a kept second authorisation stores only that token.
4. Complete Phase 24 (T161, T162) so an overnight scan uses that token.
5. **STOP and VALIDATE**: quickstart.md section 6 rows for a queued token, a newer token left in place, and the two failure outcomes.

### Incremental Delivery

1. Add US2 (T163, T164) so a hand start still uses the session token.
2. Add US5 (T165–T169) so the page shows the FR-025 sentences and **Refresh overnight access**.
3. Add US3 (T170–T172) so lean posture does not show that ask.
4. Run Phase 28, including T174.

### Suggested MVP scope

- **This revision's MVP**: Phase 23 and User Story 1 (T155–T162). That is the split credential and an overnight scan that uses only the worker token.
- **Full revision**: T155–T174, including hand start, the sentence catalogue, the refresh control, the guide, and the lean posture.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete siblings
- [Story] label maps task to spec.md user scenarios for traceability
- data-model.md field citations in task descriptions are normative for constrained fields
- Do not unit-test the AppHost graph (constitution)
- UK English in operator-facing strings; no em dash
- The auth-cookie refresh token and data-model.md `HostedSignInRecord.workerRefreshToken` must not appear in the snapshot JSON, the status blob, queue messages, or logs
- Phases 1–22 stay checked. T162 replaces only the token outcome inside the delivered pass
- T156 and T157 fail before T158 and T160. T161 fails before T162. T163 fails before T164. T165 fails before T167. T166 fails before T168
- T165 then T170 edit `OperatorSnapshotScheduleFactsServiceTests.cs`
- T166 then T171 edit `RepositoriesTests.cs`
- T168 edits `Repositories.razor` after the delivered T136 and T147
- Consult the `mudblazor` skill before T168