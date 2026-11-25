# Contract: Repositories schedule copy

Extends [specs/001-operator-snapshot-scanner/contracts/repositories-page.md](../../001-operator-snapshot-scanner/contracts/repositories-page.md). Route remains `/repositories`. No new page, no settings page, no retry control.

Feature 001 said this page does not show schedule copy. This feature replaces that sentence. Durability copy for a local or ephemeral file remains. Schedule copy and the account scan status are added in the same status alert, not a second alert and not a snackbar.

In hosted sign-in the current scan state is the reported account scan status from [account-scan-status.md](account-scan-status.md). The page does not infer it from the clock, from a missing visit, or from whether a worker seems to be absent.

## Actions

| Control | Behaviour |
| --- | --- |
| **Reload from GitHub** | Unchanged. Does not start a collection and does not change the snapshot, the status blob, or the queue. |
| **Collect snapshot** | Still calls `StartAsync` when callers are not told `scheduled` or `running`. A reported `stalled` account may start. That start sets `running` and writes a liveness mark. Visible label and accessible name stay **Collect snapshot**. |
| Page open | `GetStatusAsync` only. Does not call `StartAsync`. Does not fill the daily queue. Does not persist `stalled`. |

The in-progress poll (about once a second, disposed with the component) stays while callers are told `running`. It must show the recorded count rising, and whether the operator or the daily occasion started the run, without a full reload.

## Schedule clause

UK English. No em dash. Times already on the page stay `en-GB` local formatting of stored UTC. The occasion itself is written as the literal **05:00 UTC**, not converted to the operator's zone.

One schedule sentence is included in the same `MudAlert` as the catalogue line, the snapshot clause, and the status word.

| Facts | Sentence |
| --- | --- |
| Lean posture | Nothing collects on a schedule. The snapshot is kept on this machine rather than promised on another machine. Do not show `scheduled`, `running`, `stalled`, or `suspended` from a worker. |
| Hosted, before 05:00 UTC, and the next ask will queue them | Next scheduled collection: 05:00 UTC. |
| Hosted, status `scheduled`, and no refusal applies | Next scheduled collection: 05:00 UTC. Do not say the day was missed. Do not show `running` until the worker has taken the entry. |
| Hosted, after today's 05:00 UTC ask has passed, not `scheduled`, and they would be eligible at the following 05:00 UTC | Next scheduled collection: 05:00 UTC on the following UTC day. The sentence still says 05:00 UTC. Use this row whether or not today's scan succeeded. It names the next ask. It does not say a sooner fill is forbidden. |
| Hosted, not allowed | No scheduled collection will run because you are not allowed to use the product. This row wins when the 30-day window is also closed or a stored-access wait is set. Show `suspended` and that reason. Do not show `running` and do not present a failed snapshot for that removal. When a stored-access wait is set, still show that failed run and the sign-in or token reason. A previous successful snapshot stays. |
| Hosted, allowed, and `credentialUnusable` | No scheduled collection will run until you sign in again. Show `suspended`, the failed result, and the sign-in or token reason. Do not add a second sentence for a closed 30-day window. Do not say a collection will run tonight or at the next 05:00 UTC. |
| Hosted, allowed, not in a stored-access wait, outside the 30-day window | No scheduled collection will run until you sign in again. |

There is no control to change the time, how often, or the 30 days.

## Status words

Use these words in the same alert. Do not invent a stored `stalled` value.

| Reported status | What the alert shows |
| --- | --- |
| `never run` | Never run. No last-run time, no result, and no comment. |
| `scheduled` | Scheduled. Not running and not finished. Tries are not listed. The minimum wait is not its own state. |
| `running` | Running. When the daily occasion started it, say so. When the operator started it, say so. |
| `finished`, result success | Finished. Comment Success. Show when that scan finished and the snapshot count. |
| `finished`, result failure | Finished. One short reason and when it failed, beside the latest successful snapshot time when one exists. |
| `suspended`, not allowed | Suspended. The fixed comment that they are not allowed comes from `SuspensionReason` `notAllowed`. It is not `ResultComment`. The previous last-run time, result, and result comment stay. The earlier successful snapshot stays. |
| `suspended`, stored access | Suspended. Result failure. The sign-in or token comment. |
| `stalled` | Stalled. Do not say running, do not say the site stopped, and do not use the three short failure reasons. The previous successful snapshot stays. Partial recordings are not a new success. **Collect snapshot** remains available when they can still use the product. |

Last successful snapshot time stays beside a failed run and beside a reported `stalled` scan, as feature 001 shows it beside a failed run.

## Guide

`website/content/docs/repositories.md` gains this schedule behaviour and these status words in the same implementation change, locked by the Repositories bUnit tests and the inventory row in `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`. Update the overview screenshot only when the status region's visible text changes enough that the published image would be wrong. Do not add a Playwright test that waits for 05:00 UTC or collects against GitHub.
