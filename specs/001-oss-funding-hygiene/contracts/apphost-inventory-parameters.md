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
| `gh-app-installation-id` | required when hosted inventory runs, else `-` | `GitHubAuth__InstallationId` (Web + Functions) | Single-tenant hosted: binds Web and Functions to the same installation partition for read, scan-now, and timer. When missing in hosted mode, inventory is unavailable (FR-015). |
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

The storage account stays the existing AppHost `storage` resource (`WithHostStorage` on Functions). Scan-now adds a queues child and references it from Web only:

```csharp
var inventoryQueues = storage.AddQueues("inventory-queues");

app.WithReference(inventoryQueues).WaitFor(inventoryQueues);
functions.WithHostStorage(storage); // no WithReference(storage) for this feature
```

| Setting | Value |
|---------|--------|
| Aspire queues resource | `inventory-queues` |
| Client registration | `AddAzureQueueServiceClient("inventory-queues")` on the Web host composition root |
| Queue name inside the account | `inventory-scan-request` |
| Functions trigger | `[QueueTrigger("inventory-scan-request")]` with no `Connection` argument (host storage from `WithHostStorage`) |

`connectionName` matches the queues resource. Infrastructure publishes through `QueueServiceClient`. It does not read `ConnectionStrings:storage`.