# Implementation Plan: Scheduled snapshot collection

**Branch**: `002-scheduled-snapshot-collection` | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-scheduled-snapshot-collection/spec.md`

## Summary

In hosted sign-in, the existing operator snapshot collection is placed on a queue once each UTC day at 05:00 UTC, with the 15-minute grace window, and that queue is drained whenever the scheduled collection process is running. Time of day does not drop an entry already on the queue. When an entry is about to run, it is removed without a failed collection if that person is no longer allowed or their latest completed sign-in is outside 30 days. A further try after a rate limit or an unexpected error still goes to the back of the queue, and it also waits a minimum time before it may start. The lean local posture, including a personal deployment that stays in that posture, has no schedule. Hand-started collection stays on `/repositories`.

Technical approach: keep `web` as the Azure Container App with minimum replicas 0 and maximum replicas 1. The hosted sign-in AppHost graph keeps the Azure Functions isolated worker from the first plan. The UTC timer only fills the queue. An Azure Storage queue, with an initial invisibility on further tries, holds work that must survive the worker stopping and must not start before the minimum wait. Sign-in facts the cookie cannot provide while the site is down stay a blob on the storage account ADR-0048 already added. No SQL, no Durable Task Scheduler, and no scheduled Container App Job. ADR-0046 stays withdrawn. This revision does not add a second worker, a settings page, or a per-operator schedule.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`), Blazor Server, existing layering (Web, Composition, Application, Infrastructure, Domain), plus a new Functions host project that references Composition and ServiceDefaults.

**Primary Dependencies**: Aspire 17.0.0 (`AddAzureFunctionsProject`, `WithHostStorage`, `AddQueues`, existing `AddAzureStorage` / `AddBlobContainer`). Isolated Functions worker (timer and storage queue extensions; versions resolved at implementation). Existing snapshot store, `IGitHubService`, and hosted GitHub App token exchange. MudBlazor on the Repositories page.

**Storage**: Snapshot JSON blob unchanged (`operator-snapshots`), plus `hosted-sign-ins` and `collection-schedule` containers and queue endpoint `operator-snapshot-queues` on the same `operator-snapshot-storage` account. Lean posture stays a local file. No database.

**Testing**: xUnit v3, Microsoft.Testing.Platform, built-in asserts, NSubstitute only where a mock is required. `TimeProvider` for the occasion window. bUnit for `/repositories`. Do not unit-test the AppHost graph. No Playwright journey that waits for 05:00 UTC or collects against GitHub.

**Target Platform**: Local Aspire for lean (web only) and hosted sign-in (web, Azurite, Functions). Azure deploy remains Container Apps. The site stays scale-to-zero. The Functions publish target is the ACA function app the Aspire integration already configures, which scales to zero when idle.

**Project Type**: Blazor Server web application plus one Functions worker inside the current solution.

**Performance Goals**: One daily GitHub walk at a time. The site stays usable during a daily run. Status on `/repositories` still refreshes about once a second while a run is in progress. The worker must not require the site to scale up.

**Constraints**: Lean `aspire start` gains no Functions, storage, or SQL. Web `MinReplicas` stays 0 and `MaxReplicas` stays 1. No HTTP between `web` and the worker. Tokens stay out of the snapshot, the queue, and logs. The 05:00 UTC window bounds queue fill only. Run rules and further-try waits are enforced in Application when a message is handled. UK English. No em dash. A binding ADR (ADR-0049) is Accepted before AppHost edits. Do not edit `.aspire/modules/`.

**Scale/Scope**: One snapshot per operator, one sign-in blob per hosted operator, one occasion marker per UTC date, one shared queue. The Repositories page is the only UI surface. No settings page and no retry button.

**UI layout**: Schedule sentences join the existing status `MudAlert` on `/repositories`. Normative copy is in [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Before research

| Gate | Result |
| --- | --- |
| Principle I, layering | Pass, by approach. Eligibility, fill, retries, and collection stay in Application. Blob and queue adapters stay in Infrastructure. Web and the Functions host call Application ports. Triggers do not call GitHub. Composition stays the application DI root. Aspire clients are registered in each host before `AddSoloDevBoard`. |
| Principle II, proportionality | Pass, with a new project that the spec requires. The scheduled process cannot be the site. One extra project, two containers, and one queue on the existing storage account. No SQL, no second storage account, no HTTP API, no Durable Task hub. |
| Principle III, tests | Pass, by approach. Deterministic rules use `TimeProvider` and fakes. AppHost graph is not unit-tested. The Repositories guide is updated because operator-visible copy changes. |
| Principle IV, chrome | Pass, by approach. Same page, same alert, no new chrome. `mudblazor` skill at implementation. |
| Principle V, async and ServiceDefaults | Pass, by approach. New public async methods take `CancellationToken`. The Functions project calls `AddServiceDefaults()` and references `SoloDevBoard.ServiceDefaults`. |
| Principle VI, AppHost | Pass, with a timing gate. The worker and the extra storage resources are hosted-graph only, after ADR-0049 is Accepted. `web` keeps scale-to-zero. Endpoint URLs are not hardcoded. Clients use `WithReference` resource names. |
| Principle VII, lean path | Pass. PAT-only `aspire start` stays web-only. Hand collection remains. Copy explains the missing schedule. Hosted sign-in is the posture that models the worker. No third authentication mode. |
| ADR-0046 and ADR-0048 | Pass. SQL is not reintroduced. ADR-0048's snapshot blob stays. ADR-0048's "do not add Functions" line is the snapshot feature, not a ban on this later schedule. ADR-0049 records the worker without withdrawing ADR-0048. |

No unjustified violation.

### After design

Re-checked against [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), and [quickstart.md](quickstart.md).

| Gate | Result |
| --- | --- |
| Principle VI | Still pass. Design names `AddAzureFunctionsProject`, `WithHostStorage` on `operator-snapshot-storage`, `AddQueues("operator-snapshot-queues")`, and `AddAzureQueueServiceClient`. `web` does not reference the worker. Scale on `web` stays minimum 0 and maximum 1. ADR-0049 is required before those edits. |
| Principle VII | Still pass. Quickstart requires a lean start with no Functions resource and no fill at 05:00 UTC. Hosted local run includes the worker because that posture has the schedule. |
| Proportionality | Still pass. Container App Jobs and Durable Task Scheduler stay rejected. Run-rule removal and the further-try wait use the existing queue and snapshot document. `retainedRun` is one optional object so a refused further try can put the previous run back. It is not a history of occasions. |
| ADR-0046 | Still pass. No Azure SQL and no inventory database. |

Gates pass. No NEEDS CLARIFICATION remains.

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
src/SoloDevBoard.Functions/                 # new isolated worker; thin triggers only
src/SoloDevBoard.Application/Services/Snapshots/   # occasion fill, eligibility, daily pass, status facts
src/SoloDevBoard.Application/Authentication/       # sign-in record model and token-exchange port
src/SoloDevBoard.Infrastructure/Snapshots/         # new fields, interruption branch, schedule blobs, queue
src/SoloDevBoard.Infrastructure/Identity/          # sign-in blob adapter and shared refresh exchange
src/SoloDevBoard.Composition/                      # register ports; no connection-string lookup
src/SoloDevBoard.Web/Program.cs                    # sign-in blob client; do not treat daily inProgress as siteStopped
src/SoloDevBoard.Web/Authentication/               # write the sign-in record when hosted sign-in completes
src/SoloDevBoard.Web/Components/Features/Repositories/  # schedule sentences; start control respects the queue
src/SoloDevBoard.AppHost/LeanAppHostGraph.cs       # unchanged: no Functions, no storage
src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs   # worker, containers, queue; only after ADR-0049
src/SoloDevBoard.AppHost/SoloDevBoardAppHost.cs    # keep web MinReplicas 0, MaxReplicas 1
src/SoloDevBoard.ServiceDefaults/                  # referenced by the Functions host

tests/SoloDevBoard.Application.Tests/              # window, eligibility, retries, single flight
tests/SoloDevBoard.Infrastructure.Tests/           # interruption branch, etag, no token in snapshot
tests/SoloDevBoard.Web.Tests/                      # bUnit schedule copy
website/content/docs/repositories.md               # shipped behaviour, same change
adr/adr-0049-scheduled-collection-functions.md     # binding ADR before AppHost edits
```

**Structure Decision**: Add one Functions host project. Do not add an `apiservice`, a test project for the AppHost, or a SQL project. Schedule rules live in Application so they can be tested without the Functions host. The worker's `Program.cs` registers Aspire clients and `AddServiceDefaults`, then `AddSoloDevBoard`.

## Implementation notes

Order:

1. Write and accept ADR-0049 with the ADR skill. Decision content is [research.md](research.md). Do not edit the AppHost until that status is Accepted. Do not withdraw ADR-0048 or reinstate ADR-0046.
2. Add the Aspire 17.0.0 packages named in research, after the usual package-add approval. Resolve Functions worker package versions then. Do not add them in planning.
3. Extend the snapshot document and the read-path interruption rule. Daily passes must not become `siteStopped` because the web process has no live task.
4. Sign-in record port and blob adapter. Web writes it when hosted sign-in completes. Sign-out does not delete it. Move token exchange behind the shared port.
5. Occasion fill, queue, runner lease, and daily pass, including the three further tries and redelivery-as-interruption. `functionTimeout` `02:00:00`. Timer `0 0-15 5 * * *` UTC fills only. The queue trigger has no time-of-day gate.
6. Before a pass starts, re-check allowance and the 30-day window at the current UTC time. On failure, complete the message, restore the previous operator-visible run, and do not record a failed collection. Further tries carry `notBefore` and an initial queue invisibility (GitHub retry delay or 2 minutes for a rate limit; 3, 6, then 12 minutes for an unexpected error).
7. Repositories copy, bUnit, and the user guide. The page states the no-schedule reason when the next fill will not include them, including after a queued entry was removed.
8. Hosted AppHost graph only: `WithHostStorage(operator-snapshot-storage)`, `AddBlobContainer` for `hosted-sign-ins` and `collection-schedule`, `AddQueues("operator-snapshot-queues")`, `AddAzureFunctionsProject` by project path, `WithReference` from the worker, `WaitFor` the emulator locally. `web` references `hosted-sign-ins` only. No `WithExternalHttpEndpoints`. Publish assigns the four storage roles on the Functions identity. Lean graph untouched. Re-apply web scale (minimum 0, maximum 1) if the custom-domain callback replaces it.
9. Update `docs/getting-started.md`, `docs/deployment.md`, and `src/SoloDevBoard.AppHost/README.md` for the new hosted resources.

Validation before a pull request is in [quickstart.md](quickstart.md).

## Complexity Tracking

No constitution violations require justification. The Functions project is the spec's separate scheduled process, and ADR-0049 is the binding record Principle VI requires.
