# ADR-0047: OSS funding inventory topology (migrations, queue scan now, hosted installation auth)

**Date:** 2026-11-16  
**Status:** Accepted

---

## Context

[ADR-0046](adr-0046-overnight-inventory-functions-and-sql.md) accepted Azure Functions, Azure SQL `inventory`, and Functions host storage as the overnight inventory scaffold. It deliberately deferred EF schema, scanners, UI, storage-queue **scan now**, automated migration delivery, and hosted **GitHub App installation** credentials for unattended runs.

The first consumer is **OSS funding file hygiene** ([#439](https://github.com/markheydon/solo-dev-board/issues/439), [spec `001-oss-funding-hygiene`](../specs/001-oss-funding-hygiene/spec.md)). That work needs:

- Relational schema on `inventory` with EF Core while keeping `DbContext` in Infrastructure per layered architecture.
- **Scan now** acknowledged without waiting for a catalogue walk (SC-004) and without coupling to Blazor circuit lifetime.
- Reliable schema application in Azure before the web app and Functions worker serve traffic.
- Hosted sign-in inventories that run with nobody signed in (FR-015, FR-016), using installation tokens rather than the interactive OAuth session.

This ADR extends ADR-0046; it does **not** amend the accepted ADR-0046 decision body.

---

## Decision

- **DEC-001**: Add a dedicated class library **`SoloDevBoard.Inventory.Migrations`** that holds EF Core migration artefacts only. **`InventoryDbContext`** and entity configuration remain in **`SoloDevBoard.Infrastructure`**. AppHost uses Aspire **`AddEFMigrations`** with **`WithMigrationsProject`** pointing at the migrations assembly.
- **DEC-002**: Apply schema in Azure via the Aspire **EF migration bundle** executed as an **Azure Container Apps Job**, with **`RunDatabaseUpdateOnStart`** locally and **`WaitForCompletion`** on `app` and `functions` so neither host races an unmigrated database. Do not rely on silent `Migrate()` in the Blazor app alone for production.
- **DEC-003**: Implement **scan now** by enqueueing JSON messages on queue **`inventory-scan-request`** in the **same storage account** already provisioned for Functions host storage ([ADR-0046](adr-0046-overnight-inventory-functions-and-sql.md)). The **Web** app publishes through Aspire **`AddAzureQueueServiceClient`** with connection name **`inventory-queues`** (AppHost `storage.AddQueues("inventory-queues")`). The **Functions** worker processes messages with a **`QueueTrigger`** bound to **host storage** (`WithHostStorage`), not a separate connection string assembled in application code. Coalesce duplicate scan requests per operator while a run is queued or in progress (FR-005).
- **DEC-004**: Wire **hosted unattended GitHub access** for the Functions scanner via **`IGitHubInstallationTokenProvider`** in Infrastructure, using **`GitHubAuthOptions.GitHubAppId`** and **`GitHubAppPrivateKey`** bound from AppHost parameters and Key Vault on publish (alongside existing auth secrets per [ADR-0043](adr-0043-key-vault-backed-hosted-auth-secrets.md) and [ADR-0045](adr-0045-persistent-key-vault-dual-path-local-secrets.md)). **PAT mode** continues to use the same long-lived operator credential as the web app. Unattended inventory must **not** read or refresh the interactive sign-in session. Inventory scope remains the **linked open-source catalogue** (shared repository catalogue service), not the full installation repository list (FR-007a).

---

## Rationale

- **DEC-001**: A migrations-only project satisfies Aspire `WithMigrationsProject`, keeps Infrastructure as the domain/data layer, and avoids coupling deploy tooling to layer rules.
- **DEC-002**: A one-shot ACA Job surfaces migration failures before user-facing compute starts; web-only `Migrate()` hides races and breaks scale-to-zero assumptions.
- **DEC-003**: Reusing host storage for the queue avoids a second storage account and matches ADR-0046. Aspire queue client integration on Web satisfies constitution principle VI; Functions keeps the established host-storage trigger path.
- **DEC-004**: Installation JWT plus installation access tokens are the supported way to call repository APIs when no browser session exists. OAuth client credentials do not replace installation-scoped content access across the operator's linked repos.

---

## Consequences

### Positive

- **POS-001**: First inventory feature can ship SQL snapshots, scan now, and hosted overnight runs on the existing AppHost graph.
- **POS-002**: Schema changes are versioned, reviewable, and applied consistently in local and Azure environments.
- **POS-003**: Scan now meets fast-ACK goals without an internal HTTP API from Blazor to Functions.

### Negative

- **NEG-001**: Deployed environments gain an ACA migration Job and additional Key Vault secrets for the App private key where hosted inventory is enabled.
- **NEG-002**: Queue coalescing and dual triggers (timer plus queue) require careful orchestration to preserve a single active walk per operator.
- **NEG-003**: Operators must configure installation id and App credentials for hosted inventory; missing configuration blocks unattended runs by design (FR-015).

---

## Alternatives considered

### Migrations colocated in Infrastructure

- **ALT-001**: **Description**: Generate EF migrations inside `SoloDevBoard.Infrastructure`.
- **ALT-002**: **Rejection reason**: Couples migration CLI and publish bundles to the infrastructure layer; conflicts with Aspire `WithMigrationsProject` expectations and operator mental model for a dedicated migrations project.

### Scan now via long HTTP call or in-process Blazor job

- **ALT-001**: **Description**: Trigger inventory from the web process synchronously or over an internal Functions HTTP endpoint.
- **ALT-002**: **Rejection reason**: Violates SC-004, ties work to Blazor scale and circuits, and adds auth surface between compute resources.

### Startup-only `Database.Migrate()` on web

- **ALT-001**: **Description**: Apply migrations when the Container App starts.
- **ALT-002**: **Rejection reason**: Races with Functions on first deploy, obscures failure, and repeats on every replica scale event.

### Interactive session refresh token for hosted scans

- **ALT-001**: **Description**: Reuse the signed-in user's OAuth refresh token for overnight walks.
- **ALT-002**: **Rejection reason**: Spec forbids depending on a browser session; tokens expire and belong to the wrong trust boundary.

---

## Implementation notes

- **IMP-001**: Register `AddSqlServerDbContext<InventoryDbContext>("inventory")` in `AddSoloDevBoardHost`; Web adds `AddAzureQueueServiceClient("inventory-queues")` only; Functions does not register the queues client.
- **IMP-002**: AppHost: `inventory-scheduled-cron` parameter default `0 0 2 * * *` UTC; `gh-app-id`, `gh-app-private-key`, and installation id parameters for Functions and timer scope.
- **IMP-003**: Queue message contract: [inventory-queue-message.json](../specs/001-oss-funding-hygiene/contracts/inventory-queue-message.json); create queue if not exists on publish path.
- **IMP-004**: Document new Key Vault secret names and migration Job behaviour in deployment docs when implement lands.

---

## References

- **REF-001**: [ADR-0046](adr-0046-overnight-inventory-functions-and-sql.md) (parent scaffold)
- **REF-002**: [ADR-0044](adr-0044-github-api-response-caching-in-infrastructure.md) (catalogue cache for scope)
- **REF-003**: [spec `001-oss-funding-hygiene` plan](../specs/001-oss-funding-hygiene/plan.md), [research.md](../specs/001-oss-funding-hygiene/research.md)
- **REF-004**: [Aspire EF migrations / AddEFMigrations](https://aspire.dev/integrations/databases/efcore/migrations/#automated-ef-migrations-with-addefmigrations), [Aspire Azure Queue Storage client](https://aspire.dev/integrations/cloud/azure/azure-storage-queues-connect/)