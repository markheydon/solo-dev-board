# Data model: Scheduled snapshot collection

The operator snapshot document from [specs/001-operator-snapshot-scanner/data-model.md](../001-operator-snapshot-scanner/data-model.md) stays the single current snapshot per operator. This feature adds fields on that document, a hosted sign-in blob, and schedule blobs the worker uses. There is still no application database and no run history.

Lean posture uses the file adapter only. It does not write sign-in blobs, occasion markers, or queue messages. Hand-started collection is unchanged there.

## Snapshot document (additive)

`schemaVersion` stays `1`. New properties are absent on documents written before this feature. Readers treat absence as the hand-started behaviour from feature 001.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Trigger | `trigger` | string or absent | `operator` or `daily`. Absent means `operator` for documents already stored. Set to `daily` when a daily pass starts, not when the login is only queued. A newer run replaces it. |
| Queued occasion | `queuedOccasion` | string (`yyyy-MM-dd`) or `null` | The UTC date of the daily occasion this operator is waiting on, before their pass has started. `null` when they are not waiting to start. Not set for a hand-started run. Cleared when the entry is removed because a run rule failed, without changing `run`. |
| Pass executing | `passExecuting` | boolean | `true` only while a daily pass is inside the worker. `false` when they are queued and have not started, when a further try is waiting at the back, and when the run is terminal. Absent means `false`. |
| Pass lease until | `passLeaseUntil` | string (ISO-8601 UTC) or `null` | Renewed about every 10 seconds while `passExecuting` is `true`. A reader treats the pass as dead when this is missing or earlier than now. `null` when no pass is executing. |
| Attempts used | `attemptsUsed` | number | Daily run only. `1` on the first pass. Each further try increments it, up to `4` (first pass plus 3 further tries). Absent means no daily attempt accounting. Not used for a hand-started run. Cleared when a further try is removed by a run rule. |
| Retained run | `retainedRun` | object or `null` | Copy of the terminal `run` and its `trigger` taken when this occasion's first pass replaces that run. Restored if a later further try is removed because the person is not allowed. Cleared when this occasion succeeds, fails (including `signInOrToken`), or is interrupted. Absent when no daily pass has replaced a terminal run. Not a history. |

`run.status` values stay `inProgress`, `succeeded`, `failed`, and `interrupted`.

`run.reason` gains `scheduledCollectionStopped`. Existing values stay `signInOrToken`, `rateLimit`, `siteStopped`, and `unexpected`.

### How the page should read the combination

| `queuedOccasion` | `passExecuting` | `run.status` | Operator-visible state |
| --- | --- | --- | --- |
| today, not started | `false` | previous terminal status, or absent run | Not in progress. While they remain allowed and are not in a stored-access wait, the page still names today's 05:00 UTC. If they are no longer allowed, the one schedule sentence is the not-allowed sentence, and this wait is not shown as in progress. A sign-in older than 30 days does not, by itself, change this row while they are still queued. Start control does not start another while `queuedOccasion` is set. |
| set | `true` | `inProgress` | In progress. Daily occasion started it. |
| `null` | `false` | `inProgress` | In progress, further try waiting at the back. Tries are not listed. |
| `null` | `false` | `interrupted` and reason `scheduledCollectionStopped` | Interrupted because the scheduled collection stopped. Does not rejoin the queue. |
| `null` | `false` | `interrupted` and reason `siteStopped` | Hand-started run. The site stopped. |
| `null` | `false` | `failed` or `succeeded` | Terminal, as feature 001, plus trigger copy. |

Waiting on the queue is not a new `run.status`. The previous run stays visible until the pass starts. A further try keeps `inProgress` so the page does not show a failure between tries.

### Transitions (daily)

```text
eligible at fill --> queuedOccasion = today (status and trigger unchanged)
                 --> pass starts --> inProgress, passExecuting, trigger = daily
                       retainedRun = previous terminal run, when there was one
                       --> success --> succeeded, clear queue flags, attempts, retainedRun
                       --> signInOrToken --> failed, no requeue, credentialUnusable, clear retainedRun
                       --> rateLimit or unexpected, attemptsUsed < 4
                             --> inProgress, passExecuting false
                             --> tail message with notBefore (invisible until then)
                       --> rateLimit or unexpected, attemptsUsed = 4
                             --> failed, clear retainedRun, enqueue unmetFill hidden for 15 minutes
                       --> worker dies while passExecuting
                             --> interrupted / scheduledCollectionStopped, no rejoin, clear retainedRun
                             --> enqueue unmetFill hidden for 15 minutes
queued, not started, not allowed
                 --> clear queuedOccasion only, no failed run, message completed
retry waiting, not allowed
                 --> restore retainedRun, clear attempt fields, no failed run, message completed
queued or retry waiting, stored access cannot be used
                 --> failed / signInOrToken, credentialUnusable, no retry, no unmetFill, retainedRun cleared
queued or retry waiting, worker dies --> flags unchanged, message remains, not interrupted
message received before notBefore --> re-queue for the remaining wait, pass does not start
```

A hand-started start is refused while `queuedOccasion` is set or `run.status` is `inProgress` (including a retry waiting at the back). A daily fill does not add a second entry when they are already queued or a collection for them is in progress. A successful run does not block a later fill. There is no cooldown after a finished scan. A `credentialUnusable` sign-in record is not queued. The 15-minute hide on `unmetFill` is the next ask after a terminal technical outcome. It is not a filter inside eligibility.

Run rules at dequeue attempt the scan when the person is still allowed, including when the sign-in is now older than 30 days. They do not re-check that window. A message is not removed because of the clock. Allowance uses the saved organisation logins and the allow lists as they stand at dequeue. When stored access cannot be used, the run fails with `signInOrToken`. A `failed` run with no `finishedAt`, no `reason`, and no `lastSuccessfulCollectionAt` is read as never run, so a not-allowed withdrawal does not invent a failure. That reading also covers a document created only to hold `queuedOccasion` for someone who has never been collected.

Repositories and `lastSuccessfulCollectionAt` still change only on success. Identity, sanitised keys, and etag rules are unchanged. Unsafe keys are not queued.

## Hosted sign-in record

One JSON blob per operator in container `hosted-sign-ins`. Blob name `{sanitised-login}.json`. Same login grammar as the snapshot. Written only by the web host when a hosted sign-in completes. Updated by the worker when a refresh token rotates or a daily pass sets `credentialUnusable`. Conditional write on etag. Not created in lean posture. Not deleted on sign-out.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Schema version | `schemaVersion` | number | `1`. |
| Operator login | `operatorLogin` | string | Sanitised GitHub login. |
| Completed at | `completedAt` | string (ISO-8601 UTC) | Latest completed hosted sign-in. A sign-in that does not complete does not write. |
| Organisation logins | `organisationLogins` | array of strings | Logins saved from that completed sign-in. Replaced on the next completed sign-in. Not refreshed from GitHub at 05:00 UTC. |
| Refresh token | `refreshToken` | string | Secret. Never copied to the snapshot, the queue, or logs. |
| Refresh token expires | `refreshTokenExpiresAt` | string or `null` | UTC expiry when GitHub sent one. |
| Credential unusable | `credentialUnusable` | boolean | `true` after a daily pass fails with `signInOrToken`. Cleared only by the next completed sign-in, which also sets `completedAt`. |

At fill, the 30-day window is `completedAt` compared with the instant that fill runs. A fill at 05:00 UTC uses 05:00 UTC. A fill at another time uses that later instant. A sign-in after 05:00 UTC is inside a fill that runs after that sign-in. Dequeue does not apply this comparison. A `completedAt` exactly 30 days before the fill instant is inside. Older is outside. A successful collection does not change `completedAt`. The page names today at 05:00 UTC when that instant has not yet passed and they would be eligible then, and it keeps today while they are queued and still allowed. After today's 05:00 UTC ask has passed, and they are not still queued, it names the following day at 05:00 UTC when they would be eligible then. A success does not change that name. That name is the next planned ask. It does not forbid a fill that runs sooner.

## Occasion marker

One JSON blob per UTC date in container `collection-schedule`, name `occasions/{yyyy-MM-dd}.json`. The first fill of that date creates it with `IfNoneMatch = *` while the worker holds the fill lease. A later fill the same date replaces it under the etag. The blob existing is not a lock against another fill.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Occasion date | `occasionDate` | string (`yyyy-MM-dd`) | The UTC date. |
| Filled at | `filledAt` | string (ISO-8601 UTC) | The latest fill instant. May be after 05:15:00 UTC. |
| Operators | `operators` | array of strings | Union of logins queued for this UTC date so far. A later fill may add a login again after a success once they are not queued or in progress. |

A UTC date with no fill has no blob. The marker is not an operator-visible log. The Repositories page does not list it.

If a login is still `queuedOccasion` for that date with no queue message (worker died during fill), host start may enqueue that missing `initial` message again. It must not add a second message for someone already queued or in progress. Host start at or after 05:00 UTC also runs a fill when today's marker is absent. It does not run a fill when the marker is already there.

## Queue message

Queue `daily-snapshot-collection`. JSON text body. No token, no repository list.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Occasion date | `occasionDate` | string (`yyyy-MM-dd`) or absent | The occasion being collected. Absent on `unmetFill`. |
| Operator login | `operatorLogin` | string or absent | Sanitised login. Absent on `unmetFill`. |
| Kind | `kind` | string | `initial`, `retry`, or `unmetFill`. Retries are a new message at the tail, not a platform redelivery. `unmetFill` asks for another fill. It is not a collection. |
| Not before | `notBefore` | string (ISO-8601 UTC) or absent | Required on `retry` and `unmetFill`. The message must not be handled before this instant. The publisher hides it until then. Absent on `initial`. |

## Schedule leases

Both blobs live in `collection-schedule`. They are lease targets, not operator-facing documents.

| Blob | Holder | Rule |
| --- | --- | --- |
| `runner` | The fill tick, then whichever copy is running the one daily pass | Only one filler and only one daily pass at a time. Released between passes so the next account can run. A copy that cannot acquire it re-sends its message to the tail. A retry keeps the same `notBefore` and stays invisible until that instant. |
| (none per login) | `passLeaseUntil` on the snapshot | Liveness of the executing pass. Avoids a second blob per operator. |

## Status facts returned to the page

`OperatorSnapshotStatusDto` gains facts. The page owns UK English. Application does not return sentences.

| Fact | Meaning |
| --- | --- |
| `Trigger` | `operator`, `daily`, or none when never run. |
| `SchedulePosture` | `lean` or `hosted`. |
| `QueuedForOccasion` | UTC date when they are waiting to start, else none. |
| `NextOccasionAtUtc` | The next 05:00 UTC that will queue them, or none. |
| `NoScheduleReason` | None, `lean`, `credentialUnusable`, `outsideWindow`, `notAllowed`. |

`NextOccasionAtUtc` is today at 05:00 UTC when that instant has not yet passed and they would be eligible at that instant. After that instant, it is the following day at 05:00 UTC when they would be eligible then, including when today's scan has not succeeded and including when it has. It stays today while they are queued for today and the schedule sentence is not a refusal. It is none when a refusal applies.

`NoScheduleReason` is chosen in this order, before the next-occasion time: `notAllowed`, otherwise `credentialUnusable`, otherwise `outsideWindow`. `notAllowed` wins when they are also in a stored-access wait or outside the window. The page still shows that failed run when `credentialUnusable` is set. `outsideWindow` is judged at the instant the page would name (today at 05:00 UTC before that instant, otherwise the following day at 05:00 UTC). A queued wait does not override `notAllowed`. A sign-in older than 30 days does not, by itself, clear a queued wait. Time of day alone does not set `outsideWindow`. Lean posture always uses `SchedulePosture = lean`, `NoScheduleReason = lean`, and no next occasion, including a personal Azure deploy that did not model the worker.

## Who writes what

| Type | Home | Notes |
| --- | --- | --- |
| Snapshot fields above | Application document, Infrastructure store | Same port as feature 001. |
| Sign-in record | Application model, Infrastructure blob adapter | Web writes on completed sign-in. Worker updates token rotation and `credentialUnusable`. |
| Occasion marker and `runner` lease | Infrastructure, Functions host only | Web does not reference `collection-schedule`. |
| Queue messages | Infrastructure via injected `QueueServiceClient` | Functions host only. Further tries pass `notBefore` through to the initial visibility timeout. |
| Eligibility and fill | Application | `TimeProvider`, sign-in records, admission options, snapshot flags. |
| Token exchange | Application port, Infrastructure HTTP | Shared by web and the worker. Triggers stay thin. |
