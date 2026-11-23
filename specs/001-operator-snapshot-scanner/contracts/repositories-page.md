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

UK English. No em dash. Format date and time with `en-GB` (for example `23/11/2026 16:14`), using the server local offset from stored UTC until browser locale is available.

One persistent `MudAlert` under the command card carries **both** catalogue load feedback (for example "Loaded N repositories.") **and** a snapshot clause in the same line when snapshot status is known. Do not use a second alert for snapshots. Do not show schedule or durability copy on this page. The **Collect snapshot** control may use a short tooltip. Do not use a snackbar to repeat collection status.

| State | Snapshot clause (appended to catalogue feedback) |
| --- | --- |
| Never run | No snapshot exists. |
| In progress | Saving snapshot… started at {time}; {count} recorded so far. |
| Succeeded | Snapshot saved at {time} ({count} repositories included). Count may be zero. |
| Failed | Snapshot could not be saved at {time}. Plain-language reason. Optional `detail`. Last success when one exists. |
| Interrupted | Snapshot was not finished at {time} because the site stopped before it completed. Last success when one exists. |

Do not say the feature is unavailable in PAT-only local trusted mode.

## Guide and browser checks

`website/content/docs/repositories.md` describes this page and is locked by `RepositoriesTests` plus the inventory row in `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`. The same change updates that guide (shipped behaviour only), the bUnit tests, and the overview screenshot `repositories/overview.png`, because the command strip and status region change. Existing Playwright coverage of the repositories command strip and the 390px overflow must still pass; update those tests when the new control changes what they assert. Do not add a Playwright journey that walks a real GitHub collection.

Chrome stays the MudBlazor page header, toolbar, elevation-1 paper, and alert pattern already on this page. Consult the `mudblazor` skill when implementing.
