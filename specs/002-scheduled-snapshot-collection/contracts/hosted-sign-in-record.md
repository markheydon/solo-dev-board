# Contract: hosted sign-in record

Normative JSON for one operator's durable hosted sign-in. Field rules are in [data-model.md](../data-model.md). The auth cookie remains the interactive session. This blob is the copy the scheduled collection reads when the operator is signed out and the web app is not running.

Container: `hosted-sign-ins`. Blob name: `{sanitised-login}.json`. Media type: `application/json`. Property names are camel case.

Lean posture does not write this blob. Sign-out does not delete it.

## Example

```json
{
  "schemaVersion": 1,
  "operatorLogin": "octocat",
  "completedAt": "2026-11-23T18:04:00Z",
  "organisationLogins": ["contoso"],
  "refreshToken": "stored-secret",
  "refreshTokenExpiresAt": "2026-12-23T18:04:00Z",
  "credentialUnusable": false
}
```

`refreshToken` in real blobs is the GitHub refresh token. Tests and examples use a placeholder. Logs, snapshot documents, and queue messages must not contain it.

## Writes

| Event | Writer | Effect |
| --- | --- | --- |
| Hosted sign-in completes | Web | Insert or replace `completedAt`, `organisationLogins`, refresh token fields. Set `credentialUnusable` false. |
| Sign-in does not complete | None | No write. Window and wait flag unchanged. |
| Sign-out | None | Cookie cleared. Blob kept. |
| Daily pass fails `signInOrToken` | Functions worker | Set `credentialUnusable` true. Do not change `completedAt`. |
| Refresh during a daily pass returns a new refresh token | Functions worker | Replace refresh token fields under the blob etag. |
| Hand-started collection fails `signInOrToken` | Web, existing behaviour | Does not by itself set `credentialUnusable`. The wait in this feature follows a daily failure only. |

Conditional replace uses the blob etag. A stale writer does not publish over a newer sign-in.

## Reads

The worker lists this container at occasion fill. It does not call GitHub to discover who signed in or which organisations they belong to. Admission uses `organisationLogins` on the blob and the allow lists in the worker's configuration at fill time.
