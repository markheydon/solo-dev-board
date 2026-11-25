# Contract: account scan status

Normative JSON for one hosted account's current scan state. Field rules and reported `stalled` are in [data-model.md](../data-model.md). Operators do not see this document. The Repositories page sees the facts in [repositories-schedule-copy.md](repositories-schedule-copy.md).

Container: `account-scan-status` on `operator-snapshot-storage`. Blob name: `{sanitised-login}.json`. Media type: `application/json`. Property names are camel case.

Lean posture does not write this blob. Sign-out does not delete it. The blob must not contain a refresh token, an access token, or a repository list.

## Example

A scan the worker has taken, with a fresh mark:

```json
{
  "schemaVersion": 1,
  "operatorLogin": "octocat",
  "status": "running",
  "lastScanAt": "2026-11-24T05:10:00Z",
  "lastResult": "success",
  "resultComment": "Success",
  "suspensionReason": null,
  "livenessAt": "2026-11-25T05:04:00Z",
  "runningSince": "2026-11-25T05:01:00Z",
  "scanGeneration": 4,
  "queueMessagePending": true,
  "queueVisibleAfter": null
}
```

A new account, written when the hosted sign-in record is first stored:

```json
{
  "schemaVersion": 1,
  "operatorLogin": "octocat",
  "status": "neverRun",
  "lastScanAt": null,
  "lastResult": null,
  "resultComment": "",
  "suspensionReason": null,
  "livenessAt": null,
  "runningSince": null,
  "scanGeneration": 0,
  "queueMessagePending": false,
  "queueVisibleAfter": null
}
```

## Stored status

| JSON `status` | Meaning |
| --- | --- |
| `neverRun` | Default. No scan has run. |
| `scheduled` | A queue entry is waiting, including a further try that is not yet visible. |
| `running` | The worker, or a hand-started hosted scan, has started and has not recorded a terminal status. |
| `finished` | The try sequence ended in a real success or failure. |
| `suspended` | The scan will not run again until the block is resolved. Not allowed sets `suspensionReason` to `notAllowed` and leaves `lastScanAt`, `lastResult`, and `resultComment` unchanged. Unusable stored access sets `lastResult` to `failure`, writes the sign-in or token `resultComment`, and leaves `suspensionReason` null. |

`stalled` is not a legal stored value. A reader reports it when `status` is `running` and the latest of `livenessAt`, or `runningSince` when `livenessAt` is null, is strictly more than 5 minutes before the clock. An age of exactly 5 minutes is reported `running`.

## Writes

Every write uses the blob etag. A stale `scanGeneration` does not overwrite a newer scan's status, liveness mark, or snapshot.

| Event | Effect |
| --- | --- |
| Hosted sign-in completes and this blob is absent | Insert the `neverRun` example shape, including `suspensionReason` null. The call site is the completed hosted sign-in path under `src/SoloDevBoard.Web/Authentication/`. |
| Hosted sign-in completes and this blob exists | No change to this blob. |
| Fill finds a sign-in record and this blob is absent | Insert the same `neverRun` shape before eligibility. Do not reset a blob that already exists. Do not enqueue because of that insert alone. |
| Fill wins | `scheduled`, clear `suspensionReason`, set `queueVisibleAfter` null, increment `scanGeneration`, then set `queueMessagePending` after the message is accepted. |
| Fill or hand start loses the etag | No change. No second message and no second walk. |
| Worker takes an allowed entry | `running`, clear `suspensionReason`, set `livenessAt` and `runningSince` to now. This row does not apply when they are not allowed. |
| Scan still working | Replace `livenessAt` at least once every 60 seconds. Do not increment `scanGeneration`. |
| Success | `finished`, `lastResult` `success`, `resultComment` `Success`, set `lastScanAt`, clear the mark and `queueMessagePending`. |
| Further try queued | `scheduled`, `queueMessagePending` true. Do not change `lastResult`. Set `queueVisibleAfter` from `notBefore`. |
| Tries exhausted | `finished`, `lastResult` `failure`, comment states why, set `lastScanAt`, clear `queueMessagePending`. |
| Not allowed before a scan starts | One write to `suspended`. Set `suspensionReason` to `notAllowed` and `queueMessagePending` false. Do not set `running`. Do not change `lastScanAt`, `lastResult`, or `resultComment`. |
| Stored access cannot be used, token exchange failed before the catalogue walk | `suspended`, `lastResult` `failure`, sign-in or token comment, `suspensionReason` null, set `lastScanAt`, clear `queueMessagePending`. |
| Hand-started hosted scan starts | `running`, clear `suspensionReason`, new generation, new liveness mark. `queueMessagePending` stays false. |
| Redelivery after the owning process died | Complete the message. Leave `status` `running`. Clear `queueMessagePending` only, and only for that same generation. Do not change `status`, `lastResult`, `resultComment`, or the snapshot. Do not clear `retainedRun`. |

A reported `stalled` value is computed on read. Nothing persists it.
