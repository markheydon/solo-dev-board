# Implementation Plan: OSS funding file hygiene

**Branch**: `001-oss-funding-hygiene` | **Date**: 2026-11-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-oss-funding-hygiene/spec.md`, plus user plan input: production **daily overnight** inventory with **configurable schedule** for future higher frequency, **`SoloDevBoard.Inventory.Migrations`** project, and **`AddEFMigrations`** in AppHost per [Aspire automated EF migrations](https://aspire.dev/integrations/databases/efcore/migrations/#automated-ef-migrations-with-addefmigrations).

## Summary

Deliver the first overnight **open-source inventory** and an **Open Source** page whose opening section shows **FUNDING.yml hygiene** from stored SQL snapshots, not live GitHub calls. The same pass stores Community Standards as in-repository versus owner-`.github`-only facts for a later view. The page does not show those facts. Persist inventory data in the existing Aspire **`inventory`** database ([ADR-0046](../../adr/adr-0046-overnight-inventory-functions-and-sql.md)). Run collection from **`SoloDevBoard.Functions`** on a **configurable UTC CRON** timer (default 02:00 daily) and on **scan now** via a storage queue. Apply schema through **`AddEFMigrations`** with a dedicated migrations project. Wire hosted **installation tokens** for unattended scans where PAT mode is not in use.

## Technical Context

**Language/Version**: C# 14 / .NET 10.0 LTS (`net10.0`)

**Primary Dependencies**: Blazor Server, MudBlazor, EF Core 10 + `Aspire.Microsoft.EntityFrameworkCore.SqlServer`, `Aspire.Azure.Storage.Queues` (Web enqueue), Aspire AppHost 13.6.5 (`Aspire.Hosting.EntityFrameworkCore`, Azure Functions/Sql/Storage hosting), Azure Functions isolated worker, existing `AddSoloDevBoard` / `AddSoloDevBoardHost` composition

**Storage**: Azure SQL `inventory` (local container + volume `inventory-sql-data`); Azure Storage for Functions host + `inventory-scan-request` queue

**Testing**: xUnit v3, NSubstitute, bUnit for Open Source page components; Application unit tests for funding comparison and orchestration; no AppHost unit tests (constitution III)

**Target Platform**: Blazor Server on Azure Container Apps; Functions for timer/queue; ACA Job for published EF migration bundle

**Project Type**: Layered web application with Aspire orchestration (existing `src/` layout)

**Performance Goals**: Open Source page ≤ 5 s with ≤ 500 rows from SQL only (SC-002); scan now ACK ≤ 5 s (SC-004)

**Constraints**: Layered boundaries (Web → Application DTOs only); UK English UI; wireframe before build; 36 h staleness window; single active run per operator; Functions `functionTimeout` default 10 minutes with 120 s orchestrator margin (spec US3 scenario 4)

**Scale/Scope**: Hundreds of open-source repos per operator; Community Standards dual facts (in-repository and organisation-default only) stored but not displayed

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Assessment |
|-----------|--------------|
| I. Layered architecture | **Pass** when Web and Functions call `AddSoloDevBoardHost` (client integrations, then `AddSoloDevBoard`). Web `Program.cs` does not reference Infrastructure types. |
| II. DTO boundaries | **Pass**: Application DTO records per [contracts/application-services.md](./contracts/application-services.md); Domain entity records under `Domain/Entities/Inventory/`. |
| III. Testing | **Pass** when unit tests, `CRITICAL_JOURNEYS.md`, Playwright journey, and user guide ship with the Open Source page (constitution III). |
| IV. Chrome and UX | **Pass** with **wireframe debt**: add [plan/wireframes/open-source-wireframe.md](../../plan/wireframes/open-source-wireframe.md) before implement. |
| V. I/O and observability | **Pass**: Async GitHub + SQL; ServiceDefaults on Functions; no catalogue fan-out on page load. Queue and SQL client integrations supply health checks and traces. |
| VI. Aspire client integrations | **Pass** when SQL uses `AddSqlServerDbContext<InventoryDbContext>("inventory")` and Web enqueue uses `AddAzureQueueServiceClient("inventory-queues")`, both in the host composition root, with `connectionName` equal to the AppHost resource. Consumers take `InventoryDbContext` and `QueueServiceClient` from DI. Functions queue trigger uses `WithHostStorage` (default host storage connection). No `GetConnectionString`, env-var assembly, or `new QueueClient(connectionString)` in `src/`. |

**Post-design re-check (constitution 1.1.1, 2026-11-16)**: No unjustified violations. Dual Community Standards columns stay on the existing snapshot entity in Infrastructure. The Open Source page DTOs stay funding-only. Fork and security-policy file probes stay inside the scanner. New projects (`SoloDevBoard.Inventory.Migrations`) and queue coalescing remain proportionate to spec and ADR-0046. Principle VI (amended after the first plan) keeps relational access on the existing EF client integration and moves scan-now publish onto the Azure Queue Storage client integration. The Functions trigger stays on the Aspire Functions host-storage binding, which is the worker's connection to the same storage account.

## Project Structure

### Documentation (this feature)

```text
specs/001-oss-funding-hygiene/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1 validation guide
├── contracts/           # Application, queue, AppHost bindings
└── tasks.md             # Phase 2 (/speckit-tasks - not created by plan)
```

### Source Code (repository root)

```text
src/
├── SoloDevBoard.Domain/
│   └── Entities/Inventory/             # InventoryRun, RepositoryInventorySnapshot, CanonicalFundingStandard
├── SoloDevBoard.Application/
│   └── Services/OpenSource/            # IOpenSourceFundingService, scanner orchestration
├── SoloDevBoard.Infrastructure/
│   └── Inventory/                      # InventoryDbContext, repositories, GitHub scanner
├── SoloDevBoard.Inventory.Migrations/ # EF migrations assembly (new)
├── SoloDevBoard.Composition/           # AddSoloDevBoardHost: SQL + optional queue client; AddSoloDevBoard for services
├── SoloDevBoard.Web/
│   └── Components/Features/OpenSource/ # Page + canonical standard editor
├── SoloDevBoard.Functions/
│   ├── InventoryFunctionsWorker.cs     # Shared orchestrator entry
│   ├── InventoryTimerFunction.cs       # Configurable CRON
│   └── InventoryQueueFunction.cs       # Scan now processor
└── SoloDevBoard.AppHost/
    └── AppHost.cs                      # AddEFMigrations, schedule param, gh-app secrets

tests/
├── SoloDevBoard.Application.Tests/OpenSource/
├── SoloDevBoard.Infrastructure.Tests/Inventory/
└── SoloDevBoard.Web.Tests/…/OpenSource/

plan/wireframes/
└── open-source-wireframe.md            # Create before UI implement
```

**Structure Decision**: Keep `DbContext` in Infrastructure (ADR layering). Migrations live only in `SoloDevBoard.Inventory.Migrations` for `WithMigrationsProject` and user expectation. Reuse existing Functions and SQL resources from ADR-0046 scaffold.

## Phase 0 & 1 outputs (complete)

| Artifact | Path |
|----------|------|
| Research | [research.md](./research.md) |
| Data model | [data-model.md](./data-model.md) |
| Application contracts | [contracts/application-services.md](./contracts/application-services.md) |
| Queue contract | [contracts/inventory-queue-message.json](./contracts/inventory-queue-message.json) |
| AppHost parameters | [contracts/apphost-inventory-parameters.md](./contracts/apphost-inventory-parameters.md) |
| Validation guide | [quickstart.md](./quickstart.md) |

## Implementation phases (for tasks.md)

### Phase A - Data and Aspire migrations

1. Add `SoloDevBoard.Inventory.Migrations` project; EF packages via central versioning.  
2. Implement `InventoryDbContext` and entity configuration in Infrastructure.  
3. AppHost: `Aspire.Hosting.EntityFrameworkCore`; `AddEFMigrations` on `app` with `RunDatabaseUpdateOnStart`, publish bundle + ACA Job; `WaitForCompletion` on `app` and `functions`.  
4. AppHost: `storage.AddQueues("inventory-queues")`; Web `WithReference` + `WaitFor` that queues resource (before scan-now is validated in Phase 2 checkpoint).  
5. In `AddSoloDevBoardHost`, call `AddSqlServerDbContext<InventoryDbContext>("inventory")` (name matches the AppHost database), then `AddSoloDevBoard` for repositories and Application services. Web and Functions call `AddSoloDevBoardHost`. Web `Program.cs` also calls a sibling on the same composition type, `AddAzureQueueServiceClient("inventory-queues")`, so Functions does not register a queues client it does not reference. No Infrastructure types in Web `Program.cs`. Initial migration for entities in [data-model.md](./data-model.md).

### Phase B - GitHub scanner and Functions

1. `IGitHubInstallationTokenProvider` + AppHost `gh-app-id` / `gh-app-private-key` (Key Vault on publish).  
2. Replace placeholder timer with `%Inventory__ScheduledCron%`; AppHost parameter `inventory-scheduled-cron`.  
3. `IInventoryScanQueue` publishes JSON (base64-encoded by default SDK) with `CreateIfNotExists` on queue `inventory-scan-request`. Functions `[QueueTrigger]` uses host storage from `WithHostStorage` and does not set `Connection` to `ConnectionStrings:storage`. Coalesce per operator.  
4. Funding file fetch plus community-profile classification (Contents API for security policy and forks); persist snapshots and the dual checklist facts in [data-model.md](./data-model.md).

### Phase C - Web Open Source page

1. Wireframe under `plan/wireframes/`.  
2. Application services per contracts; nav entry.  
3. MudBlazor page: summary, table, scan now, standard editor, stale/partial/empty states (UK English).  
4. Docs, `tests/SoloDevBoard.E2E.Tests/CRITICAL_JOURNEYS.md`, and Playwright coverage with the page (constitution III).

## Complexity Tracking

> Constitution requires an ADR before implement for EF migrations topology, storage-queue scan now, and GitHub App installation auth wiring (extends [ADR-0046](../../adr/adr-0046-overnight-inventory-functions-and-sql.md); do not append status to the accepted ADR). Task T004a tracks that record.

| Addition | Why needed | Simpler alternative rejected |
|----------|------------|-------------------------------|
| `SoloDevBoard.Inventory.Migrations` | User requirement + `WithMigrationsProject` | Migrations colocated in Infrastructure couples deploy tooling to layer rules |
| Storage queue for scan now | SC-004 fast ACK + decouple from Blazor. Publish via `AddAzureQueueServiceClient` (constitution VI) | Long HTTP call, in-process job, or `GetConnectionString` plus `new QueueClient` |
| ACA Job migration bundle | Reliable schema before web/Functions in Azure | Startup `Migrate()` only on web races and hides failures |

## References

- [ADR-0046 Overnight inventory via Azure Functions and Azure SQL](../../adr/adr-0046-overnight-inventory-functions-and-sql.md)  
- [GitHub issue #439](https://github.com/markheydon/solo-dev-board/issues/439)  
- [Aspire: Apply EF Core migrations / AddEFMigrations](https://aspire.dev/integrations/databases/efcore/migrations/#automated-ef-migrations-with-addefmigrations)  
- [Aspire: Connect to Azure Queue Storage](https://aspire.dev/integrations/cloud/azure/azure-storage-queues-connect/) (`AddAzureQueueServiceClient`)  
- Constitution 1.1.1 principle VI (client integration + DI; amended 2026-11-16)  
- [AppHost README](../../src/SoloDevBoard.AppHost/README.md)