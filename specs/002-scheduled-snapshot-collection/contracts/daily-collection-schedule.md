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
  "kind": "retry"
}
```

The worker runs one message at a time. A message for a login already on the queue is not sent again for the same `kind` and occasion except for the fill repair above (`initial` only, and only if still queued). Each product retry is one new `retry` message at the tail, and only when `attemptsUsed` is still below 4.

No message contains a token, a repository list, or a failure reason.

## Worker outcomes

| Condition | Queue | Snapshot |
| --- | --- | --- |
| Pass succeeds | Message completed. No further retry. | `succeeded`, trigger `daily`, queue flags cleared. |
| `signInOrToken` | Message completed. No retry message. | `failed`, that reason. Sign-in record `credentialUnusable` true. |
| `rateLimit` or `unexpected`, tries left | Message completed. One `retry` message at the tail. | Stays `inProgress`, `passExecuting` false. |
| `rateLimit` or `unexpected`, no tries left | Message completed. No retry message. | `failed`, one reason. |
| Redelivery and `passExecuting` was already true | Message completed. No retry message. | `interrupted`, reason `scheduledCollectionStopped`. |
| Another copy holds `runner` | Current message completed after a new copy of the same payload is sent to the tail. | Unchanged by the loser. |

Hand-started collections do not send these messages and do not take the `runner` lease. They still refuse to start while this operator is queued or `inProgress`.
