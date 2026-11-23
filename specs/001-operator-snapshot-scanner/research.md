# Research: Operator repository snapshot

## Aspire storage packages and APIs

**Decision:** Stay on the Aspire **17.0.0** family already pinned in `Directory.Packages.props` (`Aspire.Hosting.AppHost` and the other `Aspire.Hosting.*` packages, CLI `17.0.0`). When implementation is allowed to add packages, add only:

- `Aspire.Hosting.Azure.Storage` **17.0.0** on `SoloDevBoard.AppHost`
- `Aspire.Azure.Storage.Blobs` **17.0.0** on `SoloDevBoard.Web` (client registration)
- `Azure.Storage.Blobs` on `SoloDevBoard.Infrastructure`, at the version `Aspire.Azure.Storage.Blobs` 17.0.0 depends on (resolve with the repo's package inspection approach when the add is approved; do not guess a version in planning)

None of these package ids are in `Directory.Packages.props` today. **Do not add them during planning.** Implementation needs an explicit package-add approval first.

AppHost methods, confirmed with `aspire docs` and `aspire docs api search --language csharp` against this CLI (Aspire commit `2d325b1`, the 17.0.0 family):

- `AddAzureStorage(IDistributedApplicationBuilder, string)` in `Aspire.Hosting.Azure.Storage`
- `AddBlobContainer(IResourceBuilder<AzureStorageResource>, string name, string? blobContainerName = null)` (the `AzureBlobStorageResource` overload is obsolete)
- `RunAsEmulator(IResourceBuilder<AzureStorageResource>, Action<IResourceBuilder<AzureStorageEmulatorResource>>?)` for local hosted sign-in only
- `WithReference` on the existing web resource

Web host method, same lookup:

- `AddAzureBlobContainerClient(IHostApplicationBuilder, string connectionName, ...)` in `Aspire.Azure.Storage.Blobs`

Do **not** call obsolete `AddAzureBlobClient`. Do **not** edit `.aspire/modules/`.

Resource names (the client `connectionName` is the AppHost resource name):

- Storage account: `operator-snapshot-storage`
- Blob container resource: `operator-snapshots` (container name `operator-snapshots`)

Hosted sign-in local run, after the ADR is accepted: `AddAzureStorage("operator-snapshot-storage").RunAsEmulator().AddBlobContainer("operator-snapshots", "operator-snapshots")`, then `WithReference` that container from `web` and `WaitFor` it. Publish for hosted sign-in uses the same storage and container resources without `RunAsEmulator`, with `WithReference` from `web`. Default blob data-contributor role from `AddAzureStorage` is enough; do not add a second storage account.

Lean graph (`LeanAppHostGraph`, PAT-only local trusted mode, and lean personal Azure publish) does not gain storage, Azurite, SQL, Functions, or an `apiservice`. `aspire start` on that graph stays web-only.

`AddAzureBlobContainerClient` throws if neither connection string nor service URI is configured. Web `Program.cs` may test whether Aspire injected `ConnectionStrings:operator-snapshots` and, only then, call `AddAzureBlobContainerClient("operator-snapshots")`. That check stays in the web host. Application and Infrastructure must not read that connection string or build a `BlobContainerClient` from it.

The blob adapter then uses the injected `BlobContainerClient`. Conditional replace uses the Azure SDK already behind that client: `BlobUploadOptions.Conditions` with `BlobRequestConditions.IfMatch` for an existing blob and `IfNoneMatch = ETag.All` for a create. Aspire does not define a separate etag method.

**Rationale:** Principle VI requires the Aspire client integration and the AppHost resource name. Principle VII forbids putting that resource on the lean graph. The 17.0.0 ids match the repo's Aspire release family.

**Alternatives considered:** `AddBlobs` plus a hand-created container name in code (rejected: the container name would not be part of the graph, and the client would be a service client rather than the container the product uses). Hand-built connection strings in Infrastructure (rejected: Principle VI). Azurite on the lean graph (rejected: Principle VII and this technical approach). `Aspire.Hosting.Azure.Storage` 13.6.5 from withdrawn ADR-0046 (rejected: wrong release family, and that ADR is not binding).

## Operator login below the web project

**Decision:** The collection key is `ICurrentUserContext.OwnerLogin` (`SoloDevBoard.Application/Identity/ICurrentUserContext.cs`). Web does not pass a login, and nothing keys off a display name (`GitHubAuthenticationSummary.IdentityLabel` is display only).

- PAT-only local trusted mode: `SingleUserCurrentUserContext` (Infrastructure) returns configured `GitHubAuth:OwnerLogin` when that value is real, otherwise `ResolvedPatOwnerLogin` from the existing PAT owner lookup (`GitHubPatOwnerLoginResolver` / `GET /user` on the existing GitHub HTTP client).
- Hosted sign-in: `HostedUserCurrentUserContext` returns the `HostedOwnerLoginClaimType` claim (default owner-login claim). That type is scoped to `HttpContext`.

`ICurrentUserContext` is registered scoped in `InfrastructureServiceExtensions` and switches on `GitHubAuth:HostedSignInEnabled`.

Two hosted operators stay isolated because every read and write takes the login from that operator's own claim. Operator A and operator B therefore address different files or blobs. The Repositories page has no operator argument. PAT-only local trusted mode is still one trusted operator; the file is still named with the resolved GitHub login.

The background walk cannot keep the Blazor scope. Hosted `GetAccessToken()` and `OwnerLogin` read `IHttpContextAccessor`. The start request copies the login and the access token into memory for that run only. A new DI scope sets a scoped credential slot that `ICurrentUserContext` consults before the HTTP or PAT implementations. `GitHubAuthHandler` then sends that token on the existing `GitHubApiClient`. The token is never written to the snapshot, logs, or UI. The slot is cleared when the run finishes.

**Rationale:** The product already resolves the GitHub login below Web. Reusing it keeps one identity for the catalogue and the snapshot. Copying the token into a fresh scope is what lets the walk outlive the circuit without a second GitHub stack.

**Alternatives considered:** Keying the file on the display name (rejected: not stable, and not the GitHub identity). Reading the hosted claim again inside the background scope (rejected: there is no `HttpContext`). A new login resolver (rejected: duplicates PAT owner resolution and the hosted claim).

## Single-flight collection off the Blazor circuit

**Decision:** One singleton coordinator in Application, constructed from Web with `IHostApplicationLifetime.ApplicationStopping` passed in as a `CancellationToken` (Application does not take a new hosting package). `IOperatorSnapshotService.StartAsync` returns as soon as the run is accepted. The walk is a `Task` on that coordinator, not a circuit continuation and not a new worker project.

The coordinator keeps a `ConcurrentDictionary` keyed by the sanitised login. `TryBegin` does not start a second task while that login has a live task. The task creates a scope through an Application-defined scope factory implemented in Infrastructure, calls existing `IGitHubService.GetRepositoriesAsync(forceReload: true)`, and writes the store. Cancellation is the host stopping token, not the circuit token and not navigation away from `/repositories`.

The page polls status on a timer (`PeriodicTimer` in the component, about once a second, disposed with the component) and calls only the Application status method. That method overlays the in-memory count and start time when this process owns the run. Opening the page and **Reload from GitHub** do not call `TryBegin`.

GitHub access stays the existing paged `GET /user/repos` (or the public variant when docs-capture mode is on) through `GitHubService`. Pagination is already sequential. This feature does not add per-repository fan-out, a second `HttpClient`, or Razor scraping. Bounded concurrency is that one walk per operator plus the existing one-page-at-a-time list. Rate-limit behaviour stays "the existing client throws `HttpRequestException` with the status code"; the collection maps that outcome (see below). It does not add a new retry policy.

Catalogue gap to close on the same mapping, not a new request: `RepositoryResponseDto` already deserialises the list payload but drops `fork` and `owner.login`, and `Repository.Id` is an `int` narrowed from the payload's `long`. Add `IsFork`, `OwnerLogin`, and the original `long` GitHub id onto the domain `Repository` from that same DTO so the snapshot can store them. Leave the existing `Id` property in place for current callers.

Reason mapping for the run, from exceptions the existing client already throws:

| Outcome | When |
| --- | --- |
| Sign-in or token problem | `HostedAuthenticationRequiredException`, `GitHubPatConnectivityRequiredException`, HTTP 401, or HTTP 403 whose body is not a rate-limit response |
| GitHub rate limit | HTTP 429, or HTTP 403 whose message contains GitHub's rate-limit wording (the existing `EnsureSuccessStatusCodeAsync` already puts the body in the exception message) |
| Site stopped mid-run | Host stopping token cancelled the walk, or a later read finds `inProgress` with no live task in this process |
| Unexpected error | Anything else. Optional detail is one line: exception type and HTTP status code only. No response body, token, or secret |

A failure or interruption updates run state and leaves `repositories` and `lastSuccessfulCollectionAt` as they were. Only a successful walk replaces those.

**Rationale:** The circuit must stay free so the operator can leave `/repositories`. A process-local dictionary is the single-flight gate the prompt asks for, without a worker project. Reusing `GetRepositoriesAsync` keeps one catalogue and one rate-limit path.

**Alternatives considered:** `BackgroundService` queue plus a worker project (rejected: out of scope). Awaiting the walk inside the button click (rejected: pins the circuit). `Parallel.ForEach` over repositories (rejected: this snapshot is the list itself; extra parallelism would be a second GitHub access pattern). Scraping the Repositories page (rejected).

## Detecting a dead process as interrupted

**Decision:** The durable document stores `run.status = inProgress` when a walk starts. The live task exists only in the coordinator's dictionary. On every status read, if the document says `inProgress` and this process has no live task for that login, the store rewrites the run to `interrupted` with reason `siteStopped`, without changing the last successful repository list. That rewrite uses the same conditional replace as any other write. Interrupted is not in progress, so `TryBegin` may start another walk.

Leaving the page, signing out, or using another page does not clear the live task and does not mark the run interrupted. Those cases keep showing in progress, succeeded, or failed when the operator returns, for as long as the process is up.

Container Apps disk is ephemeral. A lean personal Azure deploy that does not reference `operator-snapshots` still uses the file adapter on that disk. A replica restart starts from the image, so the file can disappear. The UI must say so (see the durability decision below). Hosted sign-in durability is the blob, not the container disk.

**Rationale:** After a crash the dictionary is empty and the document is the only memory of the walk. The next read is the moment the spec requires the operator to see interrupted, and it must not stick in a state that blocks a new collection. No heartbeat or second process is required for a single web process.

**Alternatives considered:** Treating in-progress as still running until a timeout (rejected: the spec says the next visit after a stop is interrupted, and a timeout would block a new collection). A blob lease across replicas (rejected for this feature: the walk is in-process and the graph is one web app; multi-replica coordination is a later decision). Marking interrupted on sign-out (rejected: the spec says sign-out is not the site stopping).

## Where the file lives, and what the UI says about durability

**Decision:** File adapter directory is `{content root}/data/operator-snapshots/{sanitisedLogin}.json`. Writes go to a temp file in that directory and then replace. The adapter runs whenever `BlobContainerClient` was not registered.

Durability kind is chosen in the web host and passed as options the status DTO exposes:

| Kind | When | Copy the page shows, with the schedule sentence |
| --- | --- | --- |
| Local machine | File adapter, and `CONTAINER_APP_NAME` is unset | Nothing collects on a schedule. The snapshot is kept on this machine rather than promised on another machine. |
| Ephemeral container disk | File adapter, and `CONTAINER_APP_NAME` is set (lean personal Azure deploy, no storage resource) | Nothing collects on a schedule. The snapshot is kept on this container's disk. A restart can drop the snapshot. |
| Hosted blob | Blob adapter registered | Nothing collects on a schedule. The snapshot is kept for this signed-in operator and remains after a restart. |

Do not use the phrases "not available in PAT-only mode", "PAT mode", or "hosted mode".

**Rationale:** Local `aspire start` keeps the file across a process restart on that machine, which matches the spec. Azure Container Apps does not. The platform variable distinguishes those two file-adapter deployments without a connection string in Application code.

**Alternatives considered:** Always warning that a restart drops the snapshot (rejected: false for local disk and for the blob). An operator-facing choice of store (rejected: out of scope).

## Conditional replace

**Decision:** The document carries an integer `revision`, starting at 1. A writer reads the revision (and, for blobs, the Azure `ETag`), applies one mutation, increments `revision`, and writes only if the stored token still matches. File adapter: hold a per-path lock, re-read `revision`, write a sibling temp file, replace only on match. Blob adapter: `IfMatch` / `IfNoneMatch` as above, and still increment `revision`. A mismatch (412 or lock re-read failure) means a stale writer stops and does not overwrite the newer document. The live owner may reload and retry only while it still holds the in-memory run.

**Rationale:** Temp-file replace avoids a torn JSON file. The etag or revision stops a stale writer publishing an older snapshot over a newer one.

**Alternatives considered:** Last-write-wins (rejected by the technical approach). A database rowversion (rejected: no application database, and ADR-0046 stays withdrawn).

## Binding ADR before storage wiring

**Decision:** The first implementation artefact that records the storage decision is `adr/adr-0048-hosted-operator-snapshot-blob-storage.md` (next number after ADR-0047), status **Accepted** before any AppHost storage edit. Author it with the ADR skill and update `adr/README.md` in the same change. Decision content to record:

- Hosted sign-in graph only: one Azure Storage account and one blob container for this JSON document, modelled with the APIs above.
- Local hosted sign-in uses `RunAsEmulator`. Lean graph does not.
- No Azure SQL, EF Core, Azure Functions, scheduled Container App Job, or `apiservice`.
- Web registers `AddAzureBlobContainerClient("operator-snapshots")` only when that resource is referenced.
- Container Apps disk is ephemeral; blob storage is the hosted durability path.

Until that ADR is Accepted, do not change `HostedSignInAppHostGraph` or `LeanAppHostGraph` to add storage.

**Rationale:** Principle VI requires a binding ADR before a material AppHost resource. Planning records the decision; it does not edit the graph.

**Alternatives considered:** Wiring storage in the same commit as the plan (rejected). Reviving ADR-0046 (rejected: withdrawn, and it chose SQL plus Functions).
