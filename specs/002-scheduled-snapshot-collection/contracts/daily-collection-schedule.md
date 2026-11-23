# Contract: daily collection schedule

Web is the only host. The scheduler is not an HTTP endpoint and not a Function. Razor calls the Application service and does not inject the store or `IGitHubService`.

## Registration

| Posture | Scheduler | Sign-in record store |
| --- | --- | --- |
| Hosted sign-in | `BackgroundService` registered | Blob prefix `schedule/` on `operator-snapshots` |
| PAT-only local trusted mode, including lean Azure deploy | Not registered | Not written |

The service uses `TimeProvider.GetUtcNow()` and `CancellationToken` from the host stopping token for the loop. A tick is 15 seconds. Constants, not operator settings: occasion `05:00:00` UTC, grace through `05:15:00` UTC, window 30 days, 3 further tries.

## Fill

On a tick, if `schedule/occasion.json` is absent or its `occasionDate` is not today's UTC date:

- If now is before 05:00:00 UTC, do nothing.
- If now is after 05:15:00 UTC, do nothing (missed day).
- Otherwise create the document once, using a conditional write. On conflict, re-read and do not append duplicates.

The create lists sign-in records and applies [data-model.md](../data-model.md) eligibility at that instant. It does not call GitHub.

## Drain

While the host is running, if the queue has an entry and no unexpired lease:

1. Conditional-write `activeLogin` and a renewed `leaseUntil` for the head entry.
2. On conflict, skip this tick.
3. Unprotect the refresh token, exchange it for an access token through the existing hosted GitHub token refresh path, and persist a rotated refresh token without changing `completedAt` or `organisationLogins`.
4. If that exchange fails, finish the run as `signInOrToken`, set `scheduleHold`, and remove the entry. Do not walk the catalogue.
5. Otherwise start the existing snapshot walk with `startedBy = dailyOccasion` and the captured access token. The request token of any open page must not cancel it.
6. Renew the lease until the pass finishes. One daily pass is active across replicas.
7. On `rateLimit` or `unexpected`, if `attemptsCompleted` is still below 3, move the entry to the back as `retryWaiting` and leave the snapshot `inProgress`. Otherwise store one failed run and remove the entry.
8. On success, store the snapshot as a hand-started success would, and remove the entry.

`waiting` does not set the snapshot to `inProgress`. `retryWaiting` requires the snapshot to already be `inProgress` for that daily run.

## Shutdown and crashed pass

On host stopping, if this process holds the lease and a pass is running, save `interrupted` / `siteStopped` and remove that entry. Entries still `waiting` or `retryWaiting` stay. A later tick whose `leaseUntil` is in the past treats that active pass as interrupted, removes it, and does not requeue it.

`GetStatusAsync` must not convert `inProgress` to `interrupted` while this login's occasion entry is `retryWaiting`.

## Hand start

`StartAsync` gains one refusal: when this operator's login is on today's queue (`waiting` or `retryWaiting`), return a distinct result `Queued` and do not start a walk. `AlreadyInProgress` remains the result when a walk is live or the snapshot run is `inProgress`. Lean posture never returns `Queued`.

Successful hand start sets `startedBy` to `operator` and does not enqueue further tries.

## Status DTO (additive)

Existing fields stay. Add:

| Field | Meaning |
| --- | --- |
| `StartedBy` | `Operator`, `DailyOccasion`, or null when never run. |
| `QueuedBeforeStart` | True only when this operator is `waiting` and the run has not started. The kind stays the previous terminal kind (or never run). |
| `Schedule` | One of `NextOccasion`, `NoneLean`, `NoneUntilSignIn`, `NoneNotAllowed`. |
| `NextOccasionAt` | 05:00 UTC on the named day when `Schedule` is `NextOccasion`. Otherwise null. |

`NextOccasion` is today at 05:00 UTC when that occasion will still queue them or they are already queued before the first pass. It is the following day at 05:00 UTC when today's occasion was missed or skipped and they will be eligible then. It is not named when the window, the hold, or admission will exclude them.

The DTO has no queue, no other operator, and no token.

## Catalogue

The daily walk calls the same `GetRepositoriesAsync(forceReload: true)` path as a hand-started walk. It does not add repositories the page cannot already show.
