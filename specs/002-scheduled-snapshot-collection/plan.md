# Implementation Plan: Scheduled snapshot collection

**Branch**: `002-scheduled-snapshot-collection` | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-scheduled-snapshot-collection/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

In hosted sign-in, each fill places eligible people on a queue. Eligible means a latest completed hosted sign-in inside 30 days at that fill, currently allowed, not waiting because stored access could not be used, and not already queued or in progress. A reported `stalled` account is not queued and is not in progress. A scan that already succeeded does not remove them from that set. This delivery asks once each UTC day at 05:00 UTC. If the scheduled collection process is down at 05:00 UTC, it runs the same fill when it is back. A finished technical failure (rate limit or unexpected error with no further tries left) may ask again sooner through `unmetFill`. A worker stop during a taken scan does not. The queue is drained whenever that process is running.

The 2026-11-25 revision adds one stored account scan status per hosted account seen under that 30-day rule. The record is created when the account is first recorded for later scanning, including when the sign-in blob stores the refresh token. The default is `never run`. The page reads that record at any time of day. It does not infer running, finished, failed, or suspended from the clock, from a missing visit, or from the absence of a worker. While a hosted scan is `running` and still working, the process doing that scan writes a fresh liveness mark at least once every 60 seconds until the scan records `finished` or `suspended`. If no liveness mark has been recorded for more than 5 minutes, callers are told `stalled` and the stored status stays `running`. `stalled` is not stored. The lean local posture has no schedule and no account scan status.

Technical approach: keep `web` as the Azure Container App with minimum replicas 0 and maximum replicas 1. Keep the isolated Functions worker on the hosted sign-in graph only. The timer stays `0 0 5 * * *` UTC. Add one blob container, `account-scan-status`, on the existing `operator-snapshot-storage` account, and reference it from `web` and from the Functions project with `WithReference`. Do not put the status on the sign-in blob (that blob holds the refresh token and must not be rewritten every minute) and do not put it in `collection-schedule` (that container stays worker-only so `web` cannot write the runner lease). No new storage account, no SQL, no second worker, and no HTTP between `web` and the worker. Lean `LeanAppHostGraph` stays web-only. ADR-0049 is amended in the same implementation change for the container and for the timer expression the accepted ADR still shows as `0 0-15 5 * * *`. Do not edit `.aspire/modules/`.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`), Blazor Server, existing layering (Web, Composition, Application, Infrastructure, Domain), plus the Functions host project that references Composition and ServiceDefaults.

**Primary Dependencies**: Aspire 17.0.0 (`AddAzureFunctionsProject`, `WithHostStorage`, `AddQueues`, `AddBlobContainer`, `WithReference`). Isolated Functions worker. Existing snapshot store, `IGitHubService`, and hosted GitHub App token exchange. Keyed `BlobContainerClient` registration already used for `hosted-sign-ins` (`AddKeyedAzureBlobContainerClient`). MudBlazor on the Repositories page. No new NuGet package for the status container.

**Storage**: Snapshot JSON blob unchanged (`operator-snapshots`), plus `hosted-sign-ins`, `collection-schedule`, new `account-scan-status`, and queue endpoint `operator-snapshot-queues` on the same `operator-snapshot-storage` account. Lean posture stays a local file and does not write a status blob. No database.

**Testing**: xUnit v3, Microsoft.Testing.Platform, built-in asserts, NSubstitute only where a port double is required. `TimeProvider` for the 05:00 UTC ask and for the liveness boundaries (a mark age of exactly 5 minutes is still told `running`; more than 5 minutes is told `stalled`; a scan that is still working must record another mark before 60 seconds elapse). Those two ages are separate tests. bUnit in `RepositoriesTests` for `/repositories`. Do not unit-test the AppHost graph. No Playwright journey that waits for 05:00 UTC or collects against GitHub. Test names use `MethodUnderTest_Scenario_ExpectedOutcome`.

**Target Platform**: Local Aspire for lean (web only) and hosted sign-in (web, Azurite, Functions). Azure deploy remains Container Apps. The site stays scale-to-zero. The Functions publish target stays the ACA function app the Aspire integration already configures.

**Project Type**: Blazor Server web application plus one Functions worker inside the current solution.

**Performance Goals**: One GitHub walk at a time. The site stays usable during a run. Status on `/repositories` still refreshes about once a second while a run is in progress. A liveness write is one small blob update at least every 60 seconds during a hosted scan. It does not rewrite the catalogue snapshot on that cadence. The worker must not require the site to scale up.

**Constraints**: Lean `aspire start` gains no Functions, storage, or SQL. Web `MinReplicas` stays 0 and `MaxReplicas` stays 1. No HTTP between `web` and the worker. Tokens stay out of the snapshot, the status blob, the queue, and logs. Endpoint URLs are not hardcoded in `WithEnvironment`. Clients use the `WithReference` resource name `account-scan-status`. The fill method must not refuse a call because of the clock, a previous success, or an existing occasion marker. A reported `stalled` account must not be treated as in progress. Dequeue does not re-check the 30-day window. A lapse of the liveness mark must not record `finished` and must not rejoin the queue entry. UK English. No em dash. Do not add a schedule setting.

**Scale/Scope**: One snapshot per operator, one sign-in blob per hosted operator, one status blob per hosted account, one occasion marker per UTC date, one shared queue. The Repositories page is the only UI surface. No settings page and no retry button.

**UI layout**: The stored status and the schedule sentence join the existing status `MudAlert` on `/repositories`. Normative copy is in [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Before research

| Gate | Result |
| --- | --- |
| Principle I, layering | Pass, by approach. Eligibility, fill, status transitions, liveness, retries, and collection stay in Application. Blob and queue adapters stay in Infrastructure. Web and the Functions host call Application ports. Triggers do not call GitHub. Composition stays the application DI root. Aspire clients are registered in each host before `AddSoloDevBoard`. |
| Principle II, proportionality | Pass. No new project and no new storage account. One container on the account ADR-0049 already uses. The sign-in blob is the wrong home because a 60-second liveness write would contend with the refresh token. `collection-schedule` stays worker-only. No SQL, no HTTP API, no Durable Task hub. |
| Principle III, tests | Pass, by approach. Deterministic rules use `TimeProvider` and fakes. The 5-minute boundary and the 60-second cadence are separate asserts. AppHost graph is not unit-tested. The Repositories guide is updated because operator-visible copy changes. |
| Principle IV, chrome | Pass, by approach. Same page, same alert, no new chrome. `mudblazor` skill at implementation. |
| Principle V, async and ServiceDefaults | Pass, by approach. New public async methods take `CancellationToken`. The Functions project already calls `AddServiceDefaults()`. |
| Principle VI, AppHost | Pass, with a required ADR amendment. The new container is `storage.AddBlobContainer("account-scan-status", "account-scan-status")` in both `ConfigureLocalRun` and `ConfigurePublishInfrastructure`, then `WithReference` from `web` and from the Functions project. No hardcoded endpoint URL. `web` scale stays minimum 0 and maximum 1. Lean graph is not edited. Amend ADR-0049 in the same change before treating the graph edit as done. Existing role assignments on the storage account already include blob data for the Functions identity. `web` keeps the access path it already uses for `hosted-sign-ins`. |
| Principle VII, lean path | Pass. PAT-only `aspire start` stays web-only. Hand collection remains. Copy explains the missing schedule and does not show a worker status. Hosted sign-in is the posture that models the worker and the status container. No third authentication mode. |
| ADR-0046 and ADR-0048 | Pass. SQL is not reintroduced. ADR-0048's snapshot blob stays. ADR-0049 is amended, not withdrawn, and does not withdraw ADR-0048. |

No unjustified violation.

### After design

Re-checked against [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), and [quickstart.md](quickstart.md).

| Gate | Result |
| --- | --- |
| Principle I | Still pass. Status transitions and the liveness rule live in Application. Infrastructure stores the blob with an etag. Web does not decide `stalled` by guessing that the worker is down. |
| Principle II | Still pass. `scanGeneration` is a compare-and-swap on the one status blob, not a new lock service. Queue visibility renewal stays an infrastructure detail of a live pass. It is not a second operator-facing lease. |
| Principle III | Still pass. Quickstart names separate cases for exactly 5 minutes, more than 5 minutes, the 60-second cadence, orphan `scheduled`, and the single winner when a fill and a hand start meet a `stalled` account. |
| Principle VI | Still pass. One new container on `operator-snapshot-storage`. `web` does not reference the queue or `collection-schedule`. No `WaitFor` between `web` and the worker. |
| Principle VII | Still pass. Quickstart requires a lean start with no Functions resource, no status container, and no fill at 05:00 UTC. |
| FR-022 and FR-023 | Recorded in [research.md](research.md) and [data-model.md](data-model.md). `stalled` is reported only. A lapse does not write `finished` and does not enqueue `unmetFill`. |

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
│   ├── account-scan-status.md
│   ├── daily-collection-schedule.md
│   └── repositories-schedule-copy.md
└── tasks.md             # /speckit-tasks, not this command
```

### Source Code (repository root)

```text
src/SoloDevBoard.Functions/                 # thin triggers; liveness is not a trigger
src/SoloDevBoard.Application/Services/Snapshots/   # status transitions, fill, pass, liveness cadence
src/SoloDevBoard.Application/Authentication/       # sign-in record; creating the status blob when access is stored
src/SoloDevBoard.Infrastructure/Snapshots/         # status blob adapter, queue, schedule blobs
src/SoloDevBoard.Infrastructure/Identity/          # sign-in blob; no liveness writes here
src/SoloDevBoard.Composition/                      # AspireHostedStorageReferenceNames.AccountScanStatus
src/SoloDevBoard.Web/Program.cs                    # keyed blob client for account-scan-status
src/SoloDevBoard.Web/Authentication/               # on completed hosted sign-in, create never-run status if absent
src/SoloDevBoard.Web/Components/Features/Repositories/  # read reported status; start control
src/SoloDevBoard.AppHost/LeanAppHostGraph.cs       # unchanged
src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs   # AddBlobContainer account-scan-status; WithReference on web and Functions
src/SoloDevBoard.AppHost/SoloDevBoardAppHost.cs    # keep web MinReplicas 0, MaxReplicas 1
src/SoloDevBoard.Functions/Program.cs              # keyed blob client, same resource name

tests/SoloDevBoard.Application.Tests/              # window, fill, status, liveness boundaries, single winner
tests/SoloDevBoard.Infrastructure.Tests/           # etag loser does not overwrite; no token in the status blob
tests/SoloDevBoard.Web.Tests/                      # bUnit status and schedule copy
website/content/docs/repositories.md               # shipped behaviour, same change
adr/adr-0049-scheduled-collection-functions.md     # amend: container, web reference, timer 0 0 5 * * *
```

**Structure Decision**: One Functions host project, already in the solution. Do not add an `apiservice`, a test project for the AppHost, or a SQL project. The status record is an Application model with an Infrastructure blob adapter. Schedule rules and status transitions stay in Application so they can be tested without the Functions host and without the AppHost graph. The worker's `Program.cs` and `web`'s `Program.cs` register the keyed container client, then `AddSoloDevBoard`.

## Implementation notes

Phases 1–12 in [tasks.md](tasks.md) are delivered work, including the 2026-11-24 scheduling delta (timer `0 0 5 * * *`, fill at any time, no 30-day check at dequeue). Phases 13–22 (T120–T154) are the open 2026-11-25 account scan status backlog. Do not append another copy of those phases. Do not treat the page's current inference from `queuedOccasion`, `passExecuting`, and `run.status` as the implementation of FR-022.

Order for the status delta:

1. Add `account-scan-status` in both hosted graph methods. Reference it from `web` and the Functions project. Register `AddKeyedAzureBlobContainerClient` under `AspireHostedStorageReferenceNames.AccountScanStatus` in both hosts. Amend ADR-0049 in the same change, including the timer expression. Leave `LeanAppHostGraph` untouched.
2. Add the status blob and the Application reader that reports `stalled` when stored status is `running` and the latest liveness mark, or `runningSince` when no mark exists, is more than 5 minutes before `TimeProvider.GetUtcNow()`. Exactly 5 minutes is still `running`. Do not persist `stalled`.
3. When a hosted sign-in completes, the completed sign-in path under `src/SoloDevBoard.Web/Authentication/` calls the Application transition that creates the status blob if it is absent (`never run`, empty last-run time, empty result, empty comment, `suspensionReason` null). Do not reset a blob that already exists. A later sign-in clears `credentialUnusable` on the sign-in record and does not change the status. Readers report a missing hosted blob as `never run` and do not write it. A fill inserts that same `neverRun` shape, before eligibility, when a sign-in record has no status blob yet.
4. Change fill. Before choosing who to queue, insert a missing `neverRun` blob for a sign-in record that has none. Then restore each account whose stored status is `scheduled` and whose `queueMessagePending` is false: empty last result becomes `never run`; any other last result becomes `finished`; do not change the last scan time, result, or comment, and do not add a failure comment. Then apply the existing eligibility rules. `scheduled` is already queued. `running` that callers would be told `running` is in progress. A reported `stalled` account is neither. The winning transition sets `scheduled`, clears `suspensionReason`, sets `queueVisibleAfter` null, increments `scanGeneration`, and then enqueues the `initial`. Set `queueMessagePending` only after the queue accepts the message. If the enqueue fails, leave that flag false so the next fill restores the status before it tries again. A lost etag does not enqueue and does not change the snapshot. Host start re-enqueues a missing `initial` only when status is `scheduled`, `queueMessagePending` is true, `queueVisibleAfter` is null or already passed, and a peek finds no visible message. It does not do that while `queueVisibleAfter` is still ahead, and it does not use `queuedOccasion`.
5. On dequeue, an allowed take sets `running`, clears `suspensionReason`, and writes the liveness mark in that same update. A not-allowed pickup does not set `running`. It is one write to `suspended` with `suspensionReason` `notAllowed`, `queueMessagePending` false, and the last scan time, result, result comment, and the successful snapshot unchanged. Do not walk GitHub. Stored access cannot be used: the token exchange failed before the catalogue walk. Set `suspended`, result failure, sign-in or token comment, `suspensionReason` null, `queueMessagePending` false, set `credentialUnusable`, no further try, no `unmetFill`. A further try waiting at the back sets `scheduled`, `queueMessagePending` true, and restores the previous terminal snapshot from `retainedRun`. It is not `running` and not `finished`. Exhausted rate-limit or unexpected error sets `finished` with that failure, clears `queueMessagePending`, then one `unmetFill` hidden for 15 minutes. Do not enqueue `unmetFill` because a liveness mark lapsed, because of redelivery, or because of `scheduledCollectionStopped`.
6. While a hosted scan is still working (queue pass or hand-started hosted scan), write a fresh liveness mark at least once every 60 seconds until `finished` or `suspended`. The mark write must carry the `scanGeneration` captured when that scan began. After a newer scan has set `scheduled` or a new `running`, the earlier pass must not change the status, the liveness mark, or the snapshot. Until that newer scan wins, the earlier pass may still record its real success or failure, and a fresh mark from that same pass means callers are told `running` again.
7. Queue redelivery after the worker died: complete the message, leave the stored status `running`, clear `queueMessagePending` without changing status, last result, or the snapshot, do not write `finished` or `interrupted`, do not clear `retainedRun`, do not write `scheduledCollectionStopped`, and do not put the pass back on the queue. A stop while the status is only `scheduled` leaves it `scheduled`. `web` stopping must not rewrite a worker-owned status and must not mark a daily pass as `siteStopped`.
8. Hand-started hosted collection updates the same record. It sets `running` (and the liveness mark) when it starts, and `finished` or `suspended` when it ends. It does not use the queue. The start control does not start another scan while the account is `scheduled` or callers are told `running`. It may start while callers are told `stalled`. When a fill and a hand start race, the first conditional write wins and the other attempt does not start a second walk.
9. Repositories copy and `website/content/docs/repositories.md` follow [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md). bUnit locks that copy.

Validation before a pull request is in [quickstart.md](quickstart.md).

## Complexity Tracking

No constitution violations require justification. The Functions project remains the spec's separate scheduled process. The new container is the smallest store that both hosts can update without writing the refresh-token blob or the runner lease. ADR-0049 is the binding record, amended in the same change.
