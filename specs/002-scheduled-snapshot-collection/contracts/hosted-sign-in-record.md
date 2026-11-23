# Contract: hosted sign-in record

## When a record is written

The OAuth callback that completes hosted sign-in calls one Application method with the owner login, the completion time (`TimeProvider`), the organisation logins already loaded for that sign-in, and the refresh token plus expiry. That call clears `scheduleHold`.

These do not write the record:

- A sign-in attempt that does not complete.
- Cookie validation that refreshes an access token for an open session.
- A daily collection's token refresh, except the ciphertext and expiry fields.
- PAT-only local trusted mode.

## Fill eligibility

The filler evaluates `data-model.md` Eligibility at the occasion instant for the UTC date being filled. A record with `completedAt` strictly after that instant is excluded for that occasion (a sign-in completed at 05:08 UTC is not queued when grace fill runs at 05:10 UTC the same day). Completing sign-in does not enqueue the same day.

The filler reads each operator's kept snapshot. A login with run status `inProgress` is excluded, including a hand-started collection at fill time.

## Admission at 05:00 UTC

The filler reads `HostedAdmissionControlOptions` as they are configured at the fill instant.

| Admission | Rule |
| --- | --- |
| Off (`Enabled` false) | Do not refuse a sign-in record because of the lists. Do not invent records for people who have not signed in. |
| On, both lists empty | Nobody is eligible. |
| On, user login listed | Eligible if the window and hold rules pass. |
| On, only a saved organisation login is listed | Eligible if the window and hold rules pass. |
| On, user removed and no saved organisation remains listed | Not eligible. |
| On, organisation removed and the user login is still listed | Still eligible. |

Organisation logins on the record are not refreshed from GitHub at fill time.

## Secrets

Application logs, snapshot JSON, status DTOs, and exception `detail` lines must not contain the refresh token, the access token, or the data-protection payload. Infrastructure protects the refresh token before the blob write and unprotects it only in the drain step that exchanges it.

The key ring blob is created by the data-protection registration in the web host when the blob connection is present. Lean startup must not require that connection.
