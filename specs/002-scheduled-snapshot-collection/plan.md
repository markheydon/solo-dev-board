# Implementation Plan: Scheduled snapshot collection

**Branch**: `002-scheduled-snapshot-collection` | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-scheduled-snapshot-collection/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

In hosted sign-in, each fill places eligible people on a queue. Eligible means a latest completed hosted sign-in inside 30 days at that fill, currently allowed, not waiting because stored access could not be used, and not already queued or in progress. A scan that already succeeded does not remove them from that set, and there is no extra wait after a finished scan. This delivery asks once each UTC day at 05:00 UTC. That is how often it asks, not a window and not a cap. If the scheduled collection process is down at 05:00 UTC, it runs the same fill when it is back. A technical failure also asks again later the same way, so the next attempt is not postponed until the following 05:00 UTC. The queue is drained whenever that process is running. Time of day does not drop an entry already on the queue. When an entry is about to run, it is removed without a failed collection only if that person is no longer allowed. If stored access cannot be used, the run fails with the sign-in or token reason. The 30-day sign-in window is evaluated when the queue is filled and when the Repositories page names the next ask, not when a message is picked up. A further try after a rate limit or an unexpected error still goes to the back of the queue, and it also waits a minimum time before it may start. The page uses one schedule sentence: not allowed, otherwise the stored-access wait, otherwise the closed 30-day window. After today's 05:00 UTC ask has passed, that page names the following day at 05:00 UTC, including when today's scan has not succeeded, unless they are still queued. The lean local posture, including a personal deployment that stays in that posture, has no schedule. Hand-started collection stays on `/repositories`.

Technical approach: keep `web` as the Azure Container App with minimum replicas 0 and maximum replicas 1. The hosted sign-in AppHost graph keeps the Azure Functions isolated worker. The timer becomes `0 0 5 * * *` UTC, one ask, replacing `0 0-15 5 * * *` on the branch. `TryFillDailyOccasion` itself has no clock window and does not treat the occasion marker as a once-per-day lock. The same method runs when the worker starts at or after 05:00 UTC and today's marker is absent, and when an `unmetFill` message becomes visible after a terminal technical outcome. That message is hidden for 15 minutes. The delay is the next ask after that outcome. It is not an eligibility filter. An Azure Storage queue, with an initial invisibility on further tries and on that later fill, holds work that must survive the worker stopping and must not start before the minimum wait. Sign-in facts the cookie cannot provide while the site is down stay a blob on the storage account ADR-0048 already added. No SQL, no Durable Task Scheduler, and no scheduled Container App Job. ADR-0046 stays withdrawn. This revision does not add a second worker, a settings page, a per-operator schedule, or an all-day timer.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`), Blazor Server, existing layering (Web, Composition, Application, Infrastructure, Domain), plus the Functions host project that references Composition and ServiceDefaults.

**Primary Dependencies**: Aspire 17.0.0 (`AddAzureFunctionsProject`, `WithHostStorage`, `AddQueues`, existing `AddAzureStorage` / `AddBlobContainer`). Isolated Functions worker (timer and storage queue extensions; versions resolved at implementation). Existing snapshot store, `IGitHubService`, and hosted GitHub App token exchange. MudBlazor on the Repositories page.

**Storage**: Snapshot JSON blob unchanged (`operator-snapshots`), plus `hosted-sign-ins` and `collection-schedule` containers and queue endpoint `operator-snapshot-queues` on the same `operator-snapshot-storage` account. Lean posture stays a local file. No database.

**Testing**: xUnit v3, Microsoft.Testing.Platform, built-in asserts, NSubstitute only where a mock is required. `TimeProvider` for the 05:00 UTC ask, catch-up when the marker is absent, and a later fill after a success. bUnit for `/repositories`. Do not unit-test the AppHost graph. No Playwright journey that waits for 05:00 UTC or collects against GitHub.

**Target Platform**: Local Aspire for lean (web only) and hosted sign-in (web, Azurite, Functions). Azure deploy remains Container Apps. The site stays scale-to-zero. The Functions publish target is the ACA function app the Aspire integration already configures, which scales to zero when idle.

**Project Type**: Blazor Server web application plus one Functions worker inside the current solution.

**Performance Goals**: One GitHub walk at a time. The site stays usable during a run. Status on `/repositories` still refreshes about once a second while a run is in progress. The worker must not require the site to scale up. A later fill wakes the worker only when someone still needs a scan.

**Constraints**: Lean `aspire start` gains no Functions, storage, or SQL. Web `MinReplicas` stays 0 and `MaxReplicas` stays 1. No HTTP between `web` and the worker. Tokens stay out of the snapshot, the queue, and logs. 05:00 UTC is when this delivery asks. The fill method must not refuse a call because of the clock, a previous success, or an existing occasion marker. Run rules and further-try waits are enforced in Application when a message is handled. Dequeue does not re-check the 30-day window. UK English. No em dash. ADR-0049 is already the binding record for the worker. Do not edit `.aspire/modules/`. Do not add a schedule setting.

**Scale/Scope**: One snapshot per operator, one sign-in blob per hosted operator, one occasion marker per UTC date, one shared queue. The marker records the latest fill. It does not veto another fill. The Repositories page is the only UI surface. No settings page and no retry button.

**UI layout**: Schedule sentences join the existing status `MudAlert` on `/repositories`. Normative copy is in [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Before research

| Gate | Result |
| --- | --- |
| Principle I, layering | Pass, by approach. Eligibility, fill, same-day unmet fill, retries, and collection stay in Application. Blob and queue adapters stay in Infrastructure. Web and the Functions host call Application ports. Triggers do not call GitHub. Composition stays the application DI root. Aspire clients are registered in each host before `AddSoloDevBoard`. |
| Principle II, proportionality | Pass. No new project. The same worker, containers, and queue absorb a later fill. No SQL, no second storage account, no HTTP API, no Durable Task hub, no all-day timer. The once-a-day ask stays a timer expression, not a lock inside eligibility. |
| Principle III, tests | Pass, by approach. Deterministic rules use `TimeProvider` and fakes. AppHost graph is not unit-tested. The Repositories guide is updated because operator-visible copy changes. |
| Principle IV, chrome | Pass, by approach. Same page, same alert, no new chrome. `mudblazor` skill at implementation. |
| Principle V, async and ServiceDefaults | Pass, by approach. New public async methods take `CancellationToken`. The Functions project calls `AddServiceDefaults()` and references `SoloDevBoard.ServiceDefaults`. |
| Principle VI, AppHost | Pass. This revision does not add AppHost resources. `web` keeps scale-to-zero. Endpoint URLs are not hardcoded. Clients use `WithReference` resource names. ADR-0049 remains the binding record. |
| Principle VII, lean path | Pass. PAT-only `aspire start` stays web-only. Hand collection remains. Copy explains the missing schedule. Hosted sign-in is the posture that models the worker. No third authentication mode. |
| ADR-0046 and ADR-0048 | Pass. SQL is not reintroduced. ADR-0048's snapshot blob stays. ADR-0049 records the worker without withdrawing ADR-0048. |

No unjustified violation.

### After design

Re-checked against [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), and [quickstart.md](quickstart.md).

| Gate | Result |
| --- | --- |
| Principle VI | Still pass. No new resource. A later fill is another message on `daily-snapshot-collection` and another call to the same fill method. `web` does not reference the worker. Scale on `web` stays minimum 0 and maximum 1. |
| Principle VII | Still pass. Quickstart requires a lean start with no Functions resource and no fill at 05:00 UTC. Hosted local run includes the worker because that posture has the schedule. |
| Proportionality | Still pass. An all-day timer, Container App Jobs, and Durable Task Scheduler stay rejected. The 15-minute hide on `unmetFill` is the next ask after a terminal technical outcome. It does not skip a successful scan. `retainedRun` stays one optional object. It is not a history of occasions. |
| ADR-0046 | Still pass. No Azure SQL and no inventory database. |

Gates pass. No NEEDS CLARIFICATION remains. FR-001, FR-006, FR-015, FR-016, FR-017, and FR-021, and the 2026-11-24 clarifications, are recorded in [research.md](research.md).

## Project Structure

### Documentation (this feature)

```text
specs/002-scheduled-snapshot-collection/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── hosted-sign-in-record.md
│   ├── daily-collection-schedule.md
│   └── repositories-schedule-copy.md
└── tasks.md             # /speckit-tasks, not this command
```

### Source Code (repository root)

```text
src/SoloDevBoard.Functions/                 # isolated worker; thin triggers only
src/SoloDevBoard.Application/Services/Snapshots/   # occasion fill, unmet fill, eligibility, daily pass, status facts
src/SoloDevBoard.Application/Authentication/       # sign-in record model and token-exchange port
src/SoloDevBoard.Infrastructure/Snapshots/         # snapshot fields, interruption branch, schedule blobs, queue
src/SoloDevBoard.Infrastructure/Identity/          # sign-in blob adapter and shared refresh exchange
src/SoloDevBoard.Composition/                      # register ports; no connection-string lookup
src/SoloDevBoard.Web/Program.cs                    # sign-in blob client; do not treat daily inProgress as siteStopped
src/SoloDevBoard.Web/Authentication/               # write the sign-in record when hosted sign-in completes
src/SoloDevBoard.Web/Components/Features/Repositories/  # schedule sentences; start control respects the queue
src/SoloDevBoard.AppHost/LeanAppHostGraph.cs       # unchanged: no Functions, no storage
src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs   # worker, containers, queue; ADR-0049
src/SoloDevBoard.AppHost/SoloDevBoardAppHost.cs    # keep web MinReplicas 0, MaxReplicas 1
src/SoloDevBoard.ServiceDefaults/                  # referenced by the Functions host

tests/SoloDevBoard.Application.Tests/              # window, same-day fill, eligibility, retries, single flight
tests/SoloDevBoard.Infrastructure.Tests/           # interruption branch, etag, no token in snapshot
tests/SoloDevBoard.Web.Tests/                      # bUnit schedule copy
website/content/docs/repositories.md               # shipped behaviour, same change
adr/adr-0049-scheduled-collection-functions.md     # binding ADR; already required before AppHost edits
```

**Structure Decision**: One Functions host project, already in the solution. Do not add an `apiservice`, a test project for the AppHost, or a SQL project. Schedule rules live in Application so they can be tested without the Functions host. The worker's `Program.cs` registers Aspire clients and `AddServiceDefaults`, then `AddSoloDevBoard`.

## Implementation notes

The branch already has the Functions worker, the queue, the sign-in blob, and the Repositories schedule sentences. Those pieces stay. This plan replaces three behaviours that code and the previous plan still have: the 05:00 to 05:15 window and the occasion marker as a once-per-day lock (`IsInFillWindow`, `AlreadyFilled`), the dequeue check that skips a closed 30-day window or `credentialUnusable` without a failed run (`IsEligibleAtRun`), and the page naming today's 05:00 UTC after that ask has passed when the scan has not succeeded. Phases 1–11 in [tasks.md](tasks.md) record delivered work; **Phase 12** (tasks T082, T097–T119, T112) is the open delta for this 2026-11-24 revision.

Order for the delta:

1. Change `TryFillDailyOccasion` so it can run at any UTC time. Delete the 05:00 to 05:15 gate and the "marker exists, so skip" return. Measure the 30-day window at the fill instant. Delete the rule that a sign-in after 05:00 UTC is ineligible until the next day. Queue every eligible person who is not already queued or in progress, including after a successful scan. A copy that loses the fill lease retries the fill. Two copies still cannot queue the same person twice while they are already queued or in progress.
2. Change the timer to `0 0 5 * * *` UTC. On worker start, repair missing `initial` messages as today, and call the fill only when the clock is at or after 05:00 UTC and today's marker is absent. Do not fill before 05:00 UTC on start. Do not set `RunOnStartup` on the timer in a way that fills overnight. Completing sign-in does not call the fill.
3. After a terminal technical outcome (rate limit or unexpected error with no further tries left, or an interruption), enqueue one `unmetFill` message, hidden for 15 minutes, including when that outcome is before 05:00 UTC. That message calls the same fill and does not walk GitHub itself. A `signInOrToken` failure does not enqueue it. Do not rejoin the interrupted pass.
4. On dequeue, attempt the scan unless the person is no longer allowed. Do not drop the message because `completedAt` is outside 30 days. Not allowed still removes the entry with no failed collection. If stored access cannot be used, record `signInOrToken`, set `credentialUnusable`, and do not restore `retainedRun` over that failure.
5. Schedule facts and Repositories copy: one sentence, in order not allowed, then stored-access wait, then closed window. Before 05:00 UTC, name today when they would be eligible then. After the ask has passed, name the following day at 05:00 UTC when they would be eligible then, whether or not today's scan succeeded, unless they are still queued for today. While queued and not refused, keep naming today and do not say the day was missed. When a stored-access wait is set, still show that failed run if the sentence says they are not allowed. bUnit and the user guide follow that copy.
6. Hosted AppHost graph stays as ADR-0049 already describes. No new resource. Lean graph untouched. Re-apply web scale (minimum 0, maximum 1) if a custom-domain callback replaces it.

Validation before a pull request is in [quickstart.md](quickstart.md).

## Complexity Tracking

No constitution violations require justification. The Functions project is the spec's separate scheduled process, and ADR-0049 is the binding record Principle VI requires. A later fill reuses that worker and the existing queue. It does not add a project or a timer.
