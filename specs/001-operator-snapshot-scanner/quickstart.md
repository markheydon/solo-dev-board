# Quickstart: operator snapshot

Validation guide for [spec.md](spec.md). Behavioural detail is in [data-model.md](data-model.md) and [contracts/](contracts/). This is not the implementation.

## Prerequisites

- .NET 10 SDK and the Aspire 17.0.0 CLI already used in this repo (`aspire --version` reports `17.0.0`).
- GitHub credentials for PAT-only local trusted mode, as in [docs/getting-started.md](../../docs/getting-started.md). No Azure subscription.
- Do not start Azurite, SQL, or Functions. The lean graph must not include them.

Packages `Aspire.Hosting.Azure.Storage`, `Aspire.Azure.Storage.Blobs`, and `Azure.Storage.Blobs` are not part of this validation until an explicit package add lands. Lean checks below do not need them.

## 1. Lean `aspire start` has no storage emulator

From the repo root, start the lean AppHost (PAT-only local trusted mode, `HostedSignInEnabled` false):

```bash
aspire start
```

Expect the dashboard to show the web app only. There must be no storage, Azurite, SQL, or Functions resource. Confirm in the Aspire resource list before continuing.

Open `https://` the web endpoint, go to **Repositories**. The page loads the live catalogue and does not start a collection by itself. **Reload from GitHub** refreshes the catalogue and leaves collection status unchanged.

Copy on the page states that nothing collects on a schedule, and that the snapshot is kept on this machine rather than promised on another machine.

## 2. Collection writes a local file

Choose **Collect snapshot**. The page shows in progress, a start time, and a recorded count that increases while you stay on the page. You can open another page and come back.

When it succeeds, the page shows the finish time and the count. It does not list snapshot repositories. The live catalogue is still the list on the page.

The file adapter writes:

```text
{web content root}/data/operator-snapshots/{github-login}.json
```

`{github-login}` is the PAT owner login, not a display name. The JSON matches [contracts/operator-snapshot-document.md](contracts/operator-snapshot-document.md): `schemaVersion` 1, `laterFacts` an empty object, and one repository entry per catalogue repository the product already shows (including private, archived, and fork, with those flags independent).

Restart the web process with `aspire start` again on the same machine. The same file is still there, and the page shows the same finish time and count. There is still no storage emulator.

To see interruption: start a collection and stop the AppHost before it finishes. Start again and open **Repositories**. The run is interrupted, with the reason that the site stopped before the collection finished, and **Collect snapshot** can be used again. A previous successful file's repository entries are unchanged if one existed.

## 3. How hosted blob mode differs

Do this only after `adr/adr-0048-hosted-operator-snapshot-blob-storage.md` is **Accepted** and the approved packages are referenced. Until then, do not add the storage resource, and do not expect a blob.

Hosted sign-in (`HostedSignInEnabled` true), after that ADR:

- Local `aspire start` includes `operator-snapshot-storage` running as the emulator (`RunAsEmulator`) and the `operator-snapshots` container referenced by `web`. The lean graph does not.
- Web calls `AddAzureBlobContainerClient("operator-snapshots")` because Aspire injected that connection. The file under `data/operator-snapshots` is not the hosted store.
- One blob per operator: container `operator-snapshots`, blob `{github-login}.json`, using the hosted owner-login claim. Two signed-in users get two blobs. A restart or a new sign-in still shows the same snapshot. Container disk is not the durability path.
- Publish of hosted sign-in provisions the storage account (no emulator). Lean personal Azure publish does not. On that lean deploy the page says the snapshot is on the container disk and a restart can drop it. Container Apps disk is ephemeral.

## Automated checks

From the repo root, the implementation change must pass:

```bash
dotnet format SoloDevBoard.slnx --verify-no-changes
dotnet build SoloDevBoard.slnx -c Release -warnaserror
dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
```

Those tests cover the store contract (fake file system and fake blob), operator isolation, a failed run keeping the last success and recording one short reason, an in-progress document becoming interrupted on the next read without blocking a new collection, unsafe keys, and empty success versus never run versus a failed first run. bUnit covers the Repositories page: open does not collect, reload does not collect, and the status sentences above. The AppHost graph is not unit-tested.
