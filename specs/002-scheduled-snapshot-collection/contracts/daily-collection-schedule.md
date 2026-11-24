# Contract: daily collection schedule

Normative shapes for the occasion marker and the queue message. Behaviour is in [data-model.md](../data-model.md) and [research.md](../research.md). Operators do not see these documents.

## Occasion marker

Container: `collection-schedule`. Blob name: `occasions/{yyyy-MM-dd}.json`. Created at most once per UTC date, only when the worker is inside 05:00:00 to 05:15:00 UTC inclusive and the blob is absent.

```json
{
  "occasionDate": "2026-11-23",
  "filledAt": "2026-11-23T05:00:00Z",
  "operators": ["octocat", "hubot"]
}
```

A missed day has no blob. A second copy that loses the create does not write another list and does not enqueue those logins again. Host start may enqueue a login who is named here and still has `queuedOccasion` set, when their message was never sent. It does not add anyone else.

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

`notBefore` is required on `retry` and absent on `initial`. The publisher sets the message's initial visibility timeout so the message cannot be received before `notBefore`. Rate-limit delays are the GitHub retry delay already on that failed response, otherwise 2 minutes. Unexpected-error delays are 3, 6, then 12 minutes for the first, second, and third further try, measured from the failed attempt.

The worker runs one message at a time. A message for a login already on the queue is not sent again for the same `kind` and occasion except for the fill repair above (`initial` only, and only if still queued). Each product retry is one new `retry` message at the tail, and only when `attemptsUsed` is still below 4. Receiving a message after 05:15 UTC, or on a later UTC day, does not by itself complete it or skip the person.

No message contains a token, a repository list, or a failure reason.

## Worker outcomes

| Condition | Queue | Snapshot |
| --- | --- | --- |
| Pass succeeds | Message completed. No further retry. | `succeeded`, trigger `daily`, queue flags and `retainedRun` cleared. |
| `signInOrToken` | Message completed. No retry message. | `failed`, that reason. Sign-in record `credentialUnusable` true. `retainedRun` cleared. |
| `rateLimit` or `unexpected`, tries left | Message completed. One `retry` message at the tail, invisible until `notBefore`. | Stays `inProgress`, `passExecuting` false. |
| `rateLimit` or `unexpected`, no tries left | Message completed. No retry message. | `failed`, one reason. `retainedRun` cleared. |
| Not allowed now, or sign-in outside 30 days of now, and no pass is executing | Message completed. No retry message. | No new `failed` or `interrupted` run. `repositories` and `lastSuccessfulCollectionAt` unchanged. Clear `queuedOccasion` if the pass has not started. If a further try was waiting, restore `retainedRun`. |
| `notBefore` is still ahead | Message completed after a copy with the same `notBefore` is sent, hidden for the remaining wait. | Unchanged. |
| Redelivery and `passExecuting` was already true | Message completed. No retry message. | `interrupted`, reason `scheduledCollectionStopped`. `retainedRun` cleared. |
| Another copy holds `runner` | Current message completed after a new copy of the same payload is sent to the tail. A retry stays hidden until `notBefore`. | Unchanged by the loser. |

Hand-started collections do not send these messages and do not take the `runner` lease. They still refuse to start while this operator is queued or `inProgress`.
