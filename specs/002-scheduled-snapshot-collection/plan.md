# Implementation Plan: Scheduled snapshot collection

**Branch**: `002-scheduled-snapshot-collection` | **Date**: 2026-11-26 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-scheduled-snapshot-collection/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

In hosted sign-in, each fill places eligible people on a queue. Eligible means a latest completed hosted sign-in inside 30 days at that fill, currently allowed, not waiting because the worker credential could not be used, and not already queued or in progress. A reported `stalled` account is not queued and is not in progress. A scan that already succeeded does not remove them from that set. This delivery asks once each UTC day at 05:00 UTC. If the scheduled collection process is down at 05:00 UTC, it runs the same fill when it is back. A finished technical failure (rate limit or unexpected error with no further tries left) may ask again sooner through `unmetFill`. A worker stop during a taken scan does not. The queue is drained whenever that process is running.

The 2026-11-25 account scan status stays. One blob per hosted account, default `never run`, liveness at least every 60 seconds while a hosted scan is still working, and a reported `stalled` when that mark is strictly older than 5 minutes. `stalled` is not stored. The lean local posture has no schedule, no account scan status, and no worker token.

The 2026-11-26 spec update splits credentials and replaces the operator-facing clock wording. An overnight scan uses only a worker refresh token from a second GitHub authorisation on the existing GitHub App, after the browser refresh token is already in the auth cookie. One GitHub response is never split into both tokens. The worker does not exchange the cookie token and does not write a rotated replacement of it. A hand-started hosted collection uses the signed-in session and can run when the worker token is missing. Sign-in is complete once the browser token works, including when the worker token is not stored. Storing the worker token again, either in that sign-in or from a refresh the operator chooses on `/repositories`, ends the stored-access wait. That page refresh does not move the 30-day window, does not replace the saved organisation list, and does not start a scan. Callers receive the FR-025 sentences. Those sentences do not name 05:00 UTC, do not say who started the run, and do not say "daily occasion". 05:00 UTC remains the ask time only.

Technical approach: keep `web` scaled to minimum 0 and maximum 1, and keep the isolated Functions worker on the hosted sign-in graph only. No new storage account, no new container, no SQL, no second worker, no HTTP between `web` and the worker, and no third authentication mode. The worker credential is two new fields on the existing `hosted-sign-ins` blob (`workerRefreshToken`, `workerRefreshTokenExpiresAt`, schema version 2). The browser refresh token stays in the auth cookie only. A schema 1 blob's `refreshToken` is not copied into the worker field and is not exchanged, because that value is the interactive grant. Amend ADR-0015 in the same implementation change so the binding record states this second authorisation. ADR-0049 stays the binding record for the worker and the `account-scan-status` container. Do not edit `.aspire/modules/`.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`), Blazor Server, existing layering (Web, Composition, Application, Infrastructure, Domain), plus the Functions host project that references Composition and ServiceDefaults.

**Primary Dependencies**: Aspire 17.0.0 (`AddAzureFunctionsProject`, `WithHostStorage`, `AddQueues`, `AddBlobContainer`, `WithReference`). Isolated Functions worker. Existing snapshot store, `IGitHubService`, and the existing GitHub App user-token exchange (`grant_type=refresh_token` on `https://github.com/login/oauth/access_token`). The second authorisation uses the same App, the same callback, and a different OAuth state purpose. Keyed `BlobContainerClient` registration already used for `hosted-sign-ins`. MudBlazor on the Repositories page. No new NuGet package.

**Storage**: Snapshot JSON blob unchanged (`operator-snapshots`). `hosted-sign-ins` gains the worker refresh token fields and moves to schema version 2. `collection-schedule`, `account-scan-status`, and queue endpoint `operator-snapshot-queues` stay on `operator-snapshot-storage`. Lean posture stays a local file and does not write a status blob or a worker token. No database.

**Testing**: xUnit v3, Microsoft.Testing.Platform, built-in asserts, NSubstitute only where a port double is required. `TimeProvider` for the 05:00 UTC ask and for the liveness boundaries (a mark age of exactly 5 minutes is still told `running`; more than 5 minutes is told `stalled`; a scan that is still working must record another mark before 60 seconds elapse). Those two ages are separate tests. Separate tests for the worker-token rules: schema 1 is not exchanged, a queued scan uses the token stored when it is taken, a running scan does not overwrite a newer worker token, and a token failure with a newer token stored is `finished` without the wait. bUnit in `RepositoriesTests` locks the FR-025 sentences and the worker-token ask. Do not unit-test the AppHost graph. No Playwright journey that waits for 05:00 UTC or collects against GitHub. Test names use `MethodUnderTest_Scenario_ExpectedOutcome`.

**Target Platform**: Local Aspire for lean (web only) and hosted sign-in (web, Azurite, Functions). Azure deploy remains Container Apps. The site stays scale-to-zero. The Functions publish target stays the ACA function app the Aspire integration already configures.

**Project Type**: Blazor Server web application plus one Functions worker inside the current solution.

**Performance Goals**: One GitHub walk at a time. The site stays usable during a run. Status on `/repositories` still refreshes about once a second while a run is in progress. A liveness write is one small blob update at least every 60 seconds during a hosted scan. It does not rewrite the catalogue snapshot on that cadence. The worker must not require the site to scale up.

**Constraints**: Lean `aspire start` gains no Functions, storage, or SQL. Web `MinReplicas` stays 0 and `MaxReplicas` stays 1. No HTTP between `web` and the worker. The worker must not exchange the auth-cookie refresh token. Tokens stay out of the snapshot, the status blob, the queue, and logs. Endpoint URLs are not hardcoded in `WithEnvironment`. Clients use the `WithReference` resource name `account-scan-status`. The fill method must not refuse a call because of the clock, a previous success, or an existing occasion marker. A reported `stalled` account must not be treated as in progress. Dequeue does not re-check the 30-day window. A lapse of the liveness mark must not record `finished` and must not rejoin the queue entry. The page must not name 05:00 UTC or who started the run. UK English. No em dash. Do not add a schedule setting or a third sign-in mode.

**Scale/Scope**: One snapshot per operator, one sign-in blob per hosted operator, one worker refresh token on that blob, one status blob per hosted account, one occasion marker per UTC date, one shared queue. The Repositories page is the only UI surface for schedule copy and for the worker-token refresh. No settings page and no retry button.

**UI layout**: FR-025 status and schedule sentences join the existing status `MudAlert` on `/repositories`. The worker-token refresh is a control on that page when the operator is signed in and the worker token is missing or the overnight scan failed because it cannot be used. Normative copy is in [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Before research

| Gate | Result |
| --- | --- |
| Principle I, layering | Pass, by approach. Eligibility, fill, status transitions, liveness, retries, and collection stay in Application. Blob and queue adapters stay in Infrastructure. Web and the Functions host call Application ports. Triggers do not call GitHub. Composition stays the application DI root. Aspire clients are registered in each host before `AddSoloDevBoard`. |
| Principle II, proportionality | Pass. No new project, no new storage account, and no new container for this revision. The worker token is two fields on the sign-in blob ADR-0049 already models. Liveness stays on `account-scan-status` so a 60-second write does not contend with that token. `collection-schedule` stays worker-only. No SQL, no HTTP API, no Durable Task hub, no second GitHub App. |
| Principle III, tests | Pass, by approach. Deterministic rules use `TimeProvider` and fakes. The 5-minute boundary, the 60-second cadence, and the worker-token outcomes are separate asserts. AppHost graph is not unit-tested. The Repositories guide is updated because operator-visible copy changes. |
| Principle IV, chrome | Pass, by approach. Same page, same alert, no new chrome. `mudblazor` skill at implementation. |
| Principle V, async and ServiceDefaults | Pass, by approach. New public async methods take `CancellationToken`. The Functions project already calls `AddServiceDefaults()`. |
| Principle VI, AppHost | Pass, with a required ADR amendment. The new container is `storage.AddBlobContainer("account-scan-status", "account-scan-status")` in both `ConfigureLocalRun` and `ConfigurePublishInfrastructure`, then `WithReference` from `web` and from the Functions project. No hardcoded endpoint URL. `web` scale stays minimum 0 and maximum 1. Lean graph is not edited. Amend ADR-0049 in the same change before treating the graph edit as done. Existing role assignments on the storage account already include blob data for the Functions identity. `web` keeps the access path it already uses for `hosted-sign-ins`. |
| Principle VII, lean path | Pass. PAT-only `aspire start` stays web-only. Hand collection remains. Copy explains the missing schedule and does not show a worker status or a worker-token ask. Hosted sign-in is the only hosted sign-in. The second GitHub authorisation is a step inside that sign-in, not a new mode. |
| ADR-0015, ADR-0046, and ADR-0048 | Pass, with a required ADR-0015 amendment. SQL is not reintroduced. ADR-0048's snapshot blob stays. ADR-0049 is not withdrawn. ADR-0015 stays GitHub App-only. The amendment records that overnight collection uses a second user authorisation on that same App, and that the cookie token is not the worker token. |

No unjustified violation.

### After design

Re-checked against [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), and [quickstart.md](quickstart.md).

| Gate | Result |
| --- | --- |
| Principle I | Still pass. Status transitions, the liveness rule, worker-token persistence, and the FR-025 sentences live in Application. Infrastructure stores blobs with an etag. Web starts the GitHub redirect and renders the sentences. It does not decide `stalled` or assemble its own status wording. |
| Principle II | Still pass. `scanGeneration` is a compare-and-swap on the one status blob. The worker-token write is a compare-and-swap on the sign-in blob. Neither is a new lock service. |
| Principle III | Still pass. Quickstart names the liveness boundaries and the worker-token cases, including schema 1 left unexchanged and a newer token left in place. |
| Principle VI | Still pass. No new container in this revision. `web` does not reference the queue or `collection-schedule`. No `WaitFor` between `web` and the worker. |
| Principle VII | Still pass. Quickstart requires a lean start with no Functions resource, no status container, no worker-token ask, and no fill at 05:00 UTC. |
| FR-022, FR-023, FR-024, and FR-025 | Recorded in [research.md](research.md), [data-model.md](data-model.md), and [contracts/](contracts/). `stalled` is reported only. A lapse does not write `finished` and does not enqueue `unmetFill`. The cookie token and the worker token are different grants. |

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
src/SoloDevBoard.Application/Services/Snapshots/   # status transitions, fill, pass, liveness, FR-025 sentences
src/SoloDevBoard.Application/Authentication/       # sign-in record; worker token fields; not the cookie token
src/SoloDevBoard.Infrastructure/Snapshots/         # status blob adapter, queue, schedule blobs
src/SoloDevBoard.Infrastructure/Identity/          # sign-in blob; worker-token rotation; no liveness writes
src/SoloDevBoard.Composition/                      # AspireHostedStorageReferenceNames.AccountScanStatus
src/SoloDevBoard.Web/Program.cs                    # keyed blob client; OAuth state purpose on the existing callback
src/SoloDevBoard.Web/Authentication/               # browser leg, worker leg, browser restore, worker-token refresh
src/SoloDevBoard.Web/Components/Features/Repositories/  # render FR-025 text; worker-token refresh control
src/SoloDevBoard.AppHost/LeanAppHostGraph.cs       # unchanged
src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs   # AddBlobContainer account-scan-status; WithReference on web and Functions
src/SoloDevBoard.AppHost/SoloDevBoardAppHost.cs    # keep web MinReplicas 0, MaxReplicas 1
src/SoloDevBoard.Functions/Program.cs              # keyed blob client, same resource name

tests/SoloDevBoard.Application.Tests/              # window, fill, status, liveness, worker-token outcomes
tests/SoloDevBoard.Infrastructure.Tests/           # etag loser does not overwrite; no token in the status blob
tests/SoloDevBoard.Web.Tests/                      # bUnit FR-025 sentences and the refresh control
website/content/docs/repositories.md               # shipped behaviour, same change
adr/adr-0049-scheduled-collection-functions.md     # binding record for the worker; not the token split
adr/adr-0015-github-app-first-hosted-authentication.md  # amend: second user authorisation, cookie token is not the worker token
```

**Structure Decision**: One Functions host project, already in the solution. Do not add an `apiservice`, a test project for the AppHost, a SQL project, or a second GitHub App. The status record and the FR-025 sentences are Application models. The worker token is fields on the existing sign-in record. Schedule rules, status transitions, and token persistence stay in Application so they can be tested without the Functions host and without the AppHost graph.

## Implementation notes

Scheduling and the 2026-11-25 account scan status are delivered behaviour this revision builds on. [tasks.md](tasks.md) is the previous delivery list. This command does not rewrite it. The open delta is FR-024 and FR-025. Do not treat a single `refreshToken` on the sign-in blob as the worker credential, and do not render 05:00 UTC or who started the run.

Order for this credential and copy delta. Leave the delivered status machine in place except where a step below changes a token outcome.

1. Move the sign-in blob to schema version 2. Add `workerRefreshToken` and `workerRefreshTokenExpiresAt`. Stop writing the browser refresh token onto that blob. On read, a schema 1 `refreshToken` is not the worker token: leave the worker token empty and do not exchange that value. Amend ADR-0015 in the same change. Leave `LeanAppHostGraph` untouched.
2. Split the existing GitHub App callback by OAuth state purpose. `browser` stores the cookie and completes sign-in (window and organisation list) without a worker token and without clearing `credentialUnusable`. It then sends the operator through the same App again as `worker-during-sign-in`. That second response is stored only when the browser access token already in the session is still accepted. If it is not, discard the candidate and run `browser-restore`, which replaces the cookie only. `worker-refresh`, started from `/repositories`, stores only the worker token. Cancel or failure of that refresh does not sign the operator out and does not record a scan failure.
3. The daily pass exchanges `workerRefreshToken` only. A missing token fails with the sign-in or token reason before the catalogue walk. Persist a rotated worker refresh token only while the stored value is still the token that scan started with. A newer stored token stays. If the token the scan started with cannot be used and a newer worker token is already stored, set `finished` with that failure, leave `suspensionReason` empty, and do not set `credentialUnusable`. If no newer token is stored, set `suspended`, set `credentialUnusable`, and do not retry. When a kept worker token clears that wait and the status is `suspended` for this failure, set `finished` and keep the failure result and comment.
4. Hand-started hosted collection keeps using the signed-in session access token. It does not read the worker token. A missing or unusable worker token does not refuse `StartAsync` unless the account is `scheduled` or callers are told `running`.
5. One Application formatter returns the FR-025 status sentence and the one schedule sentence. Web renders those strings. It does not format 05:00 UTC and it does not read `trigger` to say who started the run. The refresh control is on `/repositories` only. `website/content/docs/repositories.md` follows [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md). bUnit locks that copy.

Validation before a pull request is in [quickstart.md](quickstart.md).

## Complexity Tracking

No constitution violations require justification. The Functions project remains the spec's separate scheduled process. The worker credential is two fields on the sign-in blob, not a new authentication mode. ADR-0015 is amended in the same change. ADR-0049 remains the binding record for the worker.
