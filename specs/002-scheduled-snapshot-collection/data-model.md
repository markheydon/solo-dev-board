# Data model: Scheduled snapshot collection

The operator snapshot document from [specs/001-operator-snapshot-scanner/data-model.md](../001-operator-snapshot-scanner/data-model.md) stays the single current catalogue snapshot per operator. Hosted sign-in also stores one account scan status blob per account. That blob is the current scan state. There is still no application database and no run history.

Lean posture uses the file adapter only. It does not write sign-in blobs, status blobs, occasion markers, or queue messages. Hand-started collection there stays the feature 001 run. It has no account scan status and no `stalled` report.

## Account scan status

One JSON blob per hosted account in container `account-scan-status`. Blob name `{sanitised-login}.json`. Same login grammar as the snapshot. Created when that account is first recorded for later scanning. The completed hosted sign-in path creates it when the browser refresh token is stored, if the blob is absent, including when the worker token is not stored. A fill may create it for a sign-in record that has no status blob yet. A later sign-in does not reset a blob that already exists. The record stays after the 30-day window closes. Not created in lean posture. Not deleted on sign-out. Conditional write on etag. No refresh token.

`schemaVersion` is `1`.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Operator login | `operatorLogin` | string | Sanitised GitHub login. |
| Status | `status` | string | `neverRun`, `scheduled`, `running`, `finished`, or `suspended`. `stalled` is not stored. |
| Last scan at | `lastScanAt` | string (ISO-8601 UTC) or `null` | When the last completed scan ran. Null when none has run. Not changed when no scan ran. |
| Last result | `lastResult` | string or `null` | `success`, `failure`, or null. Null when none has run. |
| Result comment | `resultComment` | string | `Success` when the last run succeeded. The failure detail when it failed. Empty when none has run. Not changed when no scan ran. |
| Suspension reason | `suspensionReason` | string or `null` | `notAllowed` or null. Set to `notAllowed` only when dequeue refuses because they are not allowed. Null on a stored-access suspension, and null whenever status is not that refusal. Cleared when a new scan is set to `scheduled` or `running`, and on `finished`. A fact, not a sentence. The page maps `notAllowed` to the fixed comment. |
| Liveness at | `livenessAt` | string (ISO-8601 UTC) or `null` | Latest mark while `status` is `running`. Null otherwise. |
| Running since | `runningSince` | string (ISO-8601 UTC) or `null` | When this `running` generation started. Used only when `livenessAt` is null. |
| Scan generation | `scanGeneration` | number | Starts at 0. Incremented when a new scan wins (`scheduled` from a fill, or `running` from a hand start). |
| Queue message pending | `queueMessagePending` | boolean | True only after a queue message for this account has been accepted and not yet completed, including a message that is not yet visible. |
| Queue visible after | `queueVisibleAfter` | string (ISO-8601 UTC) or `null` | Copy of the message `notBefore`. Null on an `initial` message. A winning fill that enqueues an `initial` sets this null. |

### Reported status

Readers use `TimeProvider.GetUtcNow()`. They do not write. When a hosted sign-in record exists and this blob is absent, readers report `never run` and do not create the blob. The next completed sign-in, or the next fill before eligibility, creates the `neverRun` shape. That report is not an inference of `running`, `finished`, `failed`, or `suspended`.

| Stored `status` | Extra test | Told to callers |
| --- | --- | --- |
| `neverRun` | | `never run` |
| `scheduled` | | `scheduled` |
| `running` | Latest of `livenessAt`, or `runningSince` when the mark is null, is 5 minutes old or newer | `running` |
| `running` | That instant is strictly more than 5 minutes ago | `stalled` |
| `finished` | | `finished` |
| `suspended` | | `suspended` |

Already queued means told `scheduled`. In progress means told `running`. A reported `stalled` account is neither. An entry that exists but is not yet visible is `scheduled` with `queueMessagePending` true.

### Transitions

```text
sign-in completes, blob absent --> neverRun, empty last scan, suspensionReason null, generation 0
sign-in completes, blob present --> blob unchanged (credentialUnusable is on the sign-in record)
fill, sign-in record present, blob absent --> same neverRun insert before eligibility; do not reset a blob that exists

before fill, scheduled and queueMessagePending false
    lastResult null --> neverRun, last scan fields unchanged, no failure comment
    lastResult set --> finished, last scan fields unchanged, no failure comment
    then the same fill applies eligibility and may set scheduled

eligible at fill, first etag wins --> scheduled, suspensionReason null, generation + 1, queueVisibleAfter null, then enqueue, queueMessagePending true
lost etag --> no enqueue, no snapshot change

worker takes entry --> running, livenessAt = now, runningSince = now
    still working --> livenessAt at least every 60 seconds, same generation
    --> success --> finished, lastResult success, comment Success, snapshot replaced
    --> signInOrToken, no newer worker token stored
          --> suspended, lastResult failure, sign-in or token comment, credentialUnusable
    --> signInOrToken, newer worker token already stored
          --> finished, lastResult failure, sign-in or token comment, suspensionReason null
          credentialUnusable stays false, no further try
    --> rateLimit or unexpected, attempts left
          --> scheduled, queueMessagePending true, queueVisibleAfter = notBefore
          last result unchanged until the try sequence ends
    --> rateLimit or unexpected, no tries left
          --> finished, lastResult failure, that comment
          --> unmetFill hidden for 15 minutes
    --> worker dies --> stored status stays running, message completed, no unmetFill
          after more than 5 minutes callers are told stalled
          same generation may still commit a real result
          a newer generation must not be overwritten by that pass

dequeue, not allowed, scan not started
    --> suspended, suspensionReason notAllowed
    lastResult, lastScanAt, resultComment, and the successful snapshot unchanged
dequeue, worker token missing or unusable, before the catalogue walk, no newer worker token
    --> suspended, lastResult failure, sign-in or token comment, suspensionReason null, credentialUnusable, no unmetFill
dequeue, same failure, newer worker token already stored
    --> finished, lastResult failure, sign-in or token comment, suspensionReason null, wait not set, no unmetFill

worker token stored, and status is suspended for that token failure
    --> finished, keep lastResult failure and the sign-in or token comment, suspensionReason null
worker token stored, status is suspended for notAllowed
    --> status unchanged

hand start, first etag wins --> running, suspensionReason null, and a new liveness mark (no queue message)
    uses the signed-in session access token, not the worker token
hand start or fill loses the etag --> no second scan
```

A hand start is refused while callers are told `scheduled` or `running`. It is allowed while they are told `stalled`, `finished`, `suspended`, or `never run`, subject to the existing permission to use the product. A missing worker token does not refuse it. A successful scan does not block a later fill. A `credentialUnusable` sign-in record is not queued. A missing worker token with `credentialUnusable` false is still queued, and that scan then fails. `unmetFill` is only the next ask after a finished rate-limit or unexpected-error outcome. A liveness lapse does not publish it. A token failure does not publish it.

## Snapshot document

`schemaVersion` stays `1`. The catalogue fields from feature 001 stay. `trigger` may still say `operator` or `daily`. The account scan status does not copy that fact, and FR-025 text does not read it.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Trigger | `trigger` | string or absent | `operator` or `daily`. Set to `daily` when a daily pass starts, not when the account is only `scheduled`. |
| Attempts used | `attemptsUsed` | number | Daily run only. `1` on the first pass. Each further try increments it, up to `4`. Cleared when a further try is removed because they are not allowed. Not used for a hand-started run. |
| Retained run | `retainedRun` | object or `null` | Copy of the terminal `run` and its `trigger` taken when this occasion's first pass replaces that run. Restored if a further try is removed because the person is not allowed, and while a further try waits so the page does not show a failure between tries. Cleared when this occasion succeeds or fails. Not a history. |

Do not write `queuedOccasion`, `passExecuting`, or `passLeaseUntil` for new behaviour. Do not add `scheduledCollectionStopped`. Existing `run.reason` values stay `signInOrToken`, `rateLimit`, `siteStopped`, and `unexpected`. `siteStopped` remains the lean hand-started stop. A hosted worker death does not write it and does not write `interrupted` on the snapshot.

Repositories and `lastSuccessfulCollectionAt` still change only on success. Partial recordings are not a new successful snapshot, including when callers are told `stalled`. Identity, sanitised keys, and etag rules are unchanged. Unsafe keys are not queued. The status blob does not contain a token.

## Hosted sign-in record

One JSON blob per operator in container `hosted-sign-ins`. Blob name `{sanitised-login}.json`. Same login grammar as the snapshot. Schema version 2. The browser refresh token is not a field on this blob. It stays in the auth cookie. Conditional write on etag. Not created in lean posture. Not deleted on sign-out.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Schema version | `schemaVersion` | number | `2` on write. A stored `1` is read as worker token absent. Its `refreshToken` is not copied and is not exchanged. |
| Operator login | `operatorLogin` | string | Sanitised GitHub login. |
| Completed at | `completedAt` | string (ISO-8601 UTC) | Latest completed hosted sign-in. Set when the browser refresh token is stored, including when the worker token is not. A worker-token refresh does not change it. A sign-in that does not complete does not write. |
| Organisation logins | `organisationLogins` | array of strings | Logins saved from that completed sign-in. Replaced on the next completed sign-in. Not replaced by a worker-token refresh. Not refreshed from GitHub at 05:00 UTC. |
| Worker refresh token | `workerRefreshToken` | string or absent | Secret for overnight collection only. Absent when no second authorisation has been kept. Never copied to the snapshot, the account scan status, the queue, or logs. |
| Worker refresh token expires | `workerRefreshTokenExpiresAt` | string or `null` | UTC expiry when GitHub sent one. |
| Credential unusable | `credentialUnusable` | boolean | `true` after an overnight scan fails with `signInOrToken` and no newer worker token is stored. Cleared only when a worker token is kept. A completed sign-in that does not store a worker token does not clear it. |

At fill, the 30-day window is `completedAt` compared with the instant that fill runs. A fill at 05:00 UTC uses 05:00 UTC. A fill at another time uses that later instant. A sign-in after 05:00 UTC is inside a fill that runs after that sign-in. Dequeue does not apply this comparison. A `completedAt` exactly 30 days before the fill instant is inside. Older is outside. A successful collection does not change `completedAt`. Whether the next ask will queue them is still judged at today 05:00 UTC before that instant, and at the following 05:00 UTC after it, including while they are `scheduled`. That judgement is not shown as a clock time. The schedule sentence is the overnight sentence when that ask will include them, otherwise one FR-025 refusal. A success does not change the sentence. The sentence does not forbid a fill that runs sooner.

## Occasion marker

One JSON blob per UTC date in container `collection-schedule`, name `occasions/{yyyy-MM-dd}.json`. The first fill of that date creates it with `IfNoneMatch = *` while the worker holds the fill lease. A later fill the same date replaces it under the etag. The blob existing is not a lock against another fill.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Occasion date | `occasionDate` | string (`yyyy-MM-dd`) | The UTC date. |
| Filled at | `filledAt` | string (ISO-8601 UTC) | The latest fill instant. May be after 05:15:00 UTC. |
| Operators | `operators` | array of strings | Union of logins queued for this UTC date so far. A later fill may add a login again after a success once they are not queued or in progress. |

A UTC date with no fill has no blob. The marker is not an operator-visible log. The Repositories page does not list it.

If status is `scheduled`, `queueMessagePending` is true, `queueVisibleAfter` is null or already passed, and a peek finds no visible message for that login, host start may enqueue the missing `initial` again. It must not do that when `queueVisibleAfter` is still ahead, and it must not add a second message for someone already queued or in progress. Host start at or after 05:00 UTC also runs a fill when today's marker is absent. It does not run a fill when the marker is already there.

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

Operator-facing liveness is `livenessAt` on the account scan status blob, not a lease in this container. `web` does not reference `collection-schedule`.

## Status facts returned to the page

`OperatorSnapshotStatusDto` gains the account scan facts and the two FR-025 strings. Application builds those strings. Web renders them and does not assemble its own status or schedule wording. In hosted sign-in the page does not derive the current scan state from the snapshot `run` or from the clock.

| Fact | Meaning |
| --- | --- |
| `SchedulePosture` | `lean` or `hosted`. |
| `AccountScanStatus` | Stored status, or none in lean posture. |
| `ReportedScanStatus` | Stored status, except `stalled` when the liveness rule says so. None in lean posture. |
| `LastScanAt` | From the status blob. Null when none has run. |
| `LastScanResult` | `success`, `failure`, or none. |
| `ResultComment` | From the status blob. Empty when none has run. Not the not-allowed comment. |
| `SuspensionReason` | `notAllowed` or none. From `AccountScanStatus.suspensionReason`. The formatter maps `notAllowed` to the FR-025 sentence. |
| `NoScheduleReason` | None, `lean`, `credentialUnusable`, `outsideWindow`, `notAllowed`. Input to the schedule sentence. Not rendered as its own label. |
| `StatusSentence` | The FR-025 sentence for the reported status. None in lean posture. |
| `ScheduleSentence` | The one FR-025 schedule sentence, or none when the next ask will not be named because posture is lean. |
| `WorkerTokenRefreshOffered` | True in hosted sign-in when the worker token is absent or `credentialUnusable` is set. False in lean posture. The page shows the control only when this is true and the operator is signed in. |
| `Trigger` | `operator`, `daily`, or none on the snapshot facts. Not stored on the account scan status. Not used to build either sentence. |
| `NextAskIncludesAccount` | True when the next 05:00 UTC ask would queue them. Not a timestamp and not rendered as a clock time. |

The next ask is today at 05:00 UTC when that instant has not yet passed and they would be eligible at that instant. After that instant, it is the following day at 05:00 UTC when they would be eligible then, including when today's scan has not succeeded and including when it has. It stays the current day's ask while they are `scheduled` and the schedule sentence is not a refusal. `NextAskIncludesAccount` is false when a refusal applies.

`NoScheduleReason` is chosen in this order: `notAllowed`, otherwise `credentialUnusable`, otherwise `outsideWindow`. `notAllowed` wins when they are also in a stored-access wait or outside the window. The status sentence still shows the stored-access failure when `credentialUnusable` is set. `outsideWindow` is judged at the next ask instant. A `scheduled` wait does not override `notAllowed`. A sign-in older than 30 days does not, by itself, clear a queued wait. Time of day alone does not set `outsideWindow`. Lean posture always uses `SchedulePosture = lean`, `NoScheduleReason = lean`, no account scan status, no schedule sentence from this catalogue, and `WorkerTokenRefreshOffered` false, including a personal Azure deploy that did not model the worker.

## Who writes what

| Type | Home | Notes |
| --- | --- | --- |
| Snapshot catalogue | Application document, Infrastructure store | Same port as feature 001. Not the scan status. |
| Account scan status | Application model, Infrastructure blob adapter | Web and the Functions host. Etag on every transition and liveness mark. |
| Sign-in record | Application model, Infrastructure blob adapter | Web writes `completedAt` and organisations when the browser token is stored, and writes the worker token only from a kept second authorisation or a worker-token refresh. The fill also inserts a missing `neverRun` blob for a sign-in record before eligibility. The worker updates worker-token rotation and `credentialUnusable`. It does not write the cookie token and it does not write liveness here. |
| Occasion marker and `runner` lease | Infrastructure, Functions host only | Web does not reference `collection-schedule`. |
| Queue messages | Infrastructure via injected `QueueServiceClient` | Functions host only. Further tries pass `notBefore` through to the initial visibility timeout and to `queueVisibleAfter`. |
| Eligibility, fill, and reported status | Application | `TimeProvider`, sign-in records, admission options, status blob. |
| Token exchange | Application port, Infrastructure HTTP | Shared by web and the worker. Triggers stay thin. |
