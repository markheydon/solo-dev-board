# Implementation Plan: Operator repository snapshot

**Branch**: `001-operator-snapshot-scanner` | **Date**: 2026-11-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-operator-snapshot-scanner/spec.md`

## Summary

The operator starts one collection from the existing Repositories page (`/repositories`) and reads its status there. A successful run stores a single JSON document of the personal GitHub catalogue the product can already list, including private repositories, archived repositories, and forks. Later features read that document. This plan does not define Community Standards or FUNDING facts; the document has an empty `laterFacts` object they can fill later.

Technical approach: one Application store port, a file adapter for PAT-only local trusted mode and for lean personal Azure deploy, and a blob adapter only when the hosted sign-in AppHost references Azure Blob Storage. Collection runs in the web process, off the Blazor circuit, one walk per operator. Withdrawn ADR-0046 is not implemented. A binding ADR (`adr-0048`) is an implementation artefact and must be Accepted before the hosted graph gains storage.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`), Blazor Server, existing layering (Web, Composition, Application, Infrastructure, Domain).

**Primary Dependencies**: Existing `IGitHubService` catalogue list and `ICurrentUserContext`. Aspire 17.0.0. MudBlazor on the Repositories page. New packages are not added in planning: `Aspire.Hosting.Azure.Storage` 17.0.0, `Aspire.Azure.Storage.Blobs` 17.0.0, and `Azure.Storage.Blobs` (version aligned to that client package) need explicit approval before implementation. Confirmed APIs are in [research.md](research.md): `AddAzureStorage`, `AddBlobContainer`, `RunAsEmulator`, `AddAzureBlobContainerClient`.

**Storage**: One JSON document per operator. No application database. Local disk file in lean posture. Azure Blob Storage (one blob per operator) for hosted sign-in only, after ADR-0048. Container Apps disk is ephemeral: a lean personal Azure deploy without the storage resource keeps file semantics, and a restart can drop the file. Hosted durability is the blob, not the container disk.

**Testing**: xUnit v3, Microsoft.Testing.Platform, built-in asserts, NSubstitute only where a mock is required. Store contract tests use a fake file system and a fake blob. bUnit for the Repositories page. Do not unit-test the AppHost graph.

**Target Platform**: Local Aspire web app, and Azure Container Apps via the existing AppHost publish path.

**Project Type**: Blazor Server web application inside the current solution.

**Performance Goals**: The collection must not block the circuit. The operator can leave `/repositories` and use another page. Status on the page updates about once a second while a run is in progress. GitHub access stays the existing sequential paged list (no extra fan-out).

**Constraints**: PAT-only `aspire start` stays web-only (no Azurite, storage, SQL, or Functions). One in-progress run per operator. Replace the snapshot only after success. Conditional write (file revision lock and temp replace; blob etag). Unsafe operator keys cannot escape the snapshot directory or blob prefix. UK English. No em dash. No "not available in PAT-only mode". Do not edit `.aspire/modules/`. Do not edit AppHost storage wiring until ADR-0048 is Accepted.

**Scale/Scope**: One document and one live walk per operator. The Repositories page is the only new surface. No worker project, no scanner page, no settings page, no run log.

**UI layout**: **Collect snapshot** and the collection status region extend the existing command strip and feedback area on `/repositories` (see `plan/wireframes/repositories-wireframe.md` for the baseline layout; normative copy and behaviour in [contracts/repositories-page.md](contracts/repositories-page.md)).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Before research

| Gate | Result |
| --- | --- |
| Principle I, layering | Pass, by approach. Web talks to Application DTOs. The store port lives in Application. File and blob adapters live in Infrastructure. Composition stays the application DI root. Aspire client registration stays in Web `Program.cs` before `AddSoloDevBoard`. |
| Principle II, proportionality | Pass. One port and two adapters. No new project, no EF Core, no worker. Extending the existing repository list DTO with fork, owner login, and the 64-bit id is smaller than a second HTTP stack. |
| Principle III, tests | Pass, by approach. xUnit v3 and built-in asserts. NSubstitute only for the GitHub boundary where a fake is awkward. AppHost graph is not unit-tested. User-guide change for Repositories is in scope because the page behaviour changes. |
| Principle IV, chrome | Pass, by approach. Status stays on the current Repositories chrome (`MudAlert`, existing toolbar). `mudblazor` skill at implementation. |
| Principle V, async and cache | Pass, by approach. Store and collection APIs are async and take `CancellationToken`. The walk uses `GetRepositoriesAsync(forceReload: true)` so it does not snapshot a stale catalogue. No distributed cache. |
| Principle VI, AppHost and DI | Pass, with a gate on timing. Storage is an AppHost resource only on the hosted sign-in graph, only after a binding ADR. The web host registers `AddAzureBlobContainerClient` with resource name `operator-snapshots`. Application and Infrastructure do not read connection strings. Lean graph unchanged. |
| Principle VII, lean path | Pass. Lean `aspire start` remains web compute plus GitHub credentials. Collection is on demand in-process. Copy explains missing schedule and local or ephemeral durability. It does not deny the feature in PAT-only local trusted mode. No third authentication mode. |
| Withdrawn ADR-0046 | Pass. No Azure SQL, no EF Core, no Azure Functions, no scheduled Container App Job, no `apiservice`, no Azurite on the lean graph. ADR-0046 is not the storage decision. ADR-0048 replaces that gap for this JSON document only. |
| Proportionality of one optional storage resource | Pass. The resource is added only to the hosted sign-in graph, only for this blob container, and only once ADR-0048 is Accepted. Lean personal Azure deploy does not take it. That is the smallest durable store that survives a Container Apps restart for hosted sign-in. |

No unjustified violation. Complexity tracking is empty.

### After design

Re-checked against [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), and [quickstart.md](quickstart.md).

| Gate | Result |
| --- | --- |
| Principle VI | Still pass. Design names `AddAzureStorage`, `AddBlobContainer`, `RunAsEmulator` (hosted local only), and `AddAzureBlobContainerClient("operator-snapshots")`. Connection-string presence is tested only in the web host to decide whether to register the client. The blob adapter receives `BlobContainerClient` by injection. ADR-0048 is a planned artefact, and AppHost edits wait for Accepted. |
| Principle VII | Still pass. Quickstart requires lean `aspire start` with no emulator. File adapter is the lean path. Ephemeral Container Apps copy is specified for lean personal Azure deploy. Hosted blob copy does not claim the container disk is durable. |
| ADR-0046 | Still pass. Design repeats the prohibition. SQL, Functions, and a worker are not in the structure below. |
| Proportionality | Still pass. One optional storage account and one container on the hosted graph only. In-process single-flight replaces a worker. `laterFacts` avoids a second store without inventing Community Standards or FUNDING fields. |

Gates pass. No NEEDS CLARIFICATION remains.

## Project Structure

### Documentation (this feature)

```text
specs/001-operator-snapshot-scanner/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── operator-snapshot-document.md
│   ├── operator-snapshot-store.md
│   ├── operator-snapshot-collection.md
│   └── repositories-page.md
└── tasks.md              # /speckit-tasks, not created here
```

Implementation also adds `adr/adr-0048-hosted-operator-snapshot-blob-storage.md` and the `adr/README.md` index row before any storage resource is added to AppHost. When the page ships, update `website/content/docs/repositories.md`, `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`, and `website` screenshot `repositories/overview.png`. When the hosted graph changes, update `docs/getting-started.md`, `docs/deployment.md`, and `src/SoloDevBoard.AppHost/README.md` in that same change.

### Source Code (repository root)

```text
src/SoloDevBoard.Domain/
└── Entities/Repositories/Repository.cs          # add GitHubId (long), OwnerLogin, IsFork from the existing list payload

src/SoloDevBoard.Application/
├── Identity/ICurrentUserContext.cs              # unchanged; login and token source
└── Services/Snapshots/                          # document, status DTO, store port, collection service, coordinator

src/SoloDevBoard.Infrastructure/
├── Snapshots/                                   # file adapter, blob adapter, credential slot for the background scope
└── GitHub/GitHubService.cs                      # map fork, owner.login, and long id on the existing DTO only

src/SoloDevBoard.Composition/
└── SoloDevBoardServiceCollectionExtensions.cs   # no storage connection lookup

src/SoloDevBoard.Web/
├── Program.cs                                   # conditional AddAzureBlobContainerClient; durability options; stopping token
└── Components/Features/Repositories/Pages/      # Collect snapshot, status, poll

src/SoloDevBoard.AppHost/
├── LeanAppHostGraph.cs                          # unchanged: no storage
└── HostedSignInAppHostGraph.cs                  # storage only after ADR-0048 is Accepted

tests/SoloDevBoard.Infrastructure.Tests/         # store contract, isolation, unsafe keys
tests/SoloDevBoard.Application.Tests/            # failed run, interruption, empty versus never run
tests/SoloDevBoard.Web.Tests/                    # bUnit for /repositories collection UI
```

**Structure Decision**: Keep the current five runtime projects. The snapshot document is an Application model serialised by Infrastructure. The coordinator is an Application singleton created from Web so Application does not take a hosting package. No `SoloDevBoard.Functions` and no `apiservice`.

## Implementation notes

Order:

1. Write and accept ADR-0048 (ADR skill). Do not edit AppHost storage until that status is Accepted.
2. Obtain explicit approval, then add the three packages to `Directory.Packages.props` at the versions in [research.md](research.md). Planning did not add them.
3. Store port, file adapter, blob adapter, and contract tests. File adapter must work with the blob types absent from the lean registration path.
4. Collection coordinator and reason mapping. Capture login and token at start; walk on `ApplicationStopping`; single-flight dictionary; read path records interruption.
5. Extend the existing catalogue mapping (`fork`, `owner.login`, 64-bit id). Do not add a GitHub client.
6. Repositories page, bUnit, user guide, and screenshot.
7. Only then, hosted sign-in AppHost: `AddAzureStorage("operator-snapshot-storage").RunAsEmulator()` for local hosted sign-in, `AddBlobContainer("operator-snapshots", "operator-snapshots")`, `WithReference` from `web`. Publish hosted sign-in omits `RunAsEmulator`. Lean graph stays web-only.
8. Docs named above for the AppHost change.

Validation before a pull request:

```bash
dotnet format SoloDevBoard.slnx --verify-no-changes
dotnet build SoloDevBoard.slnx -c Release -warnaserror
dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
```

Run or update the existing Playwright repositories command-strip and narrow-viewport coverage if the new button changes those assertions. Do not add an end-to-end test that collects against live GitHub.

## Complexity Tracking

No constitution violations require justification.
