# Data model: Scheduled snapshot collection

Extends the operator snapshot document in [specs/001-operator-snapshot-scanner/data-model.md](../001-operator-snapshot-scanner/data-model.md). Schema version stays `1`. New snapshot fields are optional on read. Schedule state is not stored inside the snapshot document and is not created on the lean file adapter.

Blob names below are inside the existing container `operator-snapshots`. `{sanitised}` uses the same login grammar as the snapshot blob. The `schedule/` prefix cannot collide with `{sanitised}.json`.

## Sign-in record

Path: `schedule/sign-ins/{sanitised}.json`

Written only when a hosted sign-in completes. A sign-in that does not complete does not create or update it. A silent refresh during a daily collection may replace the protected refresh token and its expiry only.

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Operator login | `operatorLogin` | string | Sanitised GitHub login. Matches the blob name. |
| Completed at | `completedAt` | string (ISO-8601 UTC) | Start of the 30-day window. Replaced only by a later completed sign-in. |
| Organisation logins | `organisationLogins` | array of strings | Logins saved from that completed sign-in. Replaced as a set only by the next completed sign-in. Empty array is valid. |
| Protected refresh token | `protectedRefreshToken` | string | Data-protection ciphertext. Never a plaintext token, never copied onto the snapshot, never returned on a DTO. |
| Refresh token expires at | `refreshTokenExpiresAt` | string or `null` | UTC expiry when GitHub supplied one. |
| Schedule hold | `scheduleHold` | boolean | `true` after a daily run fails with `signInOrToken`. Cleared only by the next completed sign-in. A rate limit, unexpected error, interruption, or missed day does not set it. |

There is no history of previous sign-ins.

## Occasion queue

Path: `schedule/occasion.json` (single document, replaced for each filled UTC date)

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Occasion date | `occasionDate` | string `yyyy-MM-dd` | The UTC date of 05:00. One fill per date. |
| Filled at | `filledAt` | string (ISO-8601 UTC) | When this document was created. Not rewritten when a later tick sees it. |
| Entries | `entries` | array | Ordered. The head is the next daily collection. The operator-facing API does not return this array. |
| Active login | `activeLogin` | string or `null` | Sanitised login of the pass that holds the lease. `null` when no daily pass is running. |
| Lease until | `leaseUntil` | string or `null` | UTC. Renewed while that pass runs. An expired lease means that pass is interrupted and is not requeued. |

### Queue entry

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Login | `login` | string | Sanitised operator login. At most one entry per login. |
| State | `state` | string | `waiting` or `retryWaiting`. |
| Attempts completed | `attemptsCompleted` | number | `0` before the first pass. Incremented when a rate limit or unexpected error sends the entry to the back. Further tries stop after this value reaches `3` (three further tries after the first pass). |

`waiting` is not an in-progress snapshot run. `retryWaiting` is still the same snapshot run, stored as `inProgress`, until that run succeeds, fails, or a later pass is interrupted.

Removing an entry does not delete the snapshot document.

## Snapshot run (additive)

On `run` in the existing snapshot document:

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Started by | `startedBy` | string or absent | `operator` or `dailyOccasion`. Absent on documents written before this feature; readers treat absent as `operator`. Set when a run starts and left unchanged for that run, including further tries. |

No other snapshot fields change. `repositories` and `lastSuccessfulCollectionAt` still change only on success. Failed and interrupted runs still keep the previous success. Tokens, queue position, and try counts are not stored on the snapshot.

## Data-protection key

Path: `schedule/data-protection-keys.xml`

Application code does not read this blob. The ASP.NET Core data-protection host persists it so every web replica shares one key ring. It is not an operator document.

## Eligibility at a fill instant

Occasion instant for `occasionDate` D is D at 05:00:00 UTC. Grace fill (first tick at or before 05:15:00 UTC) uses the same instant, not the tick time.

Include a sign-in record when:

- `completedAt` is at or after the occasion instant minus 30 days (exactly 30 days is included; older is excluded);
- `completedAt` is at or before the occasion instant (strictly after excludes the login for that occasion; FR-016);
- `scheduleHold` is false;
- the login is not already on the current occasion queue and does not hold `activeLogin`;
- that operator's kept snapshot run is not `inProgress` (hand-started or daily; FR-008);
- admission allows the record: if admission is off, yes; if admission is on, the user login is listed or one saved organisation login is listed. Both lists empty allows nobody.

Do not include a login that has no sign-in record.

## State transitions

```text
sign-in completes --> record upserted, scheduleHold false, window restarts
05:00 UTC (or first tick at or before 05:15) --> occasion document created once
entry waiting --> pass starts --> snapshot inProgress, startedBy dailyOccasion
pass success --> entry removed, snapshot succeeded
pass rateLimit or unexpected, attemptsCompleted < 3
    --> entry moved to back as retryWaiting, snapshot stays inProgress
pass rateLimit or unexpected, attemptsCompleted reaches 3
    --> entry removed, snapshot failed with that one reason
pass signInOrToken --> entry removed, scheduleHold true, snapshot failed
pass interrupted (shutdown or expired lease) --> entry removed, snapshot interrupted
product stops while waiting or retryWaiting --> entry remains, snapshot not marked interrupted
next UTC date fill --> new occasion document; previous date is not replayed
```

Hand-started `StartAsync` uses `startedBy = operator`, does not read or write the occasion document except to refuse when this login is queued or a collection is already in progress, and does not receive further tries.

## Isolation

Status reads for the signed-in operator return that operator's snapshot and that operator's schedule statement only. They do not return other logins, the queue, or ciphertext.
