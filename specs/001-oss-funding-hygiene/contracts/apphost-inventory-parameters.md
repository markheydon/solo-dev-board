# AppHost parameters and environment bindings

**Feature**: `specs/001-oss-funding-hygiene`

Bindings for Aspire `AppHost.cs` (local and publish). Names use AppHost parameter kebab-case; runtime uses ASP.NET Core `__` nesting.

---

## New parameters

| Parameter | Default (Production) | Bound to | Purpose |
|-----------|---------------------|----------|---------|
| `inventory-scheduled-cron` | `0 0 2 * * *` | Functions `Inventory__ScheduledCron` | UTC CRON for nightly run; change without code rebuild |
| `gh-app-id` | `-` | `GitHubAuth__GitHubAppId` (Functions only) | GitHub App id for installation JWT |
| `gh-app-private-key` | secret, `-` | `GitHubAuth__GitHubAppPrivateKey` (Functions only) | PEM private key (Key Vault in publish) |
| `gh-app-installation-id` | optional, `-` | `GitHubAuth__InstallationId` (Web + Functions) | Single-tenant hosted: binds Web and Functions to the same installation partition for read and scan-now |
| `hosted-inventory-enabled` | derived | `Inventory__HostedInventoryConfigured` (Web + Functions) | Boolean: true when hosted sign-in and `gh-app-id` plus key are configured (Web does not receive the private key) |

Existing `gh-pat`, `hosted-sign-in-enabled`, and auth vault bindings remain per current AppHost.

---

## EF migrations resource (local + publish)

Logical names in AppHost model:

| Resource name | Type | Notes |
|---------------|------|-------|
| `inventory-migrations` | `AddEFMigrations` child of `app` | `.WithMigrationsProject<SoloDevBoard.Inventory.Migrations>()` |
| | | `.RunDatabaseUpdateOnStart()` for local only |
| | | `.PublishAsMigrationBundle(publishContainer: true).PublishAsAzureContainerAppJob()` for deploy |

**Ordering**:

1. SQL `inventory` healthy  
2. `inventory-migrations` completes  
3. `app` and `functions` start (`WaitForCompletion(inventoryMigrations)`)

---

## Queue (storage)

| Setting | Value |
|---------|--------|
| Queue name | `inventory-scan-request` |
| Connection | Functions host storage account (existing Aspire storage resource) |

Web receives storage queue connection via Aspire reference when enqueue is implemented (same account as Functions host).