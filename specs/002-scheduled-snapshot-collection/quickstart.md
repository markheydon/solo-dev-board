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

## 3. Occasion window

Application tests, fake clock, no Azure:

| Clock (UTC) | Marker | Expected |
| --- | --- | --- |
| 04:59 | absent | No fill. |
| 05:00 | absent | Marker created. Each eligible login queued once. |
| 05:10 | absent | Fill at 05:10. |
| 05:10 | already present | No second fill. |
| 05:16 | absent | No fill. The day is missed. |
| After 05:15, marker present, messages still queued | present | No new marker. Existing messages still run. |

Include two eligible people and assert one pass at a time. Include a login allowed but never signed in, a login outside 30 days, a login no longer allowed, and a login with `credentialUnusable`. None of those are queued. A sign-in exactly 30 days before the occasion is queued. Organisation allowance uses the saved logins, with no GitHub membership call.

## 4. Crash, retry, and the start control

Store and Application tests:

- Worker stops while a pass is executing: next read is `interrupted` with `scheduledCollectionStopped`, and no new queue message.
- Worker stops while the login is only queued, or waiting at the back: not interrupted, and the message still runs.
- Web process stops during a daily pass that is still renewing its lease: the snapshot is not rewritten to `siteStopped`.
- Rate limit then success on the next try: one run, one snapshot, the other queued login runs before the retry.
- Four failed attempts (`rateLimit` or `unexpected`): one failed run, next eligible day can start.
- `signInOrToken`: no retry message, later occasions skip until a completed sign-in clears the flag.
- `StartAsync` while queued or in progress does not start a second walk. After the terminal state, a hand-started success replaces the daily snapshot and does not get three further tries.

## 5. Page

bUnit, hosted posture:

- Before any run, eligible: the alert names 05:00 UTC and does not pretend a snapshot exists.
- After a credential failure: the failure is shown and the alert does not promise tonight.
- Queued before the pass starts: not "Saving snapshot…".
- Retry waiting: in progress, no try list.
- No button changes the time, the frequency, or the 30 days. No retry button.

Update `website/content/docs/repositories.md` in the same change as the page. Do not add a Playwright test that waits for the clock or calls GitHub.

## 6. Commands before a pull request

```bash
dotnet format SoloDevBoard.slnx --verify-no-changes
dotnet build SoloDevBoard.slnx -c Release -warnaserror
dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
```

Hosted `aspire start` is the manual check that the Functions worker and Azurite come up with `web`, and that stopping `web` leaves the worker running. Do not point that check at production GitHub.
