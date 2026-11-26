# Contract: daily collection schedule

Normative shapes for the occasion marker and the queue message. Behaviour is in [data-model.md](../data-model.md) and [research.md](../research.md). Operators do not see these documents.

## Occasion marker

Container: `collection-schedule`. Blob name: `occasions/{yyyy-MM-dd}.json`. Created on the first fill of that UTC date. A later fill the same date replaces it. This delivery's timer asks at 05:00 UTC. A catch-up or an `unmetFill` may write `filledAt` at any other time. The blob existing does not refuse that later fill.

```json
{
  "occasionDate": "2026-11-23",
  "filledAt": "2026-11-23T05:00:00Z",
  "operators": ["octocat", "hubot"]
}
```

A UTC date that was never filled has no blob. A second copy that loses the lease retries the fill and does not queue someone who is already queued or in progress. Host start may enqueue a login whose status is `scheduled`, whose queue message is still pending, whose `queueVisibleAfter` is null or already passed, and who has no visible message. At or after 05:00 UTC, host start also fills when this blob is absent. It does not fill when the blob is already present.

## Queue message

Queue resource: `operator-snapshot-queues`. Queue name: `daily-snapshot-collection`.

Initial:

```json
{
  "occasionDate": "2026-11-23",
  "operatorLogin": "octocat",
  "kind": "initial"
}
```

Further try after a rate limit or unexpected error:

```json
{
  "occasionDate": "2026-11-23",
  "operatorLogin": "octocat",
  "kind": "retry",
  "notBefore": "2026-11-23T05:47:00Z"
}
```

`notBefore` is required on `retry` and on `unmetFill`, and absent on `initial`. The publisher sets the message's initial visibility timeout so the message cannot be received before `notBefore`. Rate-limit delays are the GitHub retry delay already on that failed response, otherwise 2 minutes. Unexpected-error delays are 3, 6, then 12 minutes for the first, second, and third further try, measured from the failed attempt.

Later fill after a terminal rate-limit or unexpected-error outcome with no further tries left. A liveness lapse does not send this message. This message has no login and does not start a pass:

```json
{
  "kind": "unmetFill",
  "notBefore": "2026-11-23T06:12:00Z"
}
```

`unmetFill` is hidden for 15 minutes. The handler calls the same fill as the 05:00 timer, including when a scan already succeeded. It does not walk GitHub. A `signInOrToken` failure does not send this message. The message may become visible before 05:00 UTC, and that still runs the fill.

The worker runs one collection message at a time. A message for a login already on the queue is not sent again for the same `kind` and occasion except for the fill repair above (`initial` only, and only if still queued). Each product retry is one new `retry` message at the tail, and only when `attemptsUsed` is still below 4. Receiving a collection message at any time of day, or on a later UTC day, does not by itself complete it or skip the person. A previous success the same day does not complete it either.

No message contains a token, a repository list, or a failure reason.

## Worker outcomes

Account scan status values are the stored tokens in [account-scan-status.md](account-scan-status.md).

| Condition | Queue | Account scan status | Snapshot |
| --- | --- | --- | --- |
| Pass succeeds | Message completed. No further retry. | `finished`, result success, comment `Success`. Clear `queueMessagePending`. | `succeeded`, trigger `daily`, `retainedRun` cleared. |
| `signInOrToken`, no newer worker token | Message completed. No retry message. | `suspended`, result failure, sign-in or token comment. | `failed`, that reason. Sign-in record `credentialUnusable` true. `retainedRun` cleared. |
| `signInOrToken`, newer worker token already stored | Message completed. No retry message. No `unmetFill`. | `finished`, result failure, sign-in or token comment, `suspensionReason` empty. | `failed`, that reason. `credentialUnusable` stays false. `retainedRun` cleared. |
| `rateLimit` or `unexpected`, tries left | Message completed. One `retry` at the tail, invisible until `notBefore`. | `scheduled`. Last result unchanged. `queueMessagePending` true. `queueVisibleAfter` is `notBefore`. | Previous terminal run restored from `retainedRun` so the wait is not shown as a failure. `attemptsUsed` kept. |
| `rateLimit` or `unexpected`, no tries left | Message completed. No retry message. One `unmetFill`, hidden for 15 minutes. | `finished`, result failure, that comment. Clear `queueMessagePending`. | `failed`, one reason. `retainedRun` cleared. |
| Not allowed now, and the scan has not started | Message completed. No retry message. The 30-day window is not a reason to take this row. | One write to `suspended`. Do not set `running`. `suspensionReason` `notAllowed`. Clear `queueMessagePending`. Last scan time, result, and result comment unchanged. | No new `failed` run. `repositories` and `lastSuccessfulCollectionAt` unchanged. If a further try was waiting, restore `retainedRun`. |
| Stored access cannot be used, token exchange failed before the catalogue walk, no newer worker token | Message completed. No retry message. No `unmetFill`. | `suspended`, result failure, sign-in or token comment, `suspensionReason` null. Clear `queueMessagePending`. | `failed`, reason `signInOrToken`. Sign-in record `credentialUnusable` true. `retainedRun` cleared. |
| Same failure, newer worker token already stored | Message completed. No retry message. No `unmetFill`. | `finished`, result failure, sign-in or token comment, `suspensionReason` null. Clear `queueMessagePending`. | `failed`, reason `signInOrToken`. `credentialUnusable` stays false. `retainedRun` cleared. |
| `notBefore` is still ahead | Message completed after a copy with the same `notBefore` is sent, hidden for the remaining wait. | Stays `scheduled`. | Unchanged. |
| Redelivery and this pass was already `running` | Message completed. No retry message. No `unmetFill`. The pass does not rejoin. | Stays `running`. Clear `queueMessagePending` for that same generation only. Callers are told `stalled` only after more than 5 minutes without a mark. | Not rewritten to `interrupted` or `siteStopped`. Do not clear `retainedRun`. Partial recordings are not a new success. |
| `unmetFill` and `notBefore` has passed | Message completed. Run the same fill, at any time of day. | Unchanged by the message itself. A selected account, including one reported `stalled` and one whose scan already succeeded, becomes `scheduled` when the fill wins the etag. | Unchanged by the message itself. |
| Another copy holds `runner` | Current message completed after a new copy of the same payload is sent to the tail. A `retry` or `unmetFill` stays hidden until `notBefore`. When `notBefore` has already passed, hide it for 30 seconds so it does not spin against the pass. | Unchanged by the loser. | Unchanged by the loser. |

Hand-started collections do not send these messages and do not take the `runner` lease. They update the same account scan status. They refuse to start while callers are told `scheduled` or `running`. They may start while callers are told `stalled`.
