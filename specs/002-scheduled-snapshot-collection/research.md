# Research: Scheduled snapshot collection

The 2026-11-24 decisions below still govern when this delivery asks, how a fill selects people, and how further tries wait. The 2026-11-25 section **Account scan status** replaces every decision that inferred the operator-facing state from `queuedOccasion`, `passExecuting`, `passLeaseUntil`, or `run.status`, and it replaces the decision that a worker death writes `interrupted` / `scheduledCollectionStopped` and enqueues `unmetFill`. The 2026-11-26 section **Worker credential and human-friendly text** replaces the single shared refresh token in **Durable sign-in record**, and it replaces operator-facing wording that names 05:00 UTC or who started the run.

## Worker credential and human-friendly text (2026-11-26)

**Decision:** Overnight collection and the interactive session use two GitHub App user grants on the existing App. The auth cookie keeps the refresh token from the first authorisation of a hosted sign-in. The sign-in blob keeps only the refresh token from a later authorisation, in `workerRefreshToken` and `workerRefreshTokenExpiresAt`, at schema version 2. The blob does not store the cookie token. The worker exchanges `workerRefreshToken` only. It does not read the cookie and it does not persist a rotated replacement of the cookie token.

GitHub documents that refreshing a user access token returns a new refresh token and invalidates the refresh token that was sent (`Refreshing user access tokens`). A probe that refreshes the cookie token would therefore change it. That probe is rejected. After the second authorisation code is exchanged, the product checks the access token already in the interactive session with the user read the sign-in path already performs.

- If GitHub accepts that access token, the browser grant is still usable. Persist the candidate worker token. Do not write the cookie.
- If GitHub rejects it, discard the candidate. Do not replace a worker token already stored. Send the operator through the same App again to restore the browser token. That restore replaces the cookie only. It does not store a worker token, and it does not change `completedAt` or `organisationLogins`.

A residual limit remains: GitHub does not document a refresh-token check that leaves the token unchanged. An access token can still be accepted after its refresh token is already dead. The product does not paper over that by rotating the cookie during the check. A later interactive refresh that fails is the existing cookie failure, not a reason for the worker to exchange the cookie token.

OAuth state on the existing callback distinguishes four purposes. There is no new callback URL, no new GitHub App, and no new AppHost resource.

| Purpose | When | What is stored |
| --- | --- | --- |
| `browser` | First authorisation of hosted sign-in | Cookie refresh token. Sign-in record: `completedAt` and `organisationLogins`. No worker token. `credentialUnusable` is not cleared. Then redirect to `worker-during-sign-in`. Sign-in is already complete. |
| `worker-during-sign-in` | Second authorisation of that sign-in | Worker token only when the browser access token is still accepted. That write clears `credentialUnusable`. Cancel leaves the operator signed in and leaves the worker token as it was. |
| `browser-restore` | Browser access token was rejected after the second authorisation | Cookie only. |
| `worker-refresh` | Operator chooses the control on `/repositories` while signed in | Worker token only when it is kept. That write clears `credentialUnusable`. If the status is `suspended` for the token failure, set it to `finished` and keep the failure comment. Does not change the cookie, `completedAt`, or `organisationLogins`. Does not start a scan. Cancel or failure does not sign them out and does not record a scan failure. A completed sign-in that stores no worker token leaves `credentialUnusable` set. |

A schema 1 blob's `refreshToken` was the interactive grant. Readers do not copy it to `workerRefreshToken` and do not exchange it. The next overnight scan fails with the sign-in or token reason until a second authorisation stores a worker token. A signed-in operator is asked to refresh that token on `/repositories` as soon as it is missing.

At dequeue, capture the worker token the scan starts with. A queued scan that has not started reads the token again when it is taken. A scan already running keeps the captured token. Persist GitHub's replacement worker refresh token only when the stored value is still that captured token (etag, and the stored secret still matches). If the operator has stored a newer worker token, the replacement is not written. If the captured token then cannot be used and a newer worker token is already stored, the status is `finished` with result failure and the sign-in or token comment, `suspensionReason` stays empty, and `credentialUnusable` is not set. If no newer token is stored, the status is `suspended` with that failure, `suspensionReason` empty, and `credentialUnusable` true. No further try and no `unmetFill` in either case. The worker does not fall back to the cookie token.

A missing worker token is the same failure as a token exchange that fails, when the account is not already in the wait: the scan fails with the sign-in or token reason and the wait is set. Fill still queues that account until the wait is set. Hand-started collection uses `ICurrentUserContext.GetAccessToken()` and does not read the worker token.

When a kept worker token clears the wait and the status is `suspended` because stored access could not be used, set the status to `finished`, keep the failure result and the sign-in or token comment, and leave `suspensionReason` empty. A `notAllowed` suspension is not changed by that write.

Human-friendly text is one Application function over the reported status and the schedule facts. Web renders the returned strings. It does not build its own sentences and it does not format 05:00 UTC. The snapshot `trigger` field stays for the collection record. It is not copied onto the account scan status and it is not an input to the sentences. The catalogue is the FR-025 list in [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md). The refresh control is on `/repositories` only, with accessible name **Refresh overnight access**. Lean posture does not show it.

**Rationale:** FR-024 forbids sharing one refresh token between the cookie and the worker, and it forbids the worker from repairing a failure by exchanging the session token. GitHub's refresh grant rotates the token that was sent, so the "still works, and unchanged" check cannot be that grant. The access token already issued to the session is the check that can leave the cookie bytes alone. FR-013 distinguishes a token failure that still has a newer worker token (`finished`, no wait) from one that does not (`suspended`, wait set). FR-025 requires one sentence catalogue for every caller, so the wording cannot live only in the Razor page. Principle VII is met because both grants are hosted sign-in on the existing GitHub App.

**Alternatives considered:**

- **Keep one refresh token and copy it to the blob and the cookie.** Rejected. Refreshing it for an overnight scan invalidates the browser session. That is the failure FR-024 closes.
- **Split one token response into two stored copies.** Rejected. The spec says one GitHub response is never both tokens.
- **Probe the browser grant by refreshing it, then write the rotated token back to the cookie.** Rejected. The spec requires the cookie token to stay unchanged when the worker token is stored. GitHub's refresh grant cannot check a refresh token without replacing it.
- **A second GitHub App, or an OAuth App, for the worker.** Rejected. ADR-0015 stays GitHub App-only. A second registration would be a new authentication mode, which Principle VII forbids.
- **Store the worker token on `account-scan-status`.** Rejected. That blob is readable as scan state and must not hold a secret. The sign-in blob is already the secret store.
- **Let the page format "05:00 UTC" from `NextOccasionAtUtc`.** Rejected. FR-006 and FR-025 require the overnight sentence and forbid naming that clock time.

## Account scan status (2026-11-25)

**Decision:** Store one JSON blob per hosted account in a new container `account-scan-status` on the existing `operator-snapshot-storage` account. Blob name `{sanitised-login}.json`. Both `web` and the Functions project receive it with `WithReference` and open it with `AddKeyedAzureBlobContainerClient` using `AspireHostedStorageReferenceNames.AccountScanStatus`, the same pattern as `hosted-sign-ins`. Add the container in `HostedSignInAppHostGraph.ConfigureLocalRun` and `ConfigurePublishInfrastructure` only:

```csharp
var accountScanStatus = storage.AddBlobContainer("account-scan-status", "account-scan-status");
```

`web` keeps its existing references and adds `.WithReference(accountScanStatus)`. The Functions project does the same. `web` still does not reference `operator-snapshot-queues` or `collection-schedule`. `LeanAppHostGraph` is not edited. No endpoint URL is passed through `WithEnvironment`. No file under `.aspire/modules/` exists for this C# AppHost and none is added. No new NuGet package: `AddBlobContainer` and the keyed blob client are already used in this solution. Amend ADR-0049 in the same implementation change so the binding record names the container, the `web` reference, and timer `0 0 5 * * *`.

The blob holds `status` (`neverRun`, `scheduled`, `running`, `finished`, `suspended`), `lastScanAt`, `lastResult` (`success`, `failure`, or null), `resultComment`, `suspensionReason` (`notAllowed` or null), `livenessAt`, `runningSince`, `scanGeneration`, `queueMessagePending`, and `queueVisibleAfter`. `stalled` is not a stored value. A reader, given `TimeProvider.GetUtcNow()`, reports `stalled` only when `status` is `running` and the latest of `livenessAt` (or `runningSince` when `livenessAt` is null) is strictly more than 5 minutes ago. An age of exactly 5 minutes is still reported `running`.

Create the blob when a hosted sign-in completes and the blob is absent: `neverRun`, null last scan, null result, empty comment, `suspensionReason` null, generation 0. The completed sign-in path under `src/SoloDevBoard.Web/Authentication/` calls that Application transition. Do not reset an existing blob on a later sign-in. Readers report a missing hosted blob as `never run` and do not write it. The next completed sign-in, or the next fill before eligibility, creates it for an account that already has a sign-in record.

Transitions are conditional writes on the blob etag and they carry `scanGeneration`. One Application operation is the only writer of a new `scheduled` or a new `running`. The first matching etag wins. The loser does not enqueue, does not start a walk, and does not change the snapshot. Liveness updates use the generation captured when that scan entered `running` and do not increment it. After a newer scan has incremented the generation, the earlier pass's status, liveness, and snapshot writes do not apply.

`queueMessagePending` is set true only after the queue accepts the message, including a further try that is not yet visible. `queueVisibleAfter` copies `notBefore` (null on `initial`). A winning fill that enqueues an `initial` sets `queueVisibleAfter` null. Before a fill decides who is eligible, each `scheduled` account with `queueMessagePending` false is restored: null `lastResult` becomes `neverRun`; any other `lastResult` becomes `finished`. Last scan time, result, and comment stay, and no failure comment is added. The same fill may then queue them. A message that exists but is not yet visible stays `scheduled` with `queueMessagePending` true. Host start may re-send a missing `initial` only when `scheduled`, `queueMessagePending` is true, `queueVisibleAfter` is null or already passed, and a peek finds no visible message for that login. It must not re-send when `queueVisibleAfter` is still ahead.

While status is `running` and the scan is still working, the process that owns that generation writes `livenessAt` at least once every 60 seconds until it records `finished` or `suspended`. The same duty applies to a hand-started hosted scan, which sets `running` directly and does not use the queue. More frequent marks are acceptable. The catalogue snapshot is not rewritten on that cadence. Queue visibility for a live pass stays a short renewal (about every 10 seconds, visibility about 30 seconds) so a live pass is not redelivered. That renewal is not the operator-facing liveness mark and it is not `passLeaseUntil`.

On redelivery when this pass was already `running` and the mark is stale or the worker that owned it is gone: complete the message, leave `status` as `running`, do not write `finished`, do not write a failure reason, do not enqueue `unmetFill`, and do not rejoin the pass. Callers are told `stalled` once the mark is more than 5 minutes old. A later fill may set `scheduled` when the other eligibility rules allow. The operator may start a collection by hand, which sets a new `running` and a new mark. Until one of those wins, the earlier pass may still commit its real success or failure. A stop while status is only `scheduled` leaves it `scheduled`.

`unmetFill`, hidden for 15 minutes, remains the sooner ask after a terminal rate-limit or unexpected-error outcome with no further tries left. It is not published for a liveness lapse. Not allowed at dequeue sets `suspended` and `suspensionReason` `notAllowed`, and does not overwrite `lastScanAt`, `lastResult`, `resultComment`, or the successful snapshot. Unusable stored access is a token exchange that failed before the catalogue walk. When no newer worker token is stored, it sets `suspended`, `lastResult` failure, the sign-in or token comment, `suspensionReason` null, and `credentialUnusable`. When a newer worker token is already stored, it sets `finished`, `lastResult` failure, the sign-in or token comment, `suspensionReason` empty, and leaves `credentialUnusable` false. A further try waiting at the back is `scheduled`, not `running` and not `finished`, and the tries are not listed. The last result stays the previous completed result until the try sequence ends.

**Rationale:** FR-022 requires a stored record written from events, created when access is stored, including before a snapshot document exists. The snapshot blob is the wrong home: it may not exist yet, and a liveness write must not contend with the catalogue etag. The sign-in blob is the wrong home: it holds the refresh token. `collection-schedule` is the wrong home: `web` must write status during a hand-started scan, and that container also holds the runner lease, which `web` must not reference. Principle VI is met by one `AddBlobContainer` on the account ADR-0049 already models, plus an amendment of that ADR. FR-023 requires the 60-second mark, the strict 5-minute report, and a single winner. Storage Queue peek cannot see invisible messages, so `queueMessagePending` and `queueVisibleAfter` are the product's record of an entry that exists but is hidden. FR-015's sooner ask stays limited to a finished technical failure. A worker death is the `stalled` report, not a stored failure and not `unmetFill`.

**Alternatives considered:**

- **Fields on the snapshot document, including `passLeaseUntil`.** Rejected. The page would still be inferring, the record would be missing until the first collection, and a 60-second write would fight the catalogue etag. The 2026-11-24 interruption path wrote `scheduledCollectionStopped` from that lease. FR-023 forbids recording `finished` or a short failure reason because the process died.
- **Fields on the hosted sign-in blob.** Rejected. Liveness and token rotation would share one etag, and the status would sit next to the refresh token.
- **A new storage account or a SQL table.** Rejected. ADR-0046 stays withdrawn. One container on `operator-snapshot-storage` is enough.
- **`web` references `collection-schedule` and writes status blobs there.** Rejected. That reference would also expose the runner lease and the occasion marker to the site.
- **Enqueue `unmetFill` when the liveness mark lapses.** Rejected for this revision. The spec allows a later fill to queue a `stalled` account. It does not ask this delivery to treat the lapse as a finished technical failure. Publishing `unmetFill` from the lapse would also race a pass that is still allowed to commit its real result.
- **Store `stalled`.** Rejected. The clarification says it is reported only, so a fresh mark from the same pass can be told `running` again without a status transition.

## Scheduled compute

**Decision:** In the hosted sign-in AppHost graph only, model the spec's scheduled collection process as a .NET isolated Azure Functions project, `src/SoloDevBoard.Functions`, added with `AddAzureFunctionsProject` from `Aspire.Hosting.Azure.Functions` 17.0.0. Publish goes through that integration, which deploys the worker as an Azure Container Apps function app with KEDA scale rules. The worker scales to zero when no timer or queue execution is active. It is not an always-on replica.

The operator-facing web resource stays the existing Container App. `CreateWebApp` already sets `MinReplicas = 0` and `MaxReplicas = 1`. This feature must leave those values in place. The custom-domain `PublishAsAzureContainerApp` callback must not clear them. The web app is not given a `BackgroundService`, a minimum replica of 1, or any other keep-alive so that 05:00 UTC can run inside the site.

**Rationale:** The spec requires a process that is not the operator-facing app. The day's queue must fill and a collection must be able to finish while that app is scaled to zero. A timer inside the Blazor host would force the Container App to stay warm, which removes the scale-to-zero posture already modelled on `web`. Aspire 17.0.0 documents `AddAzureFunctionsProject`, `WithHostStorage`, and `WithReference` for this worker, and lists the timer trigger and the Azure Storage queue trigger as supported. Confirmed with `aspire docs api search --language csharp` on CLI 17.0.0 (`2d325b1`):

- `AddAzureFunctionsProject(IDistributedApplicationBuilder, string name, string projectPath)`
- `WithHostStorage(IResourceBuilder<AzureFunctionsProjectResource>, IResourceBuilder<AzureStorageResource>)`
- `AddQueues(IResourceBuilder<AzureStorageResource>, string name)`

Use the path overload, matching `AddDotnetProject` for `web`. Do not use `AddAzureFunctionsApp` (experimental `ASPIREAZUREFUNCTIONS001`, for a Node worker without a `.csproj`). Do not call `WithExternalHttpEndpoints` on the Functions resource. There is no public HTTP trigger. `web` does not `WithReference` the Functions resource, and neither resource `WaitFor`s the other.

**Alternatives considered:**

- **Timer inside the web Container App.** Rejected. It only runs while `web` is up, and keeping `MinReplicas` at 1 would abandon scale-to-zero.
- **`PublishAsScheduledAzureContainerAppJob`.** Rejected for this feature. A cron job is a good fit for a single finite batch, and Aspire documents it. It is a poor fit for a process that must fill again when it is back later the same UTC day, then keep draining a queue that survives that process stopping. The user asked for Functions. Jobs stay unused.
- **Durable Task Scheduler (`AddDurableTaskScheduler`).** Rejected. The API is experimental (`ASPIREDURABLETASK001`), and Aspire documents that deployment support is not included yet.
- **Reviving ADR-0046 (Functions plus Azure SQL).** Rejected. ADR-0046 stays withdrawn. The snapshot remains the JSON blob from ADR-0048. SQL and EF Core are not added.

## Where the worker sits in the graph

**Decision:** Add the Functions project, its host storage, the queue endpoint, and the extra blob containers only in `HostedSignInAppHostGraph` (local run and publish). `LeanAppHostGraph` stays web compute plus optional dev Key Vault. PAT-only `aspire start` does not start Azurite or the Functions host.

Host storage is the existing `operator-snapshot-storage` account via `WithHostStorage`, not a second account and not the implicit host-storage account Aspire would otherwise create. Local hosted sign-in keeps `RunAsEmulator(azurite => azurite.WithDataVolume())`. Publish omits the emulator.

Because host storage is explicit, publish must assign the roles the implicit account would have received, on this same account, to the Functions identity:

- `StorageBlobDataContributor`
- `StorageQueueDataContributor`
- `StorageTableDataContributor`
- `StorageAccountContributor`

The reader-only sample in the Aspire host-storage section is not enough for the Functions runtime. Local emulator runs do not add those ARM role resources (same reason the local Key Vault path skips `WithRoleAssignments`).

New product resources on that account:

| Resource | AppHost name | Azure name |
| --- | --- | --- |
| Blob container | `hosted-sign-ins` | `hosted-sign-ins` |
| Blob container | `collection-schedule` | `collection-schedule` |
| Blob container | `account-scan-status` | `account-scan-status` |
| Queue endpoint | `operator-snapshot-queues` | the account queue service |

`web` references `operator-snapshots` (already), `hosted-sign-ins`, and `account-scan-status`. It does not reference the queue or `collection-schedule`. The Functions project references all of the above plus host storage. Queue name `daily-snapshot-collection` is created by the worker with the injected `QueueServiceClient`. It is not a separate Aspire resource.

Both hosts get the same hosted auth and admission environment already applied to `web` (`GitHubAuth__*`, `HostedAdmissionControl__*`, Key Vault secret reference for the GitHub App client secret on publish). The Functions host does not receive the PAT.

**Rationale:** Principle VII forbids auxiliary resources on the lean graph. Principle VI requires one AppHost graph and a binding ADR before this material add. ADR-0048 remains the blob decision for the snapshot document and is not withdrawn. Its "do not add Functions" line applies to the snapshot feature, not to this schedule. ADR-0046 is not reinstated. ADR-0049 (next number) is the binding record for the worker, the sign-in blob, the schedule blob, and the queue. Accept it before editing the AppHost.

**Alternatives considered:**

- **Implicit Functions host storage (second account).** Rejected. A second storage account duplicates the account ADR-0048 already added.
- **Omitting the worker from local hosted sign-in.** Rejected. Hosted sign-in is the posture that has the schedule, including local hosted sign-in. Lean remains the path without it.

## When this delivery asks, and what a fill selects

**Decision:** `TryFillDailyOccasion` does not contain a clock window, a once-per-day lock, or a cooldown. Any caller may run it at any UTC time. The 30-day comparison uses that call's instant (exactly 30 days is inside). A person is queued when, at that instant, all of the following hold:

- Latest completed hosted sign-in is inside 30 days, they are currently allowed, and the sign-in record is not `credentialUnusable`.
- They are not already queued and no collection for them is in progress. Stored status `scheduled` counts as queued, including a further try whose message is not yet visible. Stored status `running` counts as in progress only while the reader would say `running`. A reported `stalled` account counts as neither. Before that test, restore orphan `scheduled` rows as the account-scan-status section describes.
- A scan that already succeeded, including earlier the same UTC day, does not exclude them. A finished scan does not start a waiting period. Signing in after 05:00 UTC does not exclude them when this instant is after that sign-in.

A `signInOrToken` failure does not publish `unmetFill`. When no newer worker token is stored, `credentialUnusable` is set and that account is not queued again until a kept worker token clears the flag, including a refresh while the operator stays signed in. A completed sign-in that stores no worker token leaves the flag set. When a newer worker token is already stored, the failure is `finished`, `credentialUnusable` stays false, and the wait is not set. A pass whose worker died is not put back on the queue. A later fill sends a new `initial` message when that account is reported `stalled` and the other rules allow. The filler holds blob lease `runner` only for the fill, then releases it. A copy that loses the lease retries the same fill after the lease is free. That retry still skips anyone already queued or in progress. Completing sign-in does not call the fill.

This delivery asks on three paths only. None of them is an all-day timer, and none of them is a setting:

- One timer trigger, NCRONTAB `0 0 5 * * *` (six fields, UTC). That is 05:00:00 UTC, once. Replace the branch's `0 0-15 5 * * *` schedule. Those extra minutes were the grace window the spec removed. Keeping them would ask sixteen times in a quarter of an hour.
- Worker start, after repair of missing `initial` messages, only when the clock is at or after 05:00:00 UTC that day and today's occasion marker is absent. That is the catch-up when the process was down at 05:00, including after 05:15. A start before 05:00 does not fill. A start after a marker already exists does not fill, so a cold start after a completed ask does not scan everyone again. Repair of a missing `initial` still runs only when status is `scheduled`, `queueMessagePending` is true, `queueVisibleAfter` is null or already passed, and a peek finds no visible message. It does not run while `queueVisibleAfter` is still ahead, and it does not read `queuedOccasion`.
- An `unmetFill` queue message, published after a terminal technical outcome (rate limit or unexpected error with no further tries left) and hidden for 15 minutes. The handler calls `TryFillDailyOccasion` and does not walk GitHub. It may run before 05:00 UTC. The 15 minutes is how soon this delivery asks again after that outcome. It is not an eligibility rule, and it does not skip someone whose scan succeeded when the fill runs.

Do not set a local `WEBSITE_TIME_ZONE` or `TZ`. The Azure Functions default is UTC. Do not enable the Functions host's "run a missed timer whenever the process starts" behaviour. Startup uses the marker check above so a missed timer is not also run on the next UTC day before 05:00.

The occasion marker remains blob `occasions/{yyyy-MM-dd}.json` in `collection-schedule`. The first fill of that date creates it. A later fill the same date replaces it under the etag. `filledAt` is the latest fill instant and may be any time of day. `operators` is the union of logins queued for that date so far. The blob existing is not a reason to skip the fill.

**Rationale:** FR-001, FR-015, FR-016, and FR-021 say 05:00 UTC is how often this delivery asks, not a window and not a cap. Each fill uses the same eligibility, including after a success and after a sign-in later that day. FR-015 also says a technical failure must not postpone the next unattended attempt until the following 05:00 UTC, which is what `unmetFill` is for. An all-day timer would wake the worker when this delivery is not asking. Filling on every worker start would rescan every operator on every cold start after a success. The marker-absent catch-up covers "down at 05:00, back later" without that loop. The spec forbids an extra wait after a finished scan inside the fill rules. It does not require the failure path to ask again in the same second.

**Alternatives considered:**

- **Keep `0 0-15 5 * * *`.** Rejected. The spec removed the grace window. Sixteen asks would queue a person again as soon as their scan finished inside that quarter-hour, which is not "once per UTC day".
- **Miss the UTC day after 05:15.** Rejected. A process that returns later still runs the fill.
- **Treat the occasion marker as a once-per-day lock.** Rejected. That is what the branch does today (`AlreadyFilled` when the marker exists). A later fill must be able to queue someone whose scan already succeeded.
- **Skip anyone whose daily run already succeeded today.** Rejected. The 2026-11-24 clarification says a success does not remove them from the next fill.
- **Skip anyone who completed sign-in after 05:00 UTC until the next day.** Rejected. That rule is in `HostedCollectionEligibilityEvaluator` today. FR-016 includes them when a fill runs after that sign-in.
- **Functions platform catch-up of a missed 05:00 timer, with no Application rule.** Rejected. The platform can run a missed timer on a later UTC day, before 05:00.
- **An all-day timer, or a fill on every sign-in.** Rejected. How often this delivery asks stays the timer, the missed-ask catch-up, and `unmetFill`. Sign-in does not itself start a scan.
- **A timer that also gates queue draining.** Rejected. Entries already queued, including further tries, run at any time the worker is up.

## Queue draining is not the fill clock

**Decision:** The queue trigger stays registered at all times. It has no NCRONTAB and no check against 05:00 UTC. A visible message is taken whenever the worker is running, including before 05:00, after 05:15, overnight, and on a later UTC day. Clock time is not a reason to delete a message. The 30-day sign-in window is not re-checked at dequeue. Collection messages do not call `TryFillDailyOccasion`. The timer, the missed-ask startup fill, and an `unmetFill` message are the only callers in this delivery. A later feature may ask several times a day, including every 10 minutes. This delivery does not add a schedule setting, and the consumer does not embed "once per day" in its run rules.

**Rationale:** FR-021 splits queuing from running. The queue trigger keeps work alive when the worker stops and comes back. Putting a fill clock on every collection message would drop work the spec says must still run. `unmetFill` is a fill signal, not a collection.

**Alternatives considered:**

- **Complete and drop messages seen after 05:15.** Rejected. That was a possible reading of the earlier spec. The clarification says clock time does not remove an entry already on the queue.
- **A second all-day timer that polls the queue.** Rejected. The storage queue trigger already starts the worker from scale-to-zero when a message is visible.

## Run rules when a message is about to run

**Decision:** Queuing and running use different checks. Fill-time eligibility is the list in the previous section. When a message is received and a pass has not yet started (an `initial` message, or a `retry` whose further try has not entered the walk), Application does the following at `TimeProvider.GetUtcNow()`:

- If the person is not currently allowed, withdraw without a failed collection. Admission uses the lists in configuration now and the organisation logins saved on the sign-in record. There is still no GitHub membership call. The 30-day window is not part of this check. Set stored status `suspended` and `suspensionReason` `notAllowed`. Do not overwrite `lastResult`, `lastScanAt`, or `resultComment`.
- Otherwise attempt the scan, including when `completedAt` is now older than 30 days because the message waited. If stored access cannot be used (no sign-in record, `credentialUnusable` already set, or token exchange fails) and no newer worker token is stored, set stored status `suspended`, `lastResult` failure, and the sign-in or token comment. Set `credentialUnusable` when a record exists, stop further tries, and do not enqueue `unmetFill`. When a newer worker token is already stored, set `finished` with that failure, leave `suspensionReason` empty, leave `credentialUnusable` false, stop further tries, and do not enqueue `unmetFill`. That is a real run, not a silent skip. T115 already stops `IsEligibleAtRun` from treating a closed window or `credentialUnusable` as a silent withdrawal.

Withdrawal when they are not allowed:

- Complete the message. Do not enqueue another.
- Do not call GitHub and do not take the runner lease.
- Do not write a failed snapshot or `interrupted`, and do not set `credentialUnusable`.
- Leave `repositories` and `lastSuccessfulCollectionAt` unchanged.
- Set the account scan status to `suspended` and `suspensionReason` `notAllowed`, without changing the last scan time, result, or result comment. Clear `queueMessagePending`.
- If a further try was waiting, restore the previous terminal snapshot run from `retainedRun` and clear the attempt fields. The page shows `suspended` and the not-allowed comment, not a failed snapshot.

`retainedRun` is written when the first pass of the occasion replaces a terminal snapshot run. It holds that run and its `trigger`. It is cleared when this occasion succeeds or fails. It is not a log of past occasions. A token failure does not restore `retainedRun`. It replaces the snapshot run with the failed daily run. When no newer worker token is stored, the account status is `suspended`. When a newer worker token is already stored, the account status is `finished` and `credentialUnusable` stays false.

A pass already `running` is not withdrawn by the not-allowed rule. Redelivery of that pass leaves the stored status `running` and does not record a failure. See the account-scan-status section.

**Rationale:** FR-021 attempts the scan unless the person is not allowed. The 30-day window applies at fill and on the Repositories page, not at dequeue. The 2026-11-24 clarification records a sign-in or token failure when stored access cannot be used, and removes the queue entry with no failed collection only when they are not allowed. Restoring one previous run on that withdrawal avoids inventing a failed daily run and avoids a history list, which FR-018 forbids.

**Alternatives considered:**

- **Record a failed collection when the person is no longer allowed.** Rejected. The spec says remove the entry and do not record a failed collection.
- **Drop the queue entry with no failed collection when stored access cannot be used.** Rejected. That was the previous plan. The clarification requires a `signInOrToken` failure and the stored-access wait.
- **Re-check the 30-day window at dequeue and skip when it is closed.** Rejected. The branch still does this in `IsEligibleAtRun`. FR-001 and FR-017 forbid it.
- **Leave the further try in progress until the next fill.** Rejected. The page must not keep showing that run as in progress after a not-allowed withdrawal.
- **Call GitHub to refresh organisation membership at dequeue.** Rejected. The same saved logins used at fill are the membership set.

## Minimum wait before a further try

**Decision:** A product retry is still a new tail message, not platform poison handling. The message gains `notBefore` (UTC). The publisher sets the Azure Storage Queue initial visibility timeout to `notBefore - now` when that delay is positive, so the message is on the queue but not visible yet. Other visible messages are taken first. When this account is the only one queued, the further try still does not start until `notBefore`.

The delay is chosen when the failed attempt enqueues the retry, from that attempt only:

| Failure | Minimum | `notBefore` |
| --- | --- | --- |
| Rate limit, and the failed response already carried a positive retry delay | That delay | Now plus that delay |
| Rate limit, no usable delay | 2 minutes | Now plus 2 minutes |
| Unexpected error, first further try (`attemptsUsed` 1) | 3 minutes | Now plus 3 minutes |
| Unexpected error, second further try (`attemptsUsed` 2) | 6 minutes | Now plus 6 minutes |
| Unexpected error, third further try (`attemptsUsed` 3) | 12 minutes | Now plus 12 minutes |

A usable GitHub delay is `Retry-After` (delta-seconds or HTTP-date) or `X-RateLimit-Reset` (UTC unix seconds) on the response that already failed the walk. `EnsureSuccessStatusCodeAsync` already has that response before it throws. Thread the parsed delay into the existing rate-limit mapping. Do not make another GitHub call to discover a reset. Missing, zero, negative, or unparseable values use 2 minutes. Cap the visibility timeout at the Storage Queue maximum of 7 days.

A copy that loses the runner lease re-sends the same payload. For a retry it recomputes visibility from the existing `notBefore` so the wait is not shortened. Initial messages and fill repair stay immediately visible. Hand-started collections are not queued and do not gain these waits.

If a message arrives before `notBefore` (clock skew or a publisher that omitted the timeout), the handler re-queues it with the remaining delay and does not start the pass.

**Rationale:** Queue position alone does not satisfy the spec when one account is queued. Visibility timeout is the queue feature that holds a message at the back without a second timer or a sleep inside the function, which would block the single runner. The stagger is measured from the failed attempt because `notBefore` is stamped then.

**Alternatives considered:**

- **`Task.Delay` inside the queue function for the whole wait.** Rejected. It would hold the runner lease and a visible message across the wait, so other accounts could not pass it, and it would fight scale-to-zero.
- **A second scheduled message or Durable timer.** Rejected. The same queue already orders work. Durable Task stays out of this feature.
- **Always wait 2 minutes, including when GitHub sent a reset.** Rejected. The spec uses the GitHub-indicated period when the failed response already has one.
- **Treat the three product tries as `maxDequeueCount`.** Still rejected. Platform redelivery remains the crash signal.

## One queue, one pass at a time, crash behaviour

**Decision:** Azure Storage Queue `daily-snapshot-collection` on `operator-snapshot-queues`. The Functions host registers `AddAzureQueueServiceClient("operator-snapshot-queues")` from `Aspire.Azure.Storage.Queues` 17.0.0 and uses `QueueServiceClient.GetQueueClient("daily-snapshot-collection")`. Application code does not read a connection string.

The consumer is a queue trigger with `batchSize` 1. Correctness does not depend on max replicas being 1. A blob lease `runner` in `collection-schedule` allows one daily pass at a time across copies. A copy that cannot get the lease sends the payload to the tail and completes the current message, so the platform does not poison it and does not run a second pass.

Message handling:

| Account scan status when the message is received | Action |
| --- | --- |
| Not allowed now, and status is not a live `running` scan | Complete the message. Do not walk GitHub. Set `suspended` and `suspensionReason` `notAllowed` without changing the last scan time, result, or result comment. Clear `queueMessagePending`. No failed snapshot. Do not apply the 30-day window here. |
| Worker token missing or unusable, and status is not a live `running` scan | Complete the message. Do not walk the catalogue. Do not exchange the cookie token. When a newer worker token is already stored, set `finished` with the sign-in or token failure and do not set `credentialUnusable`. Otherwise set `suspended`, that failure, and `credentialUnusable`. No retry and no `unmetFill`. |
| `notBefore` still in the future | Re-queue with the remaining invisibility. Leave status `scheduled`. Do not start a pass. |
| `scheduled`, still allowed, message is the initial or a due retry | Set `running`, write the liveness mark, renew visibility, run the collection, then complete the message. A previous success the same day does not take this row. |
| `running` already, redelivery after the worker died or the mark is stale | Do not walk GitHub again. Do not write `finished` or a short failure reason. Do not enqueue `unmetFill`. Complete the message. The pass does not rejoin the queue. |
| Not `scheduled`, and no pass is `running` | Complete the message and do nothing. This is a duplicate, not "already succeeded today". |

Visibility starts at 30 seconds. The executing pass renews it about every 10 seconds so a live pass is not redelivered. Operator-facing liveness is the status blob mark, at least every 60 seconds, not a field on the snapshot. A dead worker stops both. The next delivery completes the message and leaves status `running`. Readers are told `stalled` only after more than 5 minutes without a mark. A login that is only `scheduled` is not reported `stalled`. Those messages stay on the queue and run when a worker is back, including after 05:15.

`functionTimeout` in the Functions `host.json` is `02:00:00`. This publish path is the ACA Functions host, not the Consumption plan's 10-minute cap. A timeout, scale-in, or host stop while a pass is `running` is the `stalled` report once the mark lapses. It is not one of the three further tries and it is not stored as `finished`.

Product retries (rate limit or unexpected error, up to three further tries) are a new tail message written by Application code, invisible until `notBefore`. They are not the platform poison queue. Status returns to `scheduled` while that message waits. The last result is not changed to failure between tries. When those tries are exhausted, Application sets `finished` with that failure and enqueues one `unmetFill` message hidden for 15 minutes. That message runs the same fill. It is not a fourth try and it is not published for a liveness lapse. People whose scan already succeeded are included when they are eligible and idle. A sign-in or token failure completes the message and does not enqueue a retry or an `unmetFill`. Hand-started collections do not use this queue and do not get those tries. They do update the same status blob and the same liveness duty. A message is not dropped because of the clock.

**Rationale:** Completing a message only after a successful pass would put the pass back on the queue when the host dies, which the spec forbids. Deleting before the walk, without a redelivery rule, would lose the crash signal. Status `running` plus a short visibility timeout distinguishes "never started" from "died mid-pass". The redelivery completes the message and does not invent a failure. The 5-minute liveness rule is what callers see. The runner lease is what keeps two copies from walking GitHub at once. KEDA keeps the replica for the active invocation, then scales to zero when the queue is idle.

**Alternatives considered:**

- **In-memory queue inside the Functions host.** Rejected. A restart would drop people who had not started, and a second copy would not share it.
- **`maxDequeueCount` as the three product tries.** Rejected. Platform redelivery is also how a crash is observed. Those are different outcomes.
- **Web peeking the queue to render "queued".** Rejected. The status blob holds `scheduled` and `queueMessagePending`. `web` reads that blob and does not need the queue client.

## Durable sign-in record

**Decision:** On each completed hosted sign-in, `web` writes blob `{sanitised-login}.json` in `hosted-sign-ins`. The cookie remains the interactive session and is the only store for the browser refresh token. The blob is what the worker reads when the operator is signed out and `web` is scaled to zero. Sign-out clears the cookie and does not delete the blob. From schema version 2 the blob stores the login, `completedAt`, the organisation logins saved at that sign-in, `workerRefreshToken`, `workerRefreshTokenExpiresAt`, and `credentialUnusable`. It does not store the cookie token. See **Worker credential and human-friendly text** for the two grants, schema 1, and when the wait is cleared. Neither secret is written to the snapshot document, the account scan status, the queue message, or logs.

Token exchange stays behind the Application port implemented in Infrastructure. The Functions trigger methods do not call GitHub. The worker calls that port with `workerRefreshToken` only. A replacement is written back with the blob etag only while the stored worker token is still the one that scan started with.

Eligibility at fill time is Application code: list sign-in blobs, keep those with `completedAt` no older than 30 days at that fill instant (exactly 30 days is inside), drop `credentialUnusable`, then apply the admission lists from configuration. Do not also require `completedAt` to be at or before 05:00 UTC that day. Organisation allowance compares the saved logins with `HostedAdmissionControl__AllowedOrganisationLogins` as injected at fill time. There is no GitHub membership request. The same record is read again when a queued message is about to run. Allowance is the only skip. A missing record or unusable stored access fails the run with `signInOrToken`. The 30-day window is not applied at dequeue.

**Rationale:** The browser refresh token lives in the auth cookie (`HostedGitHubAuthGateway`). A scaled-to-zero web app and a signed-out operator have no cookie for the worker to read, and FR-024 forbids the worker from using that cookie token even when it is present. The sign-in blob remains the smallest store for the separate worker grant. Private blob RBAC plus the existing storage account matches ADR-0045 (no new in-app Key Vault client).

**Alternatives considered:**

- **Reading the data-protected cookie from disk.** Rejected. The cookie is per browser, and the Container App disk is ephemeral. Scale-to-zero does not leave a shared cookie jar.
- **Key Vault secret per operator.** Rejected. Key Vault is the operator's app secrets (ADR-0045), not a per-user token directory.
- **Putting either refresh token in the snapshot JSON.** Rejected. The snapshot is a catalogue document. Feature 001 forbids tokens in it.

## Snapshot document and a stopped scan

**Decision:** Keep snapshot `schemaVersion` 1. The catalogue document remains the latest successful snapshot and the terminal run when a scan actually finishes. It is not the account scan status. Stop writing `queuedOccasion`, `passExecuting`, and `passLeaseUntil` for new behaviour. Hosted page state comes from the status blob. The web read path must not treat a daily `inProgress` snapshot with no in-memory live task as `siteStopped`, and it must not rewrite it to `scheduledCollectionStopped` because a lease time has passed.

| Situation | Snapshot | Account scan status |
| --- | --- | --- |
| Hand-started lean collection, web process died | Existing `siteStopped` path | No status blob. Lean copy only. |
| Hand-started hosted collection, web process died during the walk | Do not publish the partial walk as a success | Stays `running` until the mark is older than 5 minutes, then callers are told `stalled`. The same pass may still commit a real result until a newer scan wins. |
| Daily pass, worker died | Do not write `interrupted` or a short failure reason. Partial recordings are not a new success | Stays `running`. Callers are told `stalled` after more than 5 minutes. |
| Daily, queued or retry waiting, worker down | Unchanged | Stays `scheduled`. Not `stalled`. |
| Daily pass, web scaled to zero, worker still writing liveness | Unchanged | Stays `running`. |

`web` stopping is not a reason to change a worker-owned status. The existing `siteStopped` sentence stays for a lean collection the operator started on this machine.

**Rationale:** `BlobOperatorSnapshotStore.MaybeRewriteInterruptedAsync` rewrites any `inProgress` document that this process is not running. A daily walk runs in the Functions process, so the next Repositories visit would otherwise record "the site stopped", which FR-012 forbids. FR-023 also forbids storing that stop as a finished failure.

**Alternatives considered:**

- **Keep `passLeaseUntil` as the stall signal on the snapshot.** Rejected. FR-022 says the page reads the stored account scan status, and the mark belongs on that record.
- **Bump the snapshot to `schemaVersion` 2.** Rejected. The status blob is a new document. The snapshot stays the feature 001 catalogue.
- **A second snapshot for the daily run.** Rejected by the spec.

## Packages

**Decision:** Planning does not add packages. Implementation adds, at the 17.0.0 family already in `Directory.Packages.props`:

- `Aspire.Hosting.Azure.Functions` 17.0.0 on `SoloDevBoard.AppHost`
- `Aspire.Azure.Storage.Queues` 17.0.0 on `SoloDevBoard.Functions`

Functions worker packages (`Microsoft.Azure.Functions.Worker`, `Microsoft.Azure.Functions.Worker.Sdk`, timer extension, storage queues extension) are resolved at implementation with the repo's package inspection. Do not guess those versions in the plan. Do not add an HTTP trigger package unless a later change adds an HTTP function. Do not add EF Core, SQL, Service Bus, or Durable Task packages.

**Rationale:** Constitution and `AGENTS.md` require an explicit package add at implementation time, on one Aspire release family. The user asked this plan to use Azure Functions, so the add is in scope for implementation, not for this planning command.

## Tests and docs

**Decision:** Keep the 2026-11-24 Application tests for fill selection, catch-up, one pass at a time, the three further tries, the minimum waits, and `unmetFill` after an exhausted rate-limit or unexpected error. Extend them for the status record. Use xUnit v3, built-in `Assert`, and NSubstitute only for a port that is not the subject. Use `TimeProvider`. Do not collapse these into one parameterised case:

- Liveness age of exactly 5 minutes is reported `running`.
- Liveness age of more than 5 minutes is reported `stalled`, and the stored status is still `running`.
- A scan that stays in progress across more than 60 seconds of fake time records another mark before that gap elapses. A scan that keeps the cadence is not reported `stalled`.
- A `stalled` account is queued by the next fill when the other rules allow, and the winner sets `scheduled`. A fresh `running` account is not queued.
- Two attempts (fill and hand start) against one `stalled` account: one etag wins, the loser does not enqueue and does not change the snapshot.
- An earlier pass that commits after a newer generation does not change status, liveness, or the snapshot. The same pass may commit while the generation is unchanged, including after callers were told `stalled`.
- Orphan `scheduled` with `queueMessagePending` false restores from the last result before the fill selects anyone. An invisible further try stays `scheduled`.
- Not allowed sets `suspended` and does not change the last result. Unusable worker access with no newer worker token sets `suspended` and the wait. The same failure when a newer worker token is already stored sets `finished` and does not set the wait. A waiting further try is `scheduled`, not `finished`.
- Redelivery after a dead worker does not write `finished` and does not enqueue `unmetFill`.
- A schema 1 `refreshToken` is not exchanged. A queued scan uses the worker token stored when it is taken. A running scan does not overwrite a newer worker token. A hand start uses the session access token when the worker token is missing.
- The FR-025 formatter returns the fixed sentences and does not include `trigger`, "daily occasion", or "05:00 UTC".

Store tests cover the etag loser and the absence of the refresh token on the status blob. They do not cover an interruption rewrite to `scheduledCollectionStopped`. bUnit in `tests/SoloDevBoard.Web.Tests/RepositoriesTests.cs` covers the reported words (`never run`, `scheduled`, `running`, `finished`, `suspended`, `stalled`) and the schedule sentences in [contracts/repositories-schedule-copy.md](contracts/repositories-schedule-copy.md). Names follow `MethodUnderTest_Scenario_ExpectedOutcome`. Do not unit-test `HostedSignInAppHostGraph` or `LeanAppHostGraph`. Do not add a Playwright journey that walks GitHub or waits for 05:00 UTC. Update `website/content/docs/repositories.md` and the locking tests in the same implementation change when the page copy changes.

**Rationale:** Principle III. The schedule rules and the liveness boundaries are deterministic once the clock and the stores are fakes. The AppHost graph is excluded by the constitution. Aspireify's validate step (`aspire start`) is a manual hosted check in the quickstart, not a unit test.
