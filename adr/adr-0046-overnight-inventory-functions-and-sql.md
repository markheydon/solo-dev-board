# ADR-0046: Overnight inventory via Azure Functions and Azure SQL

**Date:** 2026-11-15  
**Status:** Withdrawn (2026-11-20: Azure SQL inventory stack not implemented; overnight inventory and application persistence under reconsideration)

## Status history

| Date | Status | Notes |
| --- | --- | --- |
| 2026-11-15 | Accepted | Chosen Azure Functions worker plus Azure SQL `inventory` |
| 2026-11-17 | Accepted | Addenda: lean vs full graph posture; Blazor resource renamed `web` per [ADR-0047](adr-0047-apphost-blazor-resource-name-web.md) |
| 2026-11-20 | Withdrawn | AppHost never modelled SQL, Functions, or inventory storage (subscription provisioning limits and deliberate deferral). Persistence for derived snapshots is **not decided**; may abandon an application database entirely. **Do not implement** this ADR. A future ADR may record a replacement approach without reinstating this document as binding. |

The sections below record the accepted rationale as of 2026-11-15 for **historical context only**. They are **not** binding after withdrawal.

### Supersession of implementation addenda (2026-11-20)

The **Implementation notes** and addenda through **2026-11-17** described an AppHost graph (SQL, Functions, storage) that was **never shipped**. The live AppHost models **`web` only** in both postures ([`LeanAppHostGraph`](../src/SoloDevBoard.AppHost/LeanAppHostGraph.cs) and [`HostedSignInAppHostGraph`](../src/SoloDevBoard.AppHost/HostedSignInAppHostGraph.cs)). **`hosted-sign-in-enabled`** selects hosted OAuth wiring locally; it does **not** add SQL, Functions, or storage. **Do not implement** the graph described in those addenda.

---

## Context

Cross-repository features such as Community Standards health ([#438](https://github.com/markheydon/solo-dev-board/issues/438)), FUNDING.yml checks ([#439](https://github.com/markheydon/solo-dev-board/issues/439)), and repository catalogue roll-ups require enumerating hundreds or thousands of repositories and issuing many per-repo GitHub API calls. Driving that work from Blazor Server page loads conflicts with circuit timeouts, scale-to-zero on Azure Container Apps, and GitHub rate limits.

The AppHost today models a single compute resource (`web`, the Blazor site; [ADR-0047](adr-0047-apphost-blazor-resource-name-web.md)) per [ADR-0018](adr-0018-aspire-azure-container-apps-deployment.md). [ADR-0016](adr-0016-consider-aspire-for-local-orchestration-and-future-hosting.md) names a second runtime as the revisit trigger. Planning notes in [`plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md`](../plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md) compared Azure Functions, scheduled Container App Jobs, blob storage, and SQL, but deliberately stopped short of a decision.

`SoloDevBoard.Composition` already exposes `AddSoloDevBoard` for any host so Application and Infrastructure GitHub logic is not duplicated in a worker.

---

## Decision

- Model **overnight and scheduled inventory** as a second Aspire compute resource: an **Azure Functions** isolated worker project (`functions`), deployed through `Aspire.Hosting.Azure.Functions` (Aspire 13.6.5 family).
- Persist **derived repository snapshots** in **Azure SQL Database** via `Aspire.Hosting.Azure.Sql`, with a database resource named `inventory`. Local development uses `RunAsContainer` with a named data volume (`inventory-sql-data`) so scan data survives container recreation; deploy uses Aspire's default General Purpose serverless SKU with free-tier auto-pause unless a future change customises provisioning.
- Provision **Azure Storage** for **Functions host storage only** (Azurite locally). Product snapshot rows live in SQL, not blobs or tables. The same storage account may later host queues for "scan now" without adding Service Bus.
- Keep the **Blazor Container App** as the only user-facing HTTP compute. It receives a **reference** to `inventory` for future read paths but does **not** call the Functions host over HTTP.
- **Do not** introduce an internal `apiservice` Container App for this phase. The first inventory scanner will call `AddSoloDevBoard` from the Functions host so Application/Infrastructure GitHub logic stays DRY; the enabler timer placeholder does not register that graph yet.
- **Do not** add scanners, EF Core schema, or UI in this decision; those ship with the first feature that writes or reads `inventory`.

Non-interactive GitHub credentials for the worker match the web PAT path today (`gh-pat` / Key Vault). Hosted **installation** tokens (`GitHubAppId` / `GitHubAppPrivateKey`) are not AppHost parameters yet; hosted overnight scans remain blocked until that is specified and wired.

---

## Rationale

- **Functions + timer (and later queue) triggers** fit daily scans and on-demand enqueue without pinning the Blazor replica or fighting scale-to-zero ([Aspire Azure Functions integration](https://aspire.dev/integrations/cloud/azure/azure-functions/azure-functions-get-started/)).
- **Azure SQL** supports filtered reporting and bulk UI actions across many repos; GitHub remains the system of record for live mutations.
- **Composition reuse** avoids a parallel GitHub client or premature HTTP API split.
- **Scheduled Container App Job** was rejected for the first slice: Functions host storage, dashboard-first-class lifecycle, and a path to queue triggers outweigh a single nightly console with no host integration.

---

## Consequences

### Positive

- AppHost graph matches future ice-box features without live catalogue fan-out in the UI.
- Local `aspire start` runs SQL (container), storage (emulator), web, and Functions together for integration testing.
- Deploy adds predictable Azure SKUs documented in `docs/azure-costs.md`.

### Negative

- Deployed environments gain SQL, storage, and a Functions app; idle cost rises modestly versus web-only.
- Hosted scanners cannot run until non-interactive GitHub App installation auth is designed.
- Web SQL health checks are deferred until the first consumer uses `inventory`, so `/health` does not fail on an empty database.

---

## Alternatives considered

| Option | Rejection |
|--------|-----------|
| Scheduled Container App Job only | No first-class timer/queue integration; host storage and Durable paths weaker for later fan-out. |
| Cosmos / blob snapshots only | Poor fit for cross-repo filters and roll-up queries planned for the catalogue. |
| `apiservice` before any worker | Extra cold start, token forwarding, and duplicated deployment without a second HTTP client yet. |
| Long-running `IHostedService` in Blazor | Fights ACA scale-to-zero and couples job failures to the interactive site. |

---

## Implementation notes

> **Historical only (withdrawn):** None of the following was implemented in AppHost. See [Supersession of implementation addenda](#supersession-of-implementation-addenda-2026-11-20).

- AppHost packages: `Aspire.Hosting.Azure.Functions`, `Aspire.Hosting.Azure.Sql`, `Aspire.Hosting.Azure.Storage` at 13.6.5 alongside existing Azure hosting packages.
- Staging resource names: reuse `AzureName` for SQL and Functions; storage account logical naming must stay alphanumeric (no hyphen suffix).
- Publish mode: Functions and web share Key Vault and Application Insights references where applicable.

### Client integrations and registration (addendum, 2026-11-17)

Governance for wiring lives in constitution Principle VI; this addendum names packages and host entry points only.

**AppHost (hosting only, `src/SoloDevBoard.AppHost/`):**

- Graph and references (never shipped): a former sketch named `FullExperienceAppHostGraph` would have declared `inventory`, `storage`, `functions`, and `web` with SQL references. The shipped graph uses **`HostedSignInAppHostGraph`** for hosted sign-in and models **`web` only** alongside `LeanAppHostGraph`.
- Hosting packages stay on the 13.6.5 family in `Directory.Packages.props`; do not add client integration packages to the AppHost project.

**Consuming compute hosts (client packages, `Program.cs` only):**

- Register Aspire SQL clients in each host that needs `inventory`, **after** `AddServiceDefaults()` and **before** `AddSoloDevBoard()` when the worker graph is enabled:
  - **ADO / `DbDataSource`:** NuGet `Aspire.Microsoft.Data.SqlClient` (same release family as AppHost). Call `builder.AddSqlServerClient(connectionName: "inventory")` (or `AddKeyedSqlServerClient` if multiple databases appear later). Prefer constructor injection of `SqlConnection`, `DbDataSource`, or a thin factory registered atop the Aspire client; do not read `ConnectionStrings:inventory` by hand in Application or Infrastructure.
  - **EF Core (first schema / migrations slice):** NuGet `Aspire.Microsoft.EntityFrameworkCore.SqlServer`. Call `builder.AddSqlServerDbContext<TContext>(connectionName: "inventory", ...)` with migrations assembly configured in the host or a dedicated migrations project; keep `DbContext` types in Infrastructure, registration in the host only.
- **Azure Functions host storage:** No extra client package for the Functions **host** runtime when using `WithHostStorage(storage)`; Aspire injects the storage connection for the worker host. Queue/blob **application** clients (future “scan now”) would use the matching `Aspire.Azure.Storage.*` client package in `SoloDevBoard.Functions/Program.cs` with `connectionName` equal to the AppHost storage resource name (`storage` today), not hand-built connection strings.
- **Web:** `SoloDevBoard.Web/Program.cs` today has `AddServiceDefaults()` only; add `AddSqlServerClient` / `AddSqlServerDbContext` here when the first read or write path ships, not in `SoloDevBoard.Composition`.
- **Tests:** Unit and integration tests MAY use containers, fakes, or test-specific `WebApplicationFactory` configuration; production `Program.cs` paths MUST use the integrations above.

**Layering:**

- `SoloDevBoard.Composition` (`AddSoloDevBoard`) remains the application DI root for domain services; it MUST NOT reference `Aspire.*` client packages.
- Application and Infrastructure consume SQL only through types registered by the host (interfaces and factories defined in Application, implemented in Infrastructure).

### Local graph posture (addendum, 2026-11-17; superseded 2026-11-20)

> **Superseded:** The SQL/Functions/storage split by `hosted-sign-in-enabled` was never implemented. Current posture is documented in constitution v1.1.2 Principle VII and [Supersession of implementation addenda](#supersession-of-implementation-addenda-2026-11-20).

### Blazor resource name (addendum, 2026-11-20)

Per [ADR-0047](adr-0047-apphost-blazor-resource-name-web.md), the Blazor `AddDotnetProject` logical name is **`web`** (supersedes earlier `app` naming in this ADR's implementation notes). Both lean and hosted-sign-in graphs model **`web` only**; there is no `WithReference(inventory)` in AppHost while this ADR remains withdrawn.

---

## References

- [ADR-0016](adr-0016-consider-aspire-for-local-orchestration-and-future-hosting.md), [ADR-0018](adr-0018-aspire-azure-container-apps-deployment.md), [ADR-0043](adr-0043-key-vault-backed-hosted-auth-secrets.md), [ADR-0047](adr-0047-apphost-blazor-resource-name-web.md)
- [`plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md`](../plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md)