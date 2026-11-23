# Quickstart: Scheduled snapshot collection

Validation guide for implementers. Behaviours are proved by unit and bUnit tests with `TimeProvider`. Do not wait for a real 05:00 UTC and do not boot the AppHost project under `dotnet test`.

## Prerequisites

- .NET 10 SDK.
- Feature 001 operator snapshot collection already in the tree (hand start, one snapshot, file and blob stores).
- Binding ADR for the hosted schedule store accepted before token-persistence code (see [research.md](research.md)).
- Explicit approval before adding `Azure.Extensions.AspNetCore.DataProtection.Blobs`.

Lean checks use PAT-only configuration (`HostedSignInEnabled` false) and the file store. Hosted checks use an in-memory or fake blob store of the kind `OperatorSnapshotBlobStoreTests` already uses. They do not require Azurite for the unit gate.

## Commands

```bash
dotnet test SoloDevBoard.UnitTests.slnf -c Release
```

Narrow runs during work:

```bash
dotnet test tests/SoloDevBoard.Application.Tests -c Release --filter OperatorSnapshot
dotnet test tests/SoloDevBoard.Infrastructure.Tests -c Release --filter OperatorSnapshot
dotnet test tests/SoloDevBoard.Web.Tests -c Release --filter Repositories
```

Use the repository's xUnit v3 / Microsoft.Testing.Platform filter syntax if `--filter` is not the MTP form in this solution. Test names follow `MethodUnderTest_Scenario_ExpectedOutcome`. NSubstitute for doubles. Built-in `Assert` only.

## Scenarios

Each row is one or more tests. A single happy-path case does not cover the rows below it.

| Scenario | Expected outcome | Where |
| --- | --- | --- |
| Two eligible sign-ins, both signed out, clock at 05:00 UTC | Both queued once; collections run one at a time; each success matches a hand-started snapshot for that login | Application |
| Third person signed in inside 30 days but removed from admission | Not queued | Application |
| Sign-in exactly 30 days before 05:00 UTC | Queued | Application |
| Sign-in older than 30 days, or never completed | Not queued, even if the user login is listed | Application |
| Admission on, both lists empty | Nobody queued | Application |
| Admission off | Only in-window sign-ins without a hold are queued | Application |
| Saved organisation removed from the list; user login not listed | Not queued | Application |
| User login still listed after that organisation is removed | Queued | Application |
| Clock at 05:10 UTC, no occasion document | Queue filled then | Application |
| Clock at 05:16 UTC, no occasion document | Not filled | Application |
| Occasion already filled, tick again before 05:15 UTC | Not filled a second time | Application |
| Two fillers race the conditional create | One document, each login once | Infrastructure |
| Second replica while a lease is valid | Does not start another daily walk | Application |
| Expired lease | That pass becomes interrupted and is not requeued | Application |
| Rate limit with another login already queued | Failed attempt goes to the back; the other login runs first; status stays in progress and does not list tries | Application |
| Fourth failure (first pass plus 3 further tries) | One failed reason; next eligible day can queue again | Application |
| Sign-in or token failure on any try | No requeue; hold set; later 05:00 UTC skips until a completed sign-in | Application |
| Completed sign-in after the hold | Hold cleared; window restarts at that sign-in; no collection starts because of the sign-in | Application |
| Incomplete sign-in | Record unchanged | Application |
| Host stop during a pass | Interrupted, not requeued, previous success kept | Application |
| Host stop during `waiting` or `retryWaiting` | Entry remains; retry wait is not marked interrupted | Application |
| Hand `StartAsync` while queued or in progress | `Queued` or `AlreadyInProgress`; no second walk | Application |
| Hand success after a daily success | Replaces the snapshot; `startedBy` is operator; no further tries | Application |
| Lean posture across 05:00 UTC | Scheduler absent; no occasion document; page explains no schedule and that the snapshot stays on this machine | Web bUnit and composition registration test |
| Hosted page before any run, operator eligible today | Alert names today at 05:00 UTC; no control for the time or the 30 days | Web bUnit |
| Hosted page under schedule hold | Failure reason shown; no "tonight" or next 05:00 claim | Web bUnit |

## Manual Aspire check (not the unit gate)

After implementation, with hosted sign-in and the existing storage emulator:

1. `aspire start` on `src/SoloDevBoard.AppHost` (orchestration skill; do not add resources first).
2. Complete hosted sign-in for one allowed user.
3. Confirm a sign-in blob exists under `schedule/sign-ins/` and the snapshot blob has no refresh token.
4. Advance or temporarily invoke the filler through the test seam only. Do not change production code to run at the current minute.
5. Confirm the lean AppHost graph still has no storage resource (`LeanAppHostGraph`).

## Docs

Update `website/content/docs/repositories.md` in the same change as the bUnit assertions that lock the new sentences. Leave Playwright collection journeys unadded.
