# Implementation Plan: Scheduled snapshot collection

**Branch**: `002-scheduled-snapshot-collection` | **Date**: 2026-11-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-scheduled-snapshot-collection/spec.md`

## Summary

In hosted sign-in, queue the existing operator snapshot collection once each UTC day at 05:00, with a 15-minute grace if the site is down at 05:00:00, and run one daily collection at a time. The same snapshot, failure reasons, and hand-started control stay. Lean PAT-only local trusted mode does not gain a schedule.

The scheduler is a hosted service inside the existing `web` project. It is not an Azure Function, not a Container Apps Job, and not a new AppHost resource. Hosted durability reuses the ADR-0048 `operator-snapshots` container for a schedule prefix, protected refresh tokens, and a shared data-protection key. A binding ADR must be accepted before that secret-bearing store is implemented. Eligibility is read from sign-in records written at completed hosted sign-in, not by scanning allow lists or calling GitHub for membership.

## Technical Context

**Language/Version**: C# 14, .NET 10 (`net10.0`)

**Primary Dependencies**: Existing Blazor Server host, Application snapshot collection, Infrastructure blob and file snapshot stores, Aspire 17.0.0 hosted sign-in graph (`operator-snapshot-storage` / `operator-snapshots`). ASP.NET Core Data Protection (shared framework) plus `Azure.Extensions.AspNetCore.DataProtection.Blobs` to persist the key ring in that container. No Azure Functions worker, no EF Core, no SQL.

**Storage**: Hosted sign-in: existing blob container, additive prefix `schedule/`. Lean posture: existing file snapshot under `{content root}/data/operator-snapshots/` and no schedule documents. No new database.

**Testing**: xUnit v3 (Microsoft.Testing.Platform), NSubstitute, built-in `Assert`, `TimeProvider`. bUnit in `SoloDevBoard.Web.Tests` for Repositories copy and the start control. AppHost resource graph is not unit-tested. Playwright only if an existing repositories command-strip or alert assertion changes.

**Target Platform**: ASP.NET Core web app on the Aspire `web` resource (local `aspire start` and Azure Container Apps).

**Project Type**: Blazor Server web application with layered Domain, Application, Infrastructure, Composition, and Web projects.

**Performance Goals**: After 05:00 UTC the queue starts the next eligible collection within one scheduler tick (15 seconds) of the previous daily collection finishing. Filling the queue enumerates stored sign-in records only.

**Constraints**: Fixed 05:00 UTC, 15-minute fill grace, 30-day sign-in window, at most 3 further tries, one daily collection at a time across replicas, one snapshot per operator. Lean graph stays web-only. UK English. No em dash. Public Application surfaces return DTOs. Async APIs take `CancellationToken`. Clock reads go through `TimeProvider`.

**Scale/Scope**: One queue document per occasion and one sign-in record per operator who has completed hosted sign-in. The allow list is not the scan set. Repositories page is the only operator surface. No settings page.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Result |
| --- | --- |
| I. Layered architecture | Pass. Eligibility, queue rules, and occasion text live in Application. Blob and data-protection adapters live in Infrastructure. Web hosts the service, renders DTOs, and does not reference Infrastructure. |
| II. Proportional design and DTO boundary | Pass. No new project, no second scanner, no SQL. New public shapes are Application DTO records. Web does not see domain entities or `IGitHubService`. |
| III. Tests and guides | Pass. Behaviour is locked by unit and bUnit tests. `website/content/docs/repositories.md` changes in the same change because it currently says collection is never scheduled. AppHost graph is not unit-tested. |
| IV. Chrome and accessibility | Pass. Same `/repositories` page, one alert, existing **Collect snapshot** control. No new settings page, FAB, or chart. |
| V. Async I/O and ServiceDefaults | Pass. New public async methods take `CancellationToken`. No new compute project, so ServiceDefaults stays on `web`. |
| VI. Single AppHost model | Pass. No new resource, parameter, or `WithEnvironment` URL. Hosted `web` already has `WithReference` to `operator-snapshots`. Lean graph is unchanged. |
| VII. Lean degradation | Pass. PAT-only local trusted mode keeps on-demand collection and explains that nothing collects on a schedule and that the snapshot stays on this machine. |
| Secrets and ADRs | Conditional pass. Durable refresh tokens are a material security choice. Implementation must not start until a binding ADR (next number, expected `adr-0049`) is accepted. That ADR extends ADR-0048; it does not revive withdrawn ADR-0046. Adding `Azure.Extensions.AspNetCore.DataProtection.Blobs` needs an explicit package approval at implementation time (`AGENTS.md`). |

Post-design re-check: the same gates hold. Research rejected a second compute resource, so Principle VI does not require an AppHost binding for a new resource. The token-store ADR is still mandatory before code that writes refresh tokens.

## Project Structure

### Documentation (this feature)

```text
specs/002-scheduled-snapshot-collection/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── daily-collection-schedule.md
│   ├── hosted-sign-in-record.md
│   └── repositories-schedule-copy.md
└── tasks.md             # /speckit-tasks, not this command
```

### Source Code (repository root)

```text
src/SoloDevBoard.Application/Services/Snapshots/
src/SoloDevBoard.Infrastructure/Snapshots/
src/SoloDevBoard.Composition/
src/SoloDevBoard.Web/Authentication/
src/SoloDevBoard.Web/Components/Features/Repositories/
src/SoloDevBoard.Web/                  # hosted service registration only when hosted sign-in is on
src/SoloDevBoard.AppHost/              # no resource change; lean and hosted graphs stay as ADR-0048
tests/SoloDevBoard.Application.Tests/
tests/SoloDevBoard.Infrastructure.Tests/
tests/SoloDevBoard.Web.Tests/
website/content/docs/repositories.md
```

**Structure Decision**: Extend the operator snapshot feature inside the existing layered web app. `SoloDevBoard.Functions` stays unwired and is not the scheduler. AppHost authoring stays in `HostedSignInAppHostGraph` and `LeanAppHostGraph`; do not edit generated Aspire modules (this repo has none).

## Complexity Tracking

No constitution violation requires a row. The binding ADR for protected refresh tokens is a security gate, not an extra project or a second orchestration graph.

## Phase 0 and Phase 1 outputs

- [research.md](research.md) resolves the scheduler host, the store, multi-replica exclusion, the sign-in window, retries, and the interruption rule that must not treat a queued retry as a dead walk.
- [data-model.md](data-model.md) defines the sign-in record, the occasion queue, and the additive `startedBy` field on the existing snapshot run.
- [contracts/](contracts/) define the Application schedule surface, the sign-in record, and the Repositories copy.
- [quickstart.md](quickstart.md) is the validation guide. It names behaviours and test projects. It does not contain implementation bodies.
