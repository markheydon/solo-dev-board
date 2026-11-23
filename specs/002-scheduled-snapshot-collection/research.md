# Research: Scheduled snapshot collection

## Decision: Run the schedule inside the existing web host

**Decision**: A `BackgroundService` in `SoloDevBoard.Web`, registered only when hosted sign-in is enabled, fills and drains the daily queue. It uses `TimeProvider` and a 15-second tick. The occasion is 05:00:00 UTC. If no queue document exists for that UTC date and the clock is still at or before 05:15:00 UTC, the tick fills the queue. After 05:15:00 UTC a missing document means the day is missed.

**Rationale**: The spec treats "the product is running" as the condition for filling the queue. A separate Function or Container Apps Job could run while `web` is stopped, which would collect on a day the spec calls missed. Withdrawn ADR-0046 and accepted ADR-0048 already forbid Azure Functions, SQL, and a scheduled Container Apps Job for this product. `SoloDevBoard.Functions` is a placeholder and is not in the AppHost graph; this feature must not wire it.

**Alternatives considered**: Azure Functions timer (rejected: withdrawn ADR, and it splits "product running" from the site). A Container Apps Job (rejected: new compute resource and the same semantic split). An operator-editable cron (rejected: the spec forbids a settings page).

## Decision: Do not add an AppHost resource

**Decision**: Lean `LeanAppHostGraph` stays web-only. Hosted `HostedSignInAppHostGraph` keeps the existing `operator-snapshot-storage` account and `operator-snapshots` container, with `WithReference` already on `web`. No new parameter, no `aspire add`, no change to endpoint URLs.

**Rationale**: Constitution Principle VII forbids auxiliary infrastructure on the lean path. Principle VI requires a binding ADR before a new AppHost resource. This feature does not need one: the hosted container already exists, and the lean posture has no schedule to store.

**Alternatives considered**: Azurite on the lean graph (rejected: Principle VII). A second storage account for schedule state (rejected: proportionality; one container can hold a `schedule/` prefix that cannot collide with `{login}.json`).

## Decision: Persist sign-in facts and protected refresh tokens in the hosted blob

**Decision**: On a completed hosted OAuth callback only, Web asks Application to upsert a sign-in record: operator login, completion time (UTC), organisation logins returned by that sign-in, and the refresh token. Infrastructure stores the record at `schedule/sign-ins/{sanitised}.json` in `operator-snapshots`. The refresh token is protected with ASP.NET Core Data Protection. The key ring is persisted to `schedule/data-protection-keys.xml` in the same container via `Azure.Extensions.AspNetCore.DataProtection.Blobs`, so every replica and every restart can unprotect it. A daily run that refreshes GitHub tokens writes the new refresh token back and does not change completion time or organisation logins. Lean posture does not write this record and does not register the protector against blob storage.

**Rationale**: Today's hand-started walk copies the access token into memory for that run only. The refresh token lives in the authentication cookie. A signed-out operator at 05:00 UTC has neither. The snapshot document must not gain a secret. Shared data-protection keys are required because a per-replica key ring cannot be read by another copy or after a restart, and the spec requires both.

**Alternatives considered**: Plaintext tokens in the blob (rejected: container access is not an application secret boundary). Cookie replay (rejected: no cookie when signed out, and cookies are not a queue). Calling GitHub only to re-check organisation membership (rejected: the spec forbids it). Treating a silent token refresh as a new completed sign-in (rejected: only a completed sign-in moves the 30 days and the saved organisations).

**Package**: `Azure.Extensions.AspNetCore.DataProtection.Blobs` is not in the repo today. Implementation needs an explicit approval to add it to `Directory.Packages.props`. Data Protection itself is part of the shared ASP.NET Core framework.

**ADR gate**: Before any token is written, accept a binding ADR that records this store, the key blob, the decision not to add an AppHost resource, and the rejection of ADR-0046. Expected file: `adr/adr-0049-hosted-snapshot-schedule-store.md` (confirm the next free number at authoring time).

## Decision: One queue document, conditional writes, one runner

**Decision**: `schedule/occasion.json` is the only queue. Filling it is a conditional create for the UTC date. A lost etag race means another replica already filled it; the loser reads that document and does not insert the same login again. Entries are `waiting` (not yet started) or `retryWaiting` (further try at the back). A short blob lease, renewed while a pass runs, marks the single active login. Other replicas see the lease and do not start a second daily collection. Hand-started walks stay on the existing in-process coordinator and do not take this lease, except that `StartAsync` refuses when this operator is `waiting` or `retryWaiting`, or already has a live walk.

**Rationale**: The spec requires one queue fill per occasion and one daily collection across copies. The snapshot store already uses etags. The operator must not see the queue.

**Alternatives considered**: In-memory queue only (rejected: a restart must keep queued work, including a further try). A queue per replica (rejected: double collection). Holding the lease across the whole day (rejected: a dead replica would block everyone).

## Decision: Eligibility from sign-in records

**Decision**: The filler lists sign-in records. A person is queued when all of the following hold at the occasion instant: completion time is no older than 30 days (exactly 30 days is inside); `scheduleHold` is false; hosted admission allows them. Admission on: user login is on the user list, or a saved organisation login is on the organisation list as it stands at that instant. Empty lists allow nobody. Admission off: lists do not refuse. People with no sign-in record are never queued. A person already `waiting`, `retryWaiting`, or holding the run lease is not added again.

**Rationale**: The spec forbids scanning the allow list and forbids a GitHub membership call. Saved organisations change only at the next completed sign-in.

**Alternatives considered**: Enumerating `AllowedUserLogins` and skipping strangers (rejected: a long list with no sign-ins would still be the scan). Re-reading organisation claims from a live principal (rejected: the operator is signed out).

## Decision: Retries stay one run and do not look like a dead walk

**Decision**: A GitHub rate limit or unexpected error on a daily pass requeues that login at the back, up to 3 further tries, and leaves the snapshot run `inProgress`. The page therefore shows one in-progress daily run and does not list tries. A sign-in or token failure sets `scheduleHold`, does not requeue, and stores `failed` / `signInOrToken`. Host shutdown or an expired run lease during a pass stores `interrupted` / `siteStopped` and removes that pass from the queue. Shutdown or lease expiry while the entry is only `waiting` or `retryWaiting` leaves the entry in place and does not mark the run interrupted. The existing rule "stored `inProgress` and no live task means interrupted" must not apply while the occasion entry for that login is `retryWaiting`.

**Rationale**: Feature 001 treats a stored in-progress run with no live task as process death. Feature 002 deliberately shows the backoff wait as in progress. Without this carve-out, the next status read would interrupt a retry that must still run.

**Alternatives considered**: A separate visible "retrying" status (rejected: the spec says the tries are not listed and the run is not shown as failed). Applying the 3 further tries to hand-started collections (rejected: the spec excludes them). Sleeping at the head of the queue (rejected: other accounts must run first).

## Decision: Occasion text is an Application DTO, not Razor logic

**Decision**: `GetStatusAsync` adds who started the current run, whether this operator is waiting before the first pass, and a schedule statement the page renders. The statement names the next 05:00 UTC only when that occasion will queue them. Otherwise it states why no scheduled collection will run (lean posture, sign-in window, admission, or schedule hold). While they are queued before the first pass, the statement stays "today at 05:00 UTC" and the run is not in progress.

**Rationale**: Constitution Principle I keeps this business rule out of the component. The page already polls status while a run is in progress, which covers the backoff wait once the run is `inProgress`.

**Alternatives considered**: Computing the next occasion in the Razor code-behind (rejected: duplicates the filler rules). A standing sentence that a collection runs every day (rejected: the spec forbids it when this operator will not have one).

## Decision: Tests lock behaviour without exercising the AppHost graph

**Decision**: Pure rules (window boundary, grace, missed day, single fill, one runner, retry order, hold, admission) are Application unit tests with a fake clock and a fake queue store. Blob conditional create and lease renewal are Infrastructure tests against the existing blob test double or Azurite-free fake used by `OperatorSnapshotBlobStoreTests`. bUnit covers copy and the disabled start control. `dotnet test SoloDevBoard.UnitTests.slnf` is the gate. Do not add a unit test that boots `SoloDevBoard.AppHost`.

**Rationale**: Constitution Principle III. The Aspire graph is unchanged, so there is nothing new to prove in the AppHost project.

**Alternatives considered**: A Playwright journey that waits until 05:00 UTC (rejected: not a shell, auth, theme, viewport, or screenshot proof, and it is the wrong tier for this rule).
