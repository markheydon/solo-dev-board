# Contract: hosted sign-in record

Normative JSON for one operator's durable hosted sign-in. Field rules are in [data-model.md](../data-model.md). The auth cookie remains the interactive session and holds the browser refresh token. This blob is the copy the scheduled collection reads when the operator is signed out and the web app is not running. It holds the worker refresh token only. It does not hold the cookie token.

Container: `hosted-sign-ins`. Blob name: `{sanitised-login}.json`. Media type: `application/json`. Property names are camel case. Schema version on write is `2`.

Lean posture does not write this blob. Sign-out does not delete it.

## Example

```json
{
  "schemaVersion": 2,
  "operatorLogin": "octocat",
  "completedAt": "2026-11-23T18:04:00Z",
  "organisationLogins": ["contoso"],
  "workerRefreshToken": "stored-worker-secret",
  "workerRefreshTokenExpiresAt": "2026-12-23T18:04:00Z",
  "credentialUnusable": false
}
```

`workerRefreshToken` in real blobs is the GitHub refresh token from a second authorisation on the existing GitHub App. Tests and examples use a placeholder. Logs, snapshot documents, account scan status, and queue messages must not contain it. A blob with `schemaVersion` 1 may still contain `refreshToken`. Readers ignore that field. They do not exchange it and they do not copy it to `workerRefreshToken`.

## Writes

| Event | Writer | Effect |
| --- | --- | --- |
| First GitHub authorisation of a hosted sign-in succeeds | Web | Cookie stores the browser refresh token. Insert or replace `completedAt` and `organisationLogins`. Do not write a worker token. Do not clear `credentialUnusable`. If the account scan status blob is absent, create it as `neverRun`. Do not reset a status blob that already exists. |
| Second authorisation is kept | Web | Set `workerRefreshToken` and `workerRefreshTokenExpiresAt`. Clear `credentialUnusable`. Do not change the cookie, `completedAt`, or `organisationLogins` when this is the second leg of a sign-in that already wrote them. If status is `suspended` for the token failure, set it to `finished` and keep that failure. |
| Second authorisation leaves the browser access token rejected | Web | Discard the candidate worker token. Do not replace a worker token already stored. Replace the cookie on the following browser-restore authorisation only. Do not change `completedAt` or `organisationLogins` on that restore, and do not store a worker token there. |
| Operator cancels the second authorisation or the worker-token refresh | None | Cookie and blob unchanged. No scan failure is recorded. |
| Worker-token refresh from `/repositories` is kept | Web | Same worker-token write as a kept second authorisation. Do not change the cookie, `completedAt`, or `organisationLogins`. Do not start a scan. |
| Sign-in does not complete | None | No write. Window and wait flag unchanged. |
| Sign-out | None | Cookie cleared. Blob kept. |
| Overnight pass fails `signInOrToken` and no newer worker token is stored | Functions worker | Set `credentialUnusable` true. Do not change `completedAt`. Do not exchange the cookie token. |
| Overnight pass fails `signInOrToken` and a newer worker token is already stored | Functions worker | Do not set `credentialUnusable`. Do not overwrite the newer worker token. |
| Refresh during an overnight pass returns a new worker refresh token, and the stored token is still the one that scan started with | Functions worker | Replace the worker token fields under the blob etag. |
| Same refresh, and a newer worker token is already stored | Functions worker | Do not write. The newer token stays. |

Conditional replace uses the blob etag. A stale writer does not publish over a newer sign-in or a newer worker token.

## Reads

The worker lists this container at occasion fill. It reads one blob again when that person's queue message is about to run, and it uses `workerRefreshToken` from that read. It does not call GitHub to discover who signed in or which organisations they belong to. Admission uses `organisationLogins` on the blob and the allow lists in the worker's configuration. Fill uses the lists and the 30-day window at that fill instant, including a fill after 05:00 UTC and a fill after a successful scan. Running a queued entry uses the allow lists only. It does not re-check the 30-day window. When the person is not allowed, the entry is removed, the account scan status becomes `suspended` with `suspensionReason` `notAllowed`, without changing the last scan time, result, or result comment, and `credentialUnusable` is not set. When the worker token is missing or cannot be used, the exchange fails before the catalogue walk. If no newer worker token is stored, the account scan status becomes `suspended` with a failure result, the sign-in or token comment, and `suspensionReason` null, and `credentialUnusable` is set. If a newer worker token is already stored, the status becomes `finished` with that failure and the wait is not set. Neither case changes `completedAt`. The scan status itself is [account-scan-status.md](account-scan-status.md), not this blob.
