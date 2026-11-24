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
| Retained run | `retainedRun` | object or `null` | Copy of the terminal `run` and its `trigger` taken when this occasion's first pass replaces that run. Restored if a later further try is removed because the person is not allowed or is outside 30 days. Cleared when this occasion succeeds, fails, or is interrupted. Absent when no daily pass has replaced a terminal run. Not a history. |

`run.status` values stay `inProgress`, `succeeded`, `failed`, and `interrupted`.

`run.reason` gains `scheduledCollectionStopped`. Existing values stay `signInOrToken`, `rateLimit`, `siteStopped`, and `unexpected`.

### How the page should read the combination

| `queuedOccasion` | `passExecuting` | `run.status` | Operator-visible state |
| --- | --- | --- | --- |
| today, not started | `false` | previous terminal status, or absent run | Not in progress. If they are still eligible for that occasion, the page still names today's 05:00 UTC. If a run rule already fails at read time, the page states that no scheduled collection will run, and why, and still does not show this wait as in progress. Start control does not start another while `queuedOccasion` is set. |
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
                       --> rateLimit or unexpected, attemptsUsed = 4 --> failed, clear retainedRun
                       --> worker dies while passExecuting
                             --> interrupted / scheduledCollectionStopped, no requeue, clear retainedRun
queued, not started, run rule fails at dequeue
                 --> clear queuedOccasion only, no failed run, message completed
retry waiting, run rule fails at dequeue
                 --> restore retainedRun, clear attempt fields, no failed run, message completed
queued or retry waiting, worker dies --> flags unchanged, message remains, not interrupted
message received before notBefore --> re-queue for the remaining wait, pass does not start
```

A hand-started start is refused while `queuedOccasion` is set or `run.status` is `inProgress` (including a retry waiting at the back). A daily fill does not add a second entry when `queuedOccasion` is already that date, or when a daily run is still `inProgress`, or when the previous occasion's queue entry is still there.

Run rules at dequeue use the current UTC time. They are not the 05:00 UTC occasion instant. A message is not removed because that clock has passed. Allowance uses the saved organisation logins and the allow lists as they stand at dequeue. A `failed` run with no `finishedAt`, no `reason`, and no `lastSuccessfulCollectionAt` is read as never run, so removing a queue entry does not invent a failure. That reading also covers a document created only to hold `queuedOccasion` for someone who has never been collected.

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

At fill, the 30-day window is `completedAt` compared with the occasion instant (05:00 UTC, or the later fill instant inside the grace window). At dequeue, the same comparison uses the current UTC time, not that occasion instant. A `completedAt` exactly 30 days earlier is inside. Older is outside. A successful collection does not change `completedAt`. The page uses the next occasion instant it is about to name, which is the existing schedule-facts rule.

## Occasion marker

One JSON blob per UTC date in container `collection-schedule`, name `occasions/{yyyy-MM-dd}.json`. Created once with `IfNoneMatch = *` while the worker holds the fill lease.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Occasion date | `occasionDate` | string (`yyyy-MM-dd`) | The UTC date. |
| Filled at | `filledAt` | string (ISO-8601 UTC) | When the queue was filled. Inside 05:00:00 to 05:15:00 UTC inclusive. |
| Operators | `operators` | array of strings | Logins enqueued for this occasion, once each. |

No marker is written for a missed day. The marker is not an operator-visible log. The Repositories page does not list it.

If the marker exists and a listed login is still `queuedOccasion` for that date with no queue message (worker died during fill), host start may enqueue the missing login again. It must not create a second marker and must not add a login who was not on the marker.

## Queue message

Queue `daily-snapshot-collection`. JSON text body. No token, no repository list.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Occasion date | `occasionDate` | string (`yyyy-MM-dd`) | The occasion being collected. |
| Operator login | `operatorLogin` | string | Sanitised login. |
| Kind | `kind` | string | `initial` or `retry`. Retries are a new message at the tail, not a platform redelivery. |
| Not before | `notBefore` | string (ISO-8601 UTC) or absent | Retries only. The pass must not start before this instant. The publisher hides the message until then. Absent on `initial`. |

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

`NextOccasionAtUtc` is today at 05:00 UTC when that instant has not passed and they would be eligible. It stays today when they are already queued for today and they still pass the page's eligibility checks. It is the following day at 05:00 UTC when today's window was missed or skipped and they would still be eligible then. It is none when the reason is `credentialUnusable`, `outsideWindow`, `notAllowed`, or `lean`. A queued wait does not override those reasons: if they are no longer allowed, or the next named occasion would fall outside 30 days, the page states that no scheduled collection will run, and why. Time of day alone does not set `outsideWindow` for an entry that is still queued and still inside 30 days of now.

Lean posture always uses `SchedulePosture = lean`, `NoScheduleReason = lean`, and no next occasion, including a personal Azure deploy that did not model the worker.

## Who writes what

| Type | Home | Notes |
| --- | --- | --- |
| Snapshot fields above | Application document, Infrastructure store | Same port as feature 001. |
| Sign-in record | Application model, Infrastructure blob adapter | Web writes on completed sign-in. Worker updates token rotation and `credentialUnusable`. |
| Occasion marker and `runner` lease | Infrastructure, Functions host only | Web does not reference `collection-schedule`. |
| Queue messages | Infrastructure via injected `QueueServiceClient` | Functions host only. Further tries pass `notBefore` through to the initial visibility timeout. |
| Eligibility and fill | Application | `TimeProvider`, sign-in records, admission options, snapshot flags. |
| Token exchange | Application port, Infrastructure HTTP | Shared by web and the worker. Triggers stay thin. |
