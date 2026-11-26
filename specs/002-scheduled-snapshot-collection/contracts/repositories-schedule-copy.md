# Contract: Repositories schedule copy

Extends [specs/001-operator-snapshot-scanner/contracts/repositories-page.md](../../001-operator-snapshot-scanner/contracts/repositories-page.md). Route remains `/repositories`. No new page, no settings page, no retry control.

Feature 001 said this page does not show schedule copy. This feature replaces that sentence. Durability copy for a local or ephemeral file remains. Schedule copy and the account scan status are added in the same status alert, not a second alert and not a snackbar.

In hosted sign-in the current scan state is the reported account scan status from [account-scan-status.md](account-scan-status.md). The page renders `StatusSentence` and `ScheduleSentence` from Application. It does not infer the state from the clock, from a missing visit, or from whether a worker seems to be absent. It does not format 05:00 UTC. It does not say who started the run. It does not use the phrase daily occasion.

## Actions

| Control | Behaviour |
| --- | --- |
| **Reload from GitHub** | Unchanged. Does not start a collection and does not change the snapshot, the status blob, the worker token, or the queue. |
| **Collect snapshot** | Still calls `StartAsync` when callers are not told `scheduled` or `running`. A reported `stalled` account may start. A missing worker token does not block it. The scan uses the signed-in session. Visible label and accessible name stay **Collect snapshot**. |
| **Refresh overnight access** | Shown only in hosted sign-in, only on this page, and only when the operator is signed in and `WorkerTokenRefreshOffered` is true. Starts the `worker-refresh` authorisation. Does not start a scan. Cancel or failure leaves the operator signed in and does not record a scan failure. Lean posture does not show it. |
| Page open | `GetStatusAsync` only. Does not call `StartAsync`. Does not fill the daily queue. Does not persist `stalled`. |

The in-progress poll (about once a second, disposed with the component) stays while callers are told `running`. It shows the recorded count from the running sentence, without a full reload, and without saying who started the run.

## Schedule sentence

UK English. No em dash. Times inside a status sentence stay `en-GB` local formatting of stored UTC. The schedule sentence does not contain a clock time.

One schedule sentence is included in the same `MudAlert` as the catalogue line, the snapshot clause, and the status sentence.

| Facts | Sentence |
| --- | --- |
| Lean posture | Nothing collects on a schedule. The snapshot is kept on this machine rather than promised on another machine. Do not show `scheduled`, `running`, `stalled`, or `suspended` from a worker. Do not show **Refresh overnight access**. |
| Hosted, and the next ask will queue them | The next collection runs overnight. |
| Hosted, status `scheduled`, and no refusal applies | The next collection runs overnight. The status sentence is the `scheduled` sentence. Do not say the day was missed. Do not show the running sentence until the worker has taken the entry. |
| Hosted, not allowed | No scheduled collection will run because you are not allowed to use the product. This row wins when the 30-day window is also closed or a stored-access wait is set. The status sentence is the not-allowed sentence. Do not show the running sentence and do not present a failed snapshot for that removal. When a stored-access wait is set, the status sentence still shows that failed run and the sign-in or token reason. A previous successful snapshot stays. |
| Hosted, allowed, and `credentialUnusable` | No scheduled collection will run until you refresh the access used for the overnight collection. The status sentence is the suspended token sentence. Do not add a second sentence for a closed 30-day window. Do not say that the next collection runs overnight. |
| Hosted, allowed, not in a stored-access wait, outside the 30-day window | No scheduled collection will run until you sign in again. |

When the worker token is missing, the operator is otherwise eligible, and the stored-access wait is not set, the schedule sentence stays "The next collection runs overnight." and the page also shows **Refresh overnight access**. When the schedule sentence is already the refresh sentence, the control is the action and that sentence is not repeated beside itself. When the worker token is present and that failure is not the current block, the control is hidden.

There is no control to change the time, how often, or the 30 days.

## Status sentences

Use these sentences in the same alert. Do not invent a stored `stalled` value. When a successful snapshot exists, the status sentence ends with "The last successful snapshot was saved at {last success time}." When none exists, that ending is omitted.

| Reported status | Sentence |
| --- | --- |
| `never run` | No collection has run yet. |
| `scheduled` | A collection is waiting to start. Not running, not finished, and no failure reason. Tries are not listed. |
| `running` | A collection is running. It started at {started at}. {recorded count} repositories recorded so far. |
| `finished`, result success | The last collection finished successfully at {finish time}. {saved count} repositories were saved. The stored comment Success is not shown as the word Success. |
| `finished`, result failure | The last collection finished at {failed at} without saving a snapshot because of {failure comment}. `{failure comment}` is one of "a sign-in or token problem", "a GitHub rate limit", or "an unexpected error". |
| `suspended`, not allowed | Scheduled collection is paused because you are not allowed to use the product. |
| `suspended`, stored access | Scheduled collection is paused until you refresh the access used for the overnight collection. The collection at {failed at} did not save a snapshot because of a sign-in or token problem. |
| `stalled` | This collection has stopped reporting progress. Do not say running, do not say the site stopped, and do not use the three short failure reasons. The previous successful snapshot stays. Partial recordings are not a new success. **Collect snapshot** remains available when they can still use the product. |

None of these sentences say who started the run.

## Guide

`website/content/docs/repositories.md` gains this schedule behaviour, these sentences, and the overnight-access refresh in the same implementation change, locked by the Repositories bUnit tests and the inventory row in `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`. Update the overview screenshot only when the status region's visible text changes enough that the published image would be wrong. Do not add a Playwright test that waits for 05:00 UTC or collects against GitHub.
