# Quickstart: Scheduled snapshot collection

Validation for this feature. Shapes live in [data-model.md](data-model.md) and [contracts/](contracts/). AppHost method names live in [research.md](research.md). This is not the implementation.

## Prerequisites

- .NET 10 SDK and Aspire CLI 17.0.0, as in `docs/getting-started.md`.
- ADR-0049 Accepted before any AppHost edit.
- Package add approved for the Aspire 17.0.0 packages named in research. Planning does not add them.
- Lean checks use PAT-only local trusted mode (no hosted sign-in).
- Hosted checks use hosted sign-in. That graph already starts Azurite for snapshot blobs. This feature also starts the Functions worker there. Lean `aspire start` must not.

## 1. Lean posture does not schedule

1. Start the lean AppHost (`aspire start` with hosted sign-in off).
2. Confirm the resource list has `web` and does not have the Functions project, `operator-snapshot-storage`, or a queue.
3. Open `/repositories`. Confirm the status alert says that nothing collects on a schedule, and that the snapshot is kept on this machine rather than promised on another machine.
4. Leave the page open across a test clock at 05:00 UTC without choosing **Collect snapshot**. Confirm no run starts.
5. Choose **Collect snapshot** and confirm a hand-started collection can still finish, and the run is not described as the daily occasion.

Automated stand-in: Application tests with a lean posture flag and `TimeProvider` set to 05:00 UTC assert no fill and no queue message. bUnit asserts the two sentences and that **Collect snapshot** still calls start.

## 2. Web stays scaled to zero

On the hosted publish model, confirm the web Container App still has minimum replicas 0 and maximum replicas 1, including when a custom domain is configured. Confirm the Functions resource is a separate project on the hosted graph only, with no HTTP reference from `web` and no `WaitFor` between them. Confirm lean publish does not include the Functions project or the storage account.

There is no unit test of the AppHost graph. Review `HostedSignInAppHostGraph` and `LeanAppHostGraph` against [research.md](research.md).

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
| Message received after the person is no longer allowed | present or absent | Message completed. No failed run. Previous success unchanged. Page states that no scheduled collection will run because they are not allowed. |
| Message received and stored access cannot be used | present or absent | Failed run, sign-in or token reason, no further try. |

Include two eligible people and assert one pass at a time. Include a login allowed but never signed in, a login outside 30 days at the fill instant, a login no longer allowed, and a login with `credentialUnusable`. None of those are queued. A sign-in exactly 30 days before the fill instant is queued. A sign-in at 06:00 UTC is queued by a fill at 06:30 UTC. Organisation allowance uses the saved logins, with no GitHub membership call. Completing sign-in does not itself enqueue a scan.

## 4. Crash, retry, and the start control

Store and Application tests:

- Worker stops while a pass is executing: next read is `interrupted` with `scheduledCollectionStopped`, the interrupted pass does not rejoin, and one `unmetFill` is hidden for 15 minutes.
- Worker stops while the login is only queued, or waiting at the back: not interrupted, and the message still runs.
- Web process stops during a daily pass that is still renewing its lease: the snapshot is not rewritten to `siteStopped`.
- Rate limit then success on the next try: one run, one snapshot. The retry message is not visible until at least 2 minutes after the failure when GitHub sent no retry delay, or until the delay GitHub already indicated. Another queued login that is already visible runs first, and the minimum wait still applies.
- Unexpected error with only one account queued: the three further tries wait at least 3, 6, and 12 minutes from each failed attempt. The pass does not start on the next loop with no wait.
- Four failed attempts (`rateLimit` or `unexpected`): one failed run, then the same fill after 15 minutes. That fill includes them when they are eligible and idle, and it also includes someone whose scan already succeeded. The following 05:00 UTC can still ask if that later fill does not succeed.
- Queued entry, or a further try waiting, when the person is no longer allowed: message removed, no failed collection, previous success unchanged, in-progress copy gone. A sign-in older than 30 days does not remove that message. A pass that is already executing is not withdrawn this way.
- Queued entry, or a further try waiting, when stored access cannot be used: failed run with the sign-in or token reason, further tries stop, previous success's snapshot entries unchanged. Do not restore `retainedRun` over that failure.
- `signInOrToken`: no retry message, later occasions skip until a completed sign-in clears the flag.
- `StartAsync` while queued or in progress does not start a second walk. After the terminal state, a hand-started success replaces the daily snapshot and does not get three further tries.

## 5. Page

bUnit, hosted posture:

- Before 05:00 UTC, eligible, no run yet: the alert names 05:00 UTC and does not pretend a snapshot exists.
- After a credential failure, and they are allowed: the failure is shown and the alert does not promise tonight.
- Not allowed, including when a stored-access wait is also set or the window is closed: the one schedule sentence is the not-allowed sentence. When the wait is set, the failed run and the sign-in or token reason are still shown.
- Allowed, stored-access wait set, window also closed: one sentence, until they sign in again, plus the failed run. No second sentence for the window.
- Queued before the pass starts: not "Saving snapshot…", and the named occasion stays today at 05:00 UTC.
- Retry waiting: in progress, no try list, and no sentence that names the 3, 6, or 12 minute wait.
- After today's 05:00 UTC ask has passed, not queued, eligible tomorrow: the alert names 05:00 UTC on the following UTC day, whether or not today's scan succeeded.
- After a queued entry is removed because they are not allowed: the alert gives that reason and does not show that run as in progress, failed, or interrupted.
- No button changes the time, the frequency, or the 30 days. No retry button.

Update `website/content/docs/repositories.md` in the same change as the page. Do not add a Playwright test that waits for the clock or calls GitHub.

## 6. Commands before a pull request

```bash
dotnet format SoloDevBoard.slnx --verify-no-changes
dotnet build SoloDevBoard.slnx -c Release -warnaserror
dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
```

Hosted `aspire start` is the manual check that the Functions worker and Azurite come up with `web`, and that stopping `web` leaves the worker running. Do not point that check at production GitHub.
