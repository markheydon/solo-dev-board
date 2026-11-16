# Research: OSS funding file hygiene

**Feature**: `specs/001-oss-funding-hygiene`  
**Date**: 2026-11-16

Phase 0 decisions for `/speckit-plan`. User input emphasises a **daily overnight production schedule** with room to run **more often later**, a **`SoloDevBoard.Inventory.Migrations` project**, and **`AddEFMigrations`** in the AppHost per [Aspire EF Core migrations](https://aspire.dev/integrations/databases/efcore/migrations/#automated-ef-migrations-with-addefmigrations).

---

## Persistence and Aspire orchestration

**Decision**: Add EF Core against the existing AppHost `inventory` Azure SQL resource ([ADR-0046](../../adr/adr-0046-overnight-inventory-functions-and-sql.md)). Register `InventoryDbContext` in `SoloDevBoard.Infrastructure` (implements Application repository interfaces). Place generated migration C# files in a new **`SoloDevBoard.Inventory.Migrations`** class library that references Infrastructure (migrations assembly only).

**Decision**: Wire automated migrations in `AppHost.cs` using **`AddEFMigrations`** on the `app` (Web) resource, with **`.WithMigrationsProject<Projects.SoloDevBoard_Inventory_Migrations>()`**, **`.WithReference(inventory)`**, **`.WaitFor(inventory)`**, **`.RunDatabaseUpdateOnStart()`** for local `aspire start`, and **`.PublishAsMigrationBundle(publishContainer: true).PublishAsAzureContainerAppJob()`** for deploy (idempotent bundle, run once as an ACA Job before web and Functions scale up).

**Rationale**: Matches the user's expectation and Aspire's first-class migration resource without maintaining a long-running migration worker. `RunDatabaseUpdateOnStart` applies schema locally; the published bundle job covers Azure where the migration resource is not deployed with the app.

**Alternatives considered**:

| Alternative | Rejection |
|-------------|-----------|
| Dedicated `MigrationService` worker (Aspire tutorial pattern) | Extra always-on resource; `AddEFMigrations` is the preferred Aspire 13.6 path and matches user input. |
| `Migrate()` at Web startup in production | Hides failures in the web replica, races with Functions on first deploy, and bypasses observable migration health in the dashboard. |
| Migrations inside Infrastructure next to `DbContext` | Valid for small teams; rejected here because the user explicitly asked for a migrations project and AppHost `WithMigrationsProject`. |

**Packages** (implementation phase, central versions in `Directory.Packages.props`): `Aspire.Hosting.EntityFrameworkCore` on AppHost; `Aspire.Microsoft.EntityFrameworkCore.SqlServer` on the composition host that calls `AddSqlServerDbContext`; `Aspire.Azure.Storage.Queues` on the composition host that calls `AddAzureQueueServiceClient`; `Microsoft.EntityFrameworkCore.Design` on Migrations project.

---

## Aspire client integrations (constitution 1.1.1, principle VI)

**Decision**: Register modelled-resource clients in the host composition root `AddSoloDevBoardHost` (and a Web-only sibling on the same type when the worker must not take the dependency).

- Inventory SQL: `builder.AddSqlServerDbContext<InventoryDbContext>("inventory")`. The name is the AppHost database resource. Web and Functions both call this, because both `WithReference(inventory)`. Repositories inject `InventoryDbContext`.
- Scan-now publish: AppHost adds `storage.AddQueues("inventory-queues")` on the existing storage account. Web `WithReference` and `WaitFor` that queues resource. Web calls `builder.AddAzureQueueServiceClient("inventory-queues")`. `AzureInventoryScanQueue` takes `QueueServiceClient` and uses `GetQueueClient("inventory-scan-request")`. It does not read `ConnectionStrings:storage` and does not call `new QueueClient(connectionString, ...)`.
- Scan-now consume: Functions keep `WithHostStorage(storage)`. The queue trigger omits `Connection`, so the worker uses the host storage connection Aspire sets (`AzureWebJobsStorage`). Functions do not `WithReference` the storage account or the queues resource for this feature, and they do not register `AddAzureQueueServiceClient`.

**Rationale**: Constitution 1.1.1 requires the documented client integration and DI for referenced resources, and forbids assembling connection strings in Application and Infrastructure. SQL already follows `AddSqlServerDbContext`. The queue publish path is the same rule for Azure Queue Storage (`connect-to-azure-queue-storage`). The Functions trigger is the Aspire Functions host-storage integration, which is how the worker receives the account. A second queues client on Functions would require a reference the trigger does not use.

**Alternatives considered**:

| Alternative | Rejection |
|-------------|-----------|
| `GetConnectionString("storage")` and `new QueueClient` in Infrastructure | Forbidden by principle VI. Bypasses queue health checks and traces. |
| `QueueTrigger(..., Connection = "ConnectionStrings:storage")` | Reads the Aspire-injected storage connection by name. The host-storage binding is the worker path. |
| `AddAzureQueueServiceClient` on Functions as well as Web | Extra reference and health check on a process that already consumes the queue through the trigger. |
| Register `AddSqlServerDbContext` inside `AddSoloDevBoard(IServiceCollection, IConfiguration)` | That method has no `IHostApplicationBuilder`. Principle VI names `Program.cs` or `AddSoloDevBoardHost`. |

---

## Inventory schedule (daily now, configurable later)

**Decision**: Drive the scheduled inventory from an Azure Functions **`TimerTrigger`** whose CRON expression is read from configuration, not hard-coded in C#. Use the Functions app-setting pattern, for example `[TimerTrigger("%Inventory__ScheduledCron%")]`, with default **`0 0 2 * * *`** (02:00 UTC daily, matching the current placeholder). Bind via AppHost: **`.WithEnvironment("Inventory__ScheduledCron", inventoryScheduleCron)`** where `inventoryScheduleCron` is an AppHost parameter defaulting to the daily value.

**Rationale**: Production stays on one overnight run per spec FR-006. Operators can move to several runs per day (for example `0 0 6,18 * * *`) by changing parameter or environment configuration and redeploying, without recompiling the Functions project.

**Alternatives considered**:

| Alternative | Rejection |
|-------------|-----------|
| Hard-coded `0 0 2 * * *` in attribute | Blocks future frequency changes without a code change. |
| Scheduled ACA Job only (no Functions timer) | Conflicts with ADR-0046 Functions worker and queue path for scan now. |
| `host.json` timer schedule JSON | Less visible in Aspire parameter model and CD environment variables. |

**Note**: Timer CRON uses six-field Azure Functions format (including seconds). Document UTC semantics in operator docs.

---

## Scan now and single active run

**Decision**: Implement manual scan via an **Azure Storage queue** message (`inventory-scan-request`) written from the Web app with the injected `QueueServiceClient` and processed by a **Queue-triggered** function on host storage. Coalesce duplicate requests per operator while a run is **Queued** or **InProgress** (spec FR-005). Timer and manual triggers share the same orchestrator entry so only one catalogue walk runs at a time (spec edge case: scheduled run while manual in progress). See [Aspire client integrations](#aspire-client-integrations-constitution-111-principle-vi).

**Rationale**: ADR-0046 already provisions storage for Functions host storage; queues reuse that account. Decouples "acknowledge within 5 seconds" (SC-004) from catalogue walk duration.

**Alternatives considered**: HTTP from Web to Functions (rejected: extra internal surface, auth, cold start coupling). Durable Functions orchestration (rejected for first slice: disproportionate until fan-out complexity grows).

---

## Unattended GitHub credentials

**Decision**: **PAT mode (local / self-hosted)**: Functions receive the same `GitHubAuth__PersonalAccessToken` binding as today (AppHost / Key Vault). **Hosted sign-in**: introduce server-side **GitHub App installation JWT** using existing `GitHubAuthOptions.GitHubAppId` and `GitHubAppPrivateKey`, wired as AppHost secret parameters and Key Vault secrets in publish mode, plus a scoped Infrastructure service `IGitHubInstallationTokenProvider` used by the scanner (not the interactive session).

**Rationale**: Spec FR-015 and FR-016 require installation access with nobody signed in. Options fields exist on `GitHubAuthOptions` but are not yet bound in AppHost (ADR-0046 note). OAuth client credentials alone cannot replace installation tokens for repository content API calls across the installation.

**Alternatives considered**: User refresh token from hosted cookie store (rejected: spec forbids reading interactive session). Single shared PAT on hosted (rejected: contradicts hosted sign-in model).

**Open dependency**: Confirm App private key storage name in Key Vault alongside existing `gh-pat` / `gh-app-client-secret` entries (`plan/KEY_VAULT_SECRET_INVENTORY.md` update in implement phase).

**Hosted inventory scope (FR-007, FR-007a, FR-016, SC-008)**: The unattended walk MUST NOT use "all repos visible to the installation" as the scope list. Scope is always the operator's **linked open-source catalogue** (same `IRepositoryService` / warmed Infrastructure catalogue cache the Repositories page uses). Implement `IInventoryCatalogueClient` as a thin adapter over that service, not over `GET /installation/repositories`. For each catalogue entry, the scanner uses the installation token when hosted; repositories the installation cannot access are persisted as `NotVisible` with unset funding fields after a repository metadata probe (spec FR-016). That preserves rows for linked repos outside the installation without widening scope to unknown repos.

**Web `OperatorKey` in hosted mode**: Interactive pages resolve `installation:{installationId}` from the signed-in session (installation id stored at sign-in or read from `GitHubAuth__InstallationId` bound to Web for the single-tenant deployment). Functions resolve the same partition for scan-now queue messages built on Web. Timer jobs target the configured installation id when `gh-app-installation-id` is set (single-tenant default); multi-installation discovery remains out of scope unless spec is updated.

**Installation tokens**: Mint installation access tokens asynchronously inside `IGitHubInstallationTokenProvider` before the scanner scope walk; cache until expiry inside the Functions worker scope. Do not call GitHub synchronously from `ICurrentUserContext.GetAccessToken()`.

**Hosted catalogue listing (FR-007a)**: Scope comes from `IRepositoryService` with the operator's linked repositories. In hosted mode, resolve the same operator login used for interactive catalogue refresh (installation-bound or configured owner), using a PAT-equivalent list call that returns topics, not `GET /installation/repositories` as the scope source. Content and metadata probes still use the installation token from T029a.

---

## Funding file fetch and comparison

**Decision**: Use GitHub **Contents API** for `.github/FUNDING.yml` then root `FUNDING.yml` (precedence per FR-009). Decode base64 content when returned as file blobs. Compare with canonical standard using **normalised line endings** (CRLF to LF) and **trim at most one trailing newline** only (FR-010). Record platform keys from a lightweight YAML read (keys under `github`, `patreon`, and so on) without semantic sponsorship validation.

**Rationale**: Aligns with spec assumptions and archive findings (`plan/archive/ASPIRE_MULTI_PROCESS_FINDINGS.md`). Textual compare avoids taking a YAML parser dependency unless implement review finds a justified need in `Directory.Packages.props`.

**Alternatives considered**: Raw byte comparison (rejected: fails FR-010 line-ending rule). GraphQL `communityProfile` only (rejected: does not return funding file text for standard comparison).

---

## Community Standards checklist storage

**Decision**: On each checked snapshot, store repository-only facts for description and README, and two facts for each of code of conduct, contributing guide, licence, security policy, issue templates, and pull request template: present in the project repository, and present only via the owner account's `.github` default (FR-014, clarification 2026-11-16). Store one licence name: the project repository's licence when that repository has one, otherwise the default's name. Do not store file bodies. Do not put these fields on the Open Source page DTOs.

**Decision**: For non-fork repositories, call `GET /repos/{owner}/{repo}/community/profile` once.

- Description is present when `description` is non-empty after trim.
- README is present when `files.readme` is non-null. README is not an organisation-default fact. The top-level `documentation` field is the repository homepage link, not the README.
- For code of conduct, contributing, licence, issue templates, and pull request templates, classify `html_url`:
  - URL repository segment equals the project repository: in-repository true, organisation-only false.
  - URL repository segment is `.github` and the owner matches the project owner: in-repository false, organisation-only true. This covers organisation and personal-account `.github` defaults. The spec's "organisation default" means that owner-level default.
  - Null file object: both facts false.
- Use `files.code_of_conduct_file.html_url` for location. Use `files.license.name` for the licence name and `files.license.html_url` for location. GitHub already prefers the project file over the owner `.github` file, so the returned object is the repository licence when one exists.
- The documented community profile payload does not include a security policy. Probe `SECURITY.md` with the Contents API in `.github/`, repository root, then `docs/`. In-repository is true when any of those exist in the project repository. Organisation-only is true only when none of them exist there and one exists in the owner `.github` repository, using the same path order.

**Decision**: The community profile endpoint does not serve forks. For a fork, skip that call. Read description from repository metadata. Detect README, code of conduct, contributing guide, licence, issue templates, and pull request template with the same Contents paths in the fork, then in the owner `.github` repository only when the fork has no file. Read the licence name from `GET /repos/{owner}/{repo}/license` when that call succeeds, otherwise from the owner `.github` licence file name when only the default exists.

**Rationale**: One profile call classifies five of the six health files for normal repositories, which matches SC-002's "page does not call GitHub" rule because the walk still happens in Functions. The dual facts match the clarification: a file in the project repository forces the organisation-only fact false even if a default also exists, and that default is not fetched in that case. Forks and security policy need direct file checks because the profile API cannot answer them.

**Alternatives considered**:

| Alternative | Rejection |
|-------------|-----------|
| Single boolean from the community profile, including organisation defaults as present | Contradicts the 2026-11-16 clarification. |
| Always list the owner `.github` repository as well as the profile | Extra calls on every repository. The profile URL already identifies the default, and the spec does not ask to record a default that sits behind a project file. |
| Contents API only, no community profile | More round trips and a weaker licence name than `files.license.name`. |

---

## UI and navigation

**Decision**: Add route **`/open-source`** (or `/open-source/funding` with redirect) as the first section of a shared **Open Source** nav item. Follow [product UI language](../../plan/wireframes/product-ui-language-wireframe.md). Create **`plan/wireframes/open-source-wireframe.md`** before implementation (spec assumption); plan defers pixel detail to that wireframe.

**Rationale**: Constitution IV and spec user story 4. Repositories page remains classification-only (FR-002, SC-006).

---

## Operator scoping and snapshots

**Decision**: Partition all inventory rows by **`OperatorKey`**: stable string derived from hosted mode (`installation:{id}`) or PAT mode (`user:{ownerLogin}`). Web reads only rows for the current `ICurrentUserContext`. Supersede prior finished runs per operator when a newer run completes; never delete the last successful/partial snapshot on failure (spec assumptions).

**Rationale**: FR-017 and hosted/PAT switch edge case in spec.

---

## Caching and page performance

**Decision**: No GitHub calls from the Open Source page (FR-002). Application service loads snapshot DTOs from SQL only. Optional in-memory cache is unnecessary for first slice given SC-002 targets stored reads; revisit only if profiling fails the 5 second goal with 500 rows.

**Rationale**: Constitution V; ADR-0044 caching scope excludes this derived snapshot data until an ADR says otherwise.