# ADR-0047: AppHost Blazor compute resource name `web`

**Date:** 2026-11-20  
**Status:** Accepted

---

## Context

The AppHost models the Blazor Server project (`SoloDevBoard.Web`) as a single Aspire compute resource. That resource was named `app` from the first Aspire wiring ([ADR-0018](adr-0018-aspire-azure-container-apps-deployment.md), [ADR-0021](adr-0021-two-tier-cd-pipeline.md), [ADR-0046](adr-0046-overnight-inventory-functions-and-sql.md)). Upcoming inventory work adds `functions`, `inventory`, and `storage` alongside user-facing HTTP compute. Aspire and aspireify guidance prefer **role-based** resource names (`api`, `worker`, `web`) over generic `app`, which reads as “the whole product” once multiple compute roles exist.

Staging and production Azure Container App names follow the logical Aspire name via `AppHostAzureNaming` (`web` / `web-staging`).

---

## Decision

- Rename the Blazor `AddDotnetProject` logical resource from **`app`** to **`web`**.
- Staging deploy suffix: **`web-staging`** (replacing `app-staging`). Production logical name **`web`** (replacing `app`).
- Leave **`app-insights`** unchanged (Azure Application Insights resource; not the compute role).
- Operators migrating existing Azure deployments MUST treat this as a **new** Container App identity in each tier (see [docs/deployment.md](../docs/deployment.md)); do not assume an in-place rename of ACA `app`.

Reject keeping generic `app` for the multi-compute graph and reject `webfrontend` unless a second HTTP API compute (`apiservice`) is introduced later ([ADR-0046](adr-0046-overnight-inventory-functions-and-sql.md) rejects that split for the inventory phase).

---

## Consequences

### Positive

- Dashboard, `aspire describe`, `aspire wait web`, and operator docs align with project folder `SoloDevBoard.Web` and with `functions` as a sibling role.
- Service discovery env vars use `services__web__*` if another resource gains `WithReference(web)` later.

### Negative

- Production and staging ACA resources named `app` / `app-staging` are replaced on the next successful deploy after this change; operators update Kusto filters and runbooks.
- Historical ADRs and archive notes that say `app` remain accurate for periods before this ADR; addenda on [ADR-0021](adr-0021-two-tier-cd-pipeline.md) and [ADR-0046](adr-0046-overnight-inventory-functions-and-sql.md) record the superseded names.

---

## Alternatives considered

| Option | Rejection |
|--------|-----------|
| Keep `app` | Weak role clarity when `functions` ships; acceptable only while a single compute resource exists. |
| `webfrontend` | Starter-template name; verbose and implies a separate `apiservice`, which ADR-0046 defers. |
| `blazor` | Non-standard across Aspire graphs; couples hosting name to UI framework. |