# Quickstart: Scheduled snapshot collection

Validation for this feature. Shapes live in [data-model.md](data-model.md) and [contracts/](contracts/). AppHost method names live in [research.md](research.md). This is not the implementation.

## Prerequisites

- .NET 10 SDK and Aspire CLI 17.0.0, as in `docs/getting-started.md`.
- ADR-0049 amended in the same change as the `account-scan-status` container (and the timer expression `0 0 5 * * *`) before that graph edit is treated as done. The ADR is already Accepted for the worker.
- Package add approved for the Aspire 17.0.0 packages named in research. Planning does not add them.
- Lean checks use PAT-only local trusted mode (no hosted sign-in).
- Hosted checks use hosted sign-in. That graph already starts Azurite for snapshot blobs. This feature also starts the Functions worker there. Lean `aspire start` must not.

## 1. Lean posture does not schedule

1. Start the lean AppHost (`aspire start` with hosted sign-in off).
2. Confirm the resource list has `web` and does not have the Functions project, `operator-snapshot-storage`, `account-scan-status`, or a queue.
3. Open `/repositories`. Confirm the status alert says that nothing collects on a schedule, and that the snapshot is kept on this machine rather than promised on another machine. Confirm it does not show a worker status (`scheduled`, `running`, `stalled`, or `suspended`).
4. Leave the page open across a test clock at 05:00 UTC without choosing **Collect snapshot**. Confirm no run starts.
5. Choose **Collect snapshot** and confirm a hand-started collection can still finish. Confirm the alert does not name 05:00 UTC, does not say who started the run, and does not show **Refresh overnight access**.

Automated stand-in: Application tests with a lean posture flag and `TimeProvider` set to 05:00 UTC assert no fill and no queue message. bUnit asserts the two sentences and that **Collect snapshot** still calls start.

## 2. Web stays scaled to zero

On the hosted publish model, confirm the web Container App still has minimum replicas 0 and maximum replicas 1, including when a custom domain is configured. Confirm the Functions resource is a separate project on the hosted graph only, with no HTTP reference from `web` and no `WaitFor` between them. Confirm both hosted graph methods add `account-scan-status` with `AddBlobContainer` and pass it to `web` and the Functions project with `WithReference`. Confirm `web` still does not reference the queue or `collection-schedule`. Confirm lean publish does not include the Functions project, the storage account, or `account-scan-status`.

There is no unit test of the AppHost graph. Review `HostedSignInAppHostGraph` and `LeanAppHostGraph` against [research.md](research.md). Do not edit `.aspire/modules/`.

## 3. Asks and fill selection

Application tests, fake clock, no Azure. The timer under test is `0 0 5 * * *`, not `0 0-15 5 * * *`.

| Clock (UTC) | Marker | Expected |
| --- | --- | --- |
| 04:59, including worker start | absent | No fill. |
| 05:00 | absent | Marker created. Each eligible person who is not queued or in progress is queued once, including someone whose scan already succeeded. |
| 05:10, worker starts, marker absent | absent | Catch-up fill. The day is not missed. |
| 05:10, worker starts, marker present, person still queued | present | No second entry. No extra fill from startup. Repair may send a missing `initial`. |
| After 05:00, a fill is invoked again, previous scan succeeded, person idle and eligible | present | They are queued again. No cooldown. |
| Message still queued, person still allowed, sign-in now older than 30 days | present | The message still runs, including on a later UTC day. The clock and the 30-day window are not reasons to drop it. |
| Worker stays up, technical failure has exhausted further tries | present | One `unmetFill`, hidden for 15 minutes, then the same fill. Not a fourth try of the same run. A person who already succeeded is included when they are idle and eligible. |
| Message received after the person is no longer allowed | present or absent | Message completed. Status `suspended`. Last result unchanged. No failed snapshot. Page states that no scheduled collection will run because they are not allowed. |
| Message received and the worker token is missing or cannot be used, no newer worker token stored | present or absent | Status `suspended`, result failure, sign-in or token comment, no further try. The cookie token is not exchanged. |
| Same failure, and a newer worker token is already stored | present or absent | Status `finished`, result failure, sign-in or token comment. The wait is not set. |
| Status `scheduled` and `queueMessagePending` false | present | Restored before eligibility: empty last result becomes `never run`; any other last result becomes `finished`. No failure comment is added. The same fill may then queue them. |
| Reported `stalled`, other rules allow | present | Queued. The winning write sets `scheduled`. |

Include two eligible people and assert one pass at a time. Include a login allowed but never signed in, a login outside 30 days at the fill instant, a login no longer allowed, and a login with `credentialUnusable`. None of those are queued. A sign-in exactly 30 days before the fill instant is queued. A sign-in at 06:00 UTC is queued by a fill at 06:30 UTC. Organisation allowance uses the saved logins, with no GitHub membership call. Completing sign-in does not itself enqueue a scan.

## 4. Liveness, crash, retry, and the start control

Application tests with `TimeProvider`. Keep the exactly-5-minutes case and the more-than-5-minutes case as separate tests. Store tests cover a lost etag and the absence of a token on the status blob.

- A mark 5 minutes old is told `running`. A mark strictly older than 5 minutes is told `stalled`, and the stored status is still `running`.
- A hosted scan that is still working across more than 60 seconds of fake time writes another mark before that gap. While the cadence holds, callers are told `running`.
- The same duty applies to a hand-started hosted scan.
- Worker stops while status is `running`: the redelivered message is completed, the stored status stays `running`, no `finished` failure is written, no `unmetFill` is enqueued, and the pass does not rejoin. After more than 5 minutes the page is told `stalled`.
- The same pass may still commit a real success or failure until a newer scan sets `scheduled` or a new `running`. After that newer generation, the earlier pass does not change the status, the mark, or the snapshot.
- A fill and a hand start that meet a `stalled` account together: one etag wins and one scan starts. The loser does not enqueue and does not change the snapshot.
- Worker stops while status is only `scheduled`: not `stalled`, and the message still runs.
- Web process stops during a daily pass that is still writing liveness: the snapshot is not rewritten to `siteStopped`, and the status blob stays `running`.
- Rate limit then success on the next try: one run, one snapshot. While the further try waits, status is `scheduled`, not `running` or `finished`. The retry message is not visible until at least 2 minutes after the failure when GitHub sent no retry delay, or until the delay GitHub already indicated. Another queued login that is already visible runs first, and the minimum wait still applies.
- Unexpected error with only one account queued: the three further tries wait at least 3, 6, and 12 minutes from each failed attempt. The pass does not start on the next loop with no wait.
- Four failed attempts (`rateLimit` or `unexpected`): status `finished` with that failure, then the same fill after 15 minutes. That fill includes them when they are eligible and idle, and it also includes someone whose scan already succeeded. The following 05:00 UTC can still ask if that later fill does not succeed.
- Queued entry, or a further try waiting, when the person is no longer allowed: message removed, status `suspended`, last result unchanged, previous success unchanged. A sign-in older than 30 days does not remove that message. A pass that is already `running` is not withdrawn this way.
- Queued entry, or a further try waiting, when the worker token cannot be used and no newer worker token is stored: status `suspended`, result failure, sign-in or token comment, further tries stop, previous success's snapshot entries unchanged.
- The same failure when a newer worker token is already stored: status `finished`, the wait is not set, and further tries stop.
- `signInOrToken` with no newer worker token: no retry message, later occasions skip until a kept worker token clears the flag. A completed sign-in that does not store a worker token does not clear it and does not reset a status blob that has already left `neverRun`.
- `StartAsync` while `scheduled` or told `running` does not start a second walk. `StartAsync` while told `stalled` sets `running` and writes a mark. `StartAsync` while the worker token is missing still starts and uses the session access token. A hand-started success does not get three further tries.

## 5. Page

bUnit, hosted posture. Assert the sentences in [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md). None of them contain "05:00 UTC", "daily occasion", "today", "tonight", or "the following day", and none say who started the run.

- Eligible, no run yet, including before 05:00 UTC and after that ask when the following ask would include them: "The next collection runs overnight." The alert does not pretend a snapshot exists.
- Worker token missing, wait not set, and the next ask will queue them: the overnight sentence, plus **Refresh overnight access**.
- After a credential failure, and they are allowed: the suspended token sentence, and the schedule sentence says no scheduled collection will run until you refresh the access used for the overnight collection. **Refresh overnight access** is shown when they are signed in.
- Not allowed, including when a stored-access wait is also set or the window is closed: the one schedule sentence is the not-allowed sentence. When the wait is set, the failed run and the sign-in or token reason are still shown.
- Allowed, stored-access wait set, window also closed: one schedule sentence, the refresh sentence, plus the failed run. No second sentence for the window.
- A new hosted account before any queue entry: "No collection has run yet."
- Queued before the worker takes the entry: "A collection is waiting to start." The schedule sentence stays the overnight sentence. The running sentence is not shown.
- Worker has taken the entry and the mark is fresh: the running sentence, including the start time and the recorded count, with no starter.
- Retry waiting: the scheduled sentence. No try list, and no sentence that names the 3, 6, or 12 minute wait.
- After a queued entry is removed because they are not allowed: the not-allowed sentence, and the previous last result unchanged. Not the running sentence and not a failed snapshot.
- Reported `stalled`: "This collection has stopped reporting progress." Not the running sentence, not a finished failure, and not "the site stopped". **Collect snapshot** can start.
- No button changes the time, the frequency, or the 30 days. No retry button. **Refresh overnight access** is not that retry.

Update `website/content/docs/repositories.md` in the same change as the page. Do not add a Playwright test that waits for the clock or calls GitHub.

## 6. Worker token

Application tests with a fake token exchange. No live GitHub call.

- Schema version 1 with `refreshToken` set: the worker token is absent, and the exchange is not called with that value. The overnight pass fails `signInOrToken` and sets the wait.
- First authorisation stores `completedAt` and the organisation list and does not clear `credentialUnusable`. The second authorisation, when the session access token is accepted, stores `workerRefreshToken` and clears the wait. It does not change `completedAt`.
- Second authorisation when the session access token is rejected: the candidate is discarded, and the browser-restore write does not store a worker token and does not change `completedAt`.
- Worker-token refresh stores the worker token, does not change `completedAt` or the organisation list, and does not enqueue a scan. If status was `suspended` for the token failure, it becomes `finished` with that failure kept.
- A queued scan that has not started uses the worker token stored when it is taken.
- A running scan keeps the token it started with. A rotated replacement is stored only while that token is still the stored value. A newer stored token is left in place.
- Hand-started `StartAsync` with no worker token uses the session access token and can finish.

## 7. Commands before a pull request

```bash
dotnet format SoloDevBoard.slnx --verify-no-changes
dotnet build SoloDevBoard.slnx -c Release -warnaserror
dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
```

Hosted `aspire start` is the manual check that the Functions worker and Azurite come up with `web`, and that stopping `web` leaves the worker running. Do not point that check at production GitHub.
