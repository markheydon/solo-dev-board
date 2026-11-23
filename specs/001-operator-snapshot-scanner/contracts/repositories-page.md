# Contract: Repositories page collection UI

Route remains `/repositories` (`Repositories.razor`). No new page, no settings page, no top-level scanner.

## Actions

| Control | Behaviour |
| --- | --- |
| Existing **Reload from GitHub** | Still calls `IRepositoryService.GetRepositoriesAsync(forceReload: true)`. Does not start a collection and does not change the snapshot. |
| New **Collect snapshot** | Calls `StartAsync`. Disabled while status is `InProgress`. Visible in the same command strip on narrow viewports (do not hide it only inside the overflow menu). Accessible name is the visible label. |
| Page open | `GetStatusAsync` and the existing catalogue load. Does not call `StartAsync`. |

While status is `InProgress` and the component is alive, a timer refreshes `GetStatusAsync` about once a second and updates the status text without a full page reload. Dispose the timer with the component. The catalogue grid is unchanged and does not list snapshot entries.

## Status copy

UK English. No em dash. Times shown in the operator's local presentation of the stored UTC timestamps, with a readable date and time.

| State | What the operator sees |
| --- | --- |
| Never run | No snapshot is kept. No finish time and no snapshot count. |
| In progress | Collection in progress. Started at {time}. Repositories recorded so far: {count}. |
| Succeeded | Collection succeeded. Finished at {time}. Repositories in the snapshot: {count}. Count may be zero. |
| Failed | Collection failed at {time}. Then one of: a sign-in or token problem; a GitHub rate limit; an unexpected error. If `detail` is present, show that one line after the reason. If a last success exists, also show its finish time and count. |
| Interrupted | Collection interrupted at {time}. The site stopped before the collection finished. If a last success exists, also show its finish time and count. |

Place this in a `MudAlert` (persistent status), not a snackbar. A snackbar may confirm that a collection was accepted, and must not repeat the persistent alert.

## Durability and schedule

Always include: "Nothing collects on a schedule."

| `DurabilityKind` | Additional sentence |
| --- | --- |
| `LocalMachine` | The snapshot is kept on this machine rather than promised on another machine. |
| `EphemeralContainerDisk` | The snapshot is kept on this container's disk. A restart can drop the snapshot. |
| `HostedBlob` | The snapshot is kept for this signed-in operator and remains after a restart. |

Do not say the feature is unavailable in PAT-only local trusted mode.

## Guide and browser checks

`website/content/docs/repositories.md` describes this page and is locked by `RepositoriesTests` plus the inventory row in `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`. The same change updates that guide (shipped behaviour only), the bUnit tests, and the overview screenshot `repositories/overview.png`, because the command strip and status region change. Existing Playwright coverage of the repositories command strip and the 390px overflow must still pass; update those tests when the new control changes what they assert. Do not add a Playwright journey that walks a real GitHub collection.

Chrome stays the MudBlazor page header, toolbar, elevation-1 paper, and alert pattern already on this page. Consult the `mudblazor` skill when implementing.
