# Contract: Repositories schedule copy

Route remains `/repositories`. No new page and no control that changes 05:00 UTC, the daily cadence, or the 30 days. **Reload from GitHub** still does not start a collection.

This contract adds schedule copy to the existing snapshot alert. It replaces the feature 001 sentence that forbade schedule copy on this page. Durability and schedule share that one alert. Do not add a second alert or a snackbar for the same fact.

## Start control

**Collect snapshot** stays in the command strip on narrow viewports. It is disabled when status is `InProgress` or `QueuedBeforeStart` is true. Choosing it while disabled does nothing. A daily failure does not add a retry button.

## Copy

UK English. No em dash. The occasion time is the literal `05:00 UTC`. Do not convert that clock time to the operator's zone. Finish times of runs stay on the existing `en-GB` local formatting.

| Situation | What the alert adds |
| --- | --- |
| Lean posture | Nothing collects on a schedule. The snapshot is kept on this machine rather than promised on another machine. |
| Hosted, next occasion will queue them, including before any run | The next scheduled collection is {today or the following day} at 05:00 UTC. |
| Hosted, queued before the first pass | The same 05:00 UTC occasion. Do not say the day was missed. Do not say the run is in progress. |
| Hosted, daily run in progress, including a further try waiting at the back | In progress, started by the daily collection, start time, recorded count. Do not list tries. Do not say failed. |
| Hosted, hand-started run | Started by the operator. Do not call it the daily occasion. |
| Hosted, schedule hold after sign-in or token failure | The failed run and that reason. No scheduled collection will run until they sign in again. Do not name a 05:00 UTC occasion. |
| Hosted, sign-in older than 30 days, or the next 05:00 UTC would fall outside the window | No scheduled collection will run until they sign in again. |
| Hosted, not allowed | No scheduled collection will run because they are not allowed. |
| Hosted, rate limit, unexpected error, or interruption, and a later day will still queue them | That outcome, and the next occasion as 05:00 UTC. |

When they are allowed to start by hand, the page still shows that **Collect snapshot** is available, except while queued or in progress.

Live updates stay on the existing poll while the component is alive and the kind is `InProgress`. Leaving the page does not cancel a daily walk.

## Guide

`website/content/docs/repositories.md` currently says collection runs only when the operator chooses the button and not on a schedule. The same change updates that page so hosted sign-in and the lean posture match this contract, and updates the locking bUnit tests and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` if the inventory row changes. Recapture `repositories/overview.png` only when the visible command strip or alert text in that shot changes. Do not add a Playwright journey that performs a GitHub collection.
