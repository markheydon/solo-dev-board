# Contract: Repositories schedule copy

Extends [specs/001-operator-snapshot-scanner/contracts/repositories-page.md](../../001-operator-snapshot-scanner/contracts/repositories-page.md). Route remains `/repositories`. No new page, no settings page, no retry control.

Feature 001 said this page does not show schedule copy. This feature replaces that sentence. Durability copy for a local or ephemeral file remains. Schedule copy is added in the same status alert, not a second alert and not a snackbar.

## Actions

| Control | Behaviour |
| --- | --- |
| **Reload from GitHub** | Unchanged. Does not start a collection and does not change the snapshot or the queue. |
| **Collect snapshot** | Still calls `StartAsync` when the operator is not queued and no collection for them is in progress. While `queuedOccasion` is set, or the run is `inProgress` (including a further try waiting at the back), the control does not start a collection. Visible label and accessible name stay **Collect snapshot**. |
| Page open | `GetStatusAsync` only. Does not call `StartAsync`. Does not fill the daily queue. |

The in-progress poll (about once a second, disposed with the component) stays. It must show the recorded count rising, and whether the operator or the daily occasion started the run, without a full reload.

## Schedule clause

UK English. No em dash. Times already on the page stay `en-GB` local formatting of stored UTC. The occasion itself is written as the literal **05:00 UTC**, not converted to the operator's zone.

One schedule sentence is included in the same `MudAlert` as the catalogue line and the snapshot clause.

| Facts | Sentence |
| --- | --- |
| Lean posture | Nothing collects on a schedule. The snapshot is kept on this machine rather than promised on another machine. |
| Hosted, before 05:00 UTC, and the next ask will queue them | Next scheduled collection: 05:00 UTC. |
| Hosted, queued for today, the pass has not started, and no refusal applies | Next scheduled collection: 05:00 UTC. Do not say the day was missed. Do not show the run as in progress until the pass starts. |
| Hosted, after today's 05:00 UTC ask has passed, not queued, and they would be eligible at the following 05:00 UTC | Next scheduled collection: 05:00 UTC on the following UTC day. The sentence still says 05:00 UTC. Use this row whether or not today's scan succeeded. It names the next ask. It does not say a sooner fill is forbidden. |
| Hosted, not allowed | No scheduled collection will run because you are not allowed to use the product. This row wins when the 30-day window is also closed or a stored-access wait is set. Do not show a removed queue wait as in progress, failed, or interrupted. When a stored-access wait is set, still show that failed run and the sign-in or token reason. A previous successful snapshot stays. |
| Hosted, allowed, and `credentialUnusable` | No scheduled collection will run until you sign in again. Show the failed daily run and the sign-in or token reason. Do not add a second sentence for a closed 30-day window. Do not say a collection will run tonight or at the next 05:00 UTC. |
| Hosted, allowed, not in a stored-access wait, outside the 30-day window | No scheduled collection will run until you sign in again. |

There is no control to change the time, how often, or the 30 days.

## Run clause additions

| State | Extra words |
| --- | --- |
| In progress, trigger `daily` | Say the daily occasion started it. |
| In progress, trigger `operator` | Say you started it. Do not call it the daily occasion. |
| Further try waiting | Same in-progress sentence. Do not list tries. Do not show failed. The minimum wait is not shown as its own state. |
| Further try removed because they are not allowed | Stop the in-progress sentence. Do not replace it with a failed or interrupted sentence for that removal. Show the restored previous run when one exists. |
| Stored access cannot be used | Show the failed run and the sign-in or token reason. Do not restore the previous run in place of that failure. |
| Interrupted, `scheduledCollectionStopped` | The scheduled collection stopped before it finished. Do not say the site stopped. |
| Interrupted, `siteStopped` | Unchanged: the site stopped before it completed. |
| Succeeded or failed | Keep the feature 001 sentence. Where the run has a trigger, the operator can tell which start it was from the in-progress and terminal copy above. A successful daily run uses the same saved-at sentence as a hand-started success. |

Last successful snapshot time stays beside a failed or interrupted run, as feature 001.

## Guide

`website/content/docs/repositories.md` gains this schedule behaviour in the same implementation change, locked by the Repositories bUnit tests and the inventory row in `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`. Update the overview screenshot only when the status region's visible text changes enough that the published image would be wrong. Do not add a Playwright test that waits for 05:00 UTC or collects against GitHub.
