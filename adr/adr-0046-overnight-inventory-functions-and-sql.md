# ADR-0046: Overnight inventory via Azure Functions and Azure SQL

**Date:** 2026-11-15  
**Status:** Accepted

---

## Context

Cross-repository features such as Community Standards health ([#438](https://github.com/markheydon/solo-dev-board/issues/438)), FUNDING.yml checks ([#439](https://github.com/markheydon/solo-dev-board/issues/439)), and repository catalogue roll-ups require enumerating hundreds or thousands of repositories and issuing many per-repo GitHub API calls. Driving that work from Blazor Server page loads conflicts with circuit timeouts, scale-to-zero on Azure Container Apps, and GitHub rate limits.

The AppHost today models a single compute resource (`app`, the Blazor site) per [ADR-0018](adr-0018-aspire-azure-container-apps-deployment.md). [ADR-0016](adr-0016-consider-aspire-for-local-orchestration-and-future-hosting.md) names a second runtime as the revisit trigger. Planning notes in [`plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md`](../plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md) compared Azure Functions, scheduled Container App Jobs, blob storage, and SQL, but deliberately stopped short of a decision.

`SoloDevBoard.Composition` already exposes `AddSoloDevBoard` for any host so Application and Infrastructure GitHub logic is not duplicated in a worker.

---

## Decision

- Model **overnight and scheduled inventory** as a second Aspire compute resource: an **Azure Functions** isolated worker project (`functions`), deployed through `Aspire.Hosting.Azure.Functions` (Aspire 13.6.5 family).
- Persist **derived repository snapshots** in **Azure SQL Database** via `Aspire.Hosting.Azure.Sql`, with a database resource named `inventory`. Local development uses `RunAsContainer` with a named data volume (`inventory-sql-data`) so scan data survives container recreation; deploy uses Aspire's default General Purpose serverless SKU with free-tier auto-pause unless a future change customises provisioning.
- Provision **Azure Storage** for **Functions host storage only** (Azurite locally). Product snapshot rows live in SQL, not blobs or tables. The same storage account may later host queues for "scan now" without adding Service Bus.
- Keep the **Blazor Container App** as the only user-facing HTTP compute. It receives a **reference** to `inventory` for future read paths but does **not** call the Functions host over HTTP.
- **Do not** introduce an internal `apiservice` Container App for this phase. Both `app` and `functions` call `AddSoloDevBoard` and share Application/Infrastructure code (DRY).
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

- AppHost packages: `Aspire.Hosting.Azure.Functions`, `Aspire.Hosting.Azure.Sql`, `Aspire.Hosting.Azure.Storage` at 13.6.5 alongside existing Azure hosting packages.
- Staging resource names: reuse `AzureName` for SQL and Functions; storage account logical naming must stay alphanumeric (no hyphen suffix).
- Publish mode: Functions and web share Key Vault and Application Insights references where applicable.

---

## References

- [ADR-0016](adr-0016-consider-aspire-for-local-orchestration-and-future-hosting.md), [ADR-0018](adr-0018-aspire-azure-container-apps-deployment.md), [ADR-0043](adr-0043-key-vault-backed-hosted-auth-secrets.md)
- [`plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md`](../plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md)