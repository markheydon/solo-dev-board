# Research: Scheduled snapshot collection

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
- **`PublishAsScheduledAzureContainerAppJob`.** Rejected for this feature. A cron job is a good fit for a single finite batch, and Aspire documents it. It is a poor fit for a process that must notice it is back at any moment from 05:00:00 UTC through 05:15:00 UTC, then keep draining a queue that survives that process stopping, including after 05:15. The user asked for Functions. Jobs stay unused.
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
| Queue endpoint | `operator-snapshot-queues` | the account queue service |

`web` references `operator-snapshots` (already) and `hosted-sign-ins`. It does not reference the queue or `collection-schedule`. The Functions project references all of the above plus host storage. Queue name `daily-snapshot-collection` is created by the worker with the injected `QueueServiceClient`. It is not a separate Aspire resource.

Both hosts get the same hosted auth and admission environment already applied to `web` (`GitHubAuth__*`, `HostedAdmissionControl__*`, Key Vault secret reference for the GitHub App client secret on publish). The Functions host does not receive the PAT.

**Rationale:** Principle VII forbids auxiliary resources on the lean graph. Principle VI requires one AppHost graph and a binding ADR before this material add. ADR-0048 remains the blob decision for the snapshot document and is not withdrawn. Its "do not add Functions" line applies to the snapshot feature, not to this schedule. ADR-0046 is not reinstated. ADR-0049 (next number) is the binding record for the worker, the sign-in blob, the schedule blob, and the queue. Accept it before editing the AppHost.

**Alternatives considered:**

- **Implicit Functions host storage (second account).** Rejected. A second storage account duplicates the account ADR-0048 already added.
- **Omitting the worker from local hosted sign-in.** Rejected. Hosted sign-in is the posture that has the schedule, including local hosted sign-in. Lean remains the path without it.

## Occasion clock and the 15-minute grace

**Decision:** One timer trigger, NCRONTAB `0 0-15 5 * * *` (six fields, UTC). That fires at 05:00, 05:01, ... 05:15 UTC. Each tick calls one Application method, `TryFillDailyOccasion`. The method fills the queue only when the UTC clock is inside that closed window and the occasion marker blob does not already exist. A second copy, or a later tick in the same window, sees the marker and does not fill again. A host that is down for every tick in the window does not fill later that UTC day. There is no `RunOnStartup` catch-up and no Functions default late timer outside the window.

Do not set a local `WEBSITE_TIME_ZONE` or `TZ`. The Azure Functions default is UTC, which is the spec's clock.

The occasion marker is blob `occasions/{yyyy-MM-dd}.json` in `collection-schedule`, created with `IfNoneMatch = *` so two copies cannot both become the filler. The filler holds blob lease `runner` only for the fill, then releases it.

**Rationale:** A single `0 0 5 * * *` trigger misses the spec's rule that a process which is back by 05:15 still fills that day. An all-day timer would wake the worker overnight and fight scale-to-zero. The sixteen UTC ticks are the grace window itself.

**Alternatives considered:**

- **Functions timer catch-up after a long outage.** Rejected. The platform can run a missed timer hours later. The spec misses the day after 05:15.
- **Wall-clock check only at host start.** Rejected as the only mechanism. A host that stayed up through 05:00 still needs the 05:00 tick. The cron above covers both a host that is already up and a host that returns inside the window. A host that returns after 05:15 does not get a tick until the next day.
- **A timer that also gates queue draining.** Rejected. The 2026-11-24 spec limits 05:00 UTC and the grace window to filling the queue. Entries already queued, including further tries, run at any time the worker is up.

## Queue draining is not the fill clock

**Decision:** The queue trigger stays registered at all times. It has no NCRONTAB and no check that the UTC clock is inside 05:00 to 05:15. A visible message is taken whenever the worker is running, including after 05:15:00 UTC, overnight, and on a later UTC day. Passing the fill window is not a reason to delete a message or to skip a person who is still allowed and still inside 30 days. The timer function remains the only place that calls `TryFillDailyOccasion`. A later feature may change how often the queue is filled. This delivery does not add a schedule setting, and the consumer does not embed "once per day" in its run rules.

**Rationale:** FR-021 splits queuing from running. The fill cron already matches the grace window. The queue trigger is what keeps work alive when the worker stops and comes back. Putting the same clock on the consumer would drop work the spec now says must still run.

**Alternatives considered:**

- **Complete and drop messages seen after 05:15.** Rejected. That was a possible reading of the earlier spec. The clarification says clock time does not remove an entry already on the queue.
- **A second all-day timer that polls the queue.** Rejected. The storage queue trigger already starts the worker from scale-to-zero when a message is visible.

## Run rules when a message is about to run

**Decision:** Queuing and running use different checks. Fill-time eligibility is unchanged: 30 days and allowance at the occasion instant, saved organisation logins, no GitHub membership call, and not `credentialUnusable`. When a message is received and a pass has not yet started (an `initial` message, or a `retry` whose further try has not entered the walk), Application evaluates two run rules at `TimeProvider.GetUtcNow()`:

- The person is currently allowed. Admission uses the lists in configuration now and the organisation logins saved on the sign-in record. There is still no GitHub membership call.
- `completedAt` is still inside 30 days of that instant. Exactly 30 days is inside (`completedAt >= now - 30 days`).

The fill-only rule that a sign-in completed after 05:00 UTC is too late for that day's fill does not apply here. `credentialUnusable` is not this drop. A pass that actually runs and cannot use stored access still fails with `signInOrToken`, as FR-013.

If either run rule fails, or the sign-in record is missing:

- Complete the message. Do not enqueue another.
- Do not call GitHub and do not take the runner lease.
- Do not write `failed` or `interrupted`, and do not set `credentialUnusable`.
- Leave `repositories` and `lastSuccessfulCollectionAt` unchanged.
- If the pass has not started (`queuedOccasion` set, `run.status` not `inProgress`), clear `queuedOccasion` only. Do not set `trigger` to `daily` until a pass starts, so the previous run keeps its start.
- If a further try is waiting (`inProgress`, `passExecuting` false), restore the previous terminal run from `retainedRun` and clear the attempt fields. The page then stops showing that run as in progress.

`retainedRun` is written when the first pass of the occasion replaces a terminal run. It holds that run and its `trigger`. It is cleared when this occasion succeeds, fails, or is interrupted. It is not a log of past occasions. When the first pass had no prior terminal run, withdrawal leaves the document in the never-run reading (no finished failure, no interruption, no in-progress run).

A pass already executing (`passExecuting` true) is not withdrawn by these rules. Redelivery of that pass remains an interruption.

**Rationale:** The spec evaluates eligibility when the queue is filled and when the page names the next occasion. It evaluates allowance and the 30-day window again when a queued entry is about to run. Those later failures are not a collection outcome. The page already has `notAllowed` and `outsideWindow` sentences. Restoring one previous run avoids inventing a failed daily run and avoids a history list, which FR-018 forbids.

**Alternatives considered:**

- **Record a failed collection when the person is no longer allowed.** Rejected. The spec says remove the entry and do not record a failed collection.
- **Leave the further try in progress until the next fill.** Rejected. The page must not keep showing that run as in progress.
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

| Snapshot state when the message is received | Action |
| --- | --- |
| Not allowed now, or latest sign-in outside 30 days of now, and no pass is executing | Complete the message. Do not walk GitHub. Withdraw the queued presentation as in the run-rule section. No failed collection. |
| `notBefore` still in the future | Re-queue with the remaining invisibility. Do not start a pass. |
| Queued for this occasion, pass not started, run rules pass | Set `passExecuting`, renew visibility, run the collection, then complete the message. |
| Retry waiting at the back (`inProgress`, pass not executing), run rules pass, `notBefore` has passed | Same as a start. This is the next try of the same run. |
| `passExecuting` already true (redelivery after the worker died) | Do not walk GitHub again. Write interrupted with the scheduled-collection reason, then complete the message. The pass does not rejoin the queue. |
| Occasion already finished for that login | Complete the message and do nothing. |

Visibility starts at 30 seconds. The executing pass renews it about every 10 seconds, and renews `passLeaseUntil` on the snapshot document at the same cadence. A dead worker stops renewing. The next delivery, or a web read that sees `passExecuting` with `passLeaseUntil` in the past, writes the interruption. A queued login that has not started a pass has no `passExecuting` flag, so a dead worker does not interrupt it. Those messages stay on the queue and run when a worker is back, including after 05:15.

`functionTimeout` in the Functions `host.json` is `02:00:00`. This publish path is the ACA Functions host, not the Consumption plan's 10-minute cap. A timeout, scale-in, or host stop while a pass is executing is an interruption, not one of the three further tries.

Product retries (rate limit or unexpected error, up to three further tries) are a new tail message written by Application code, invisible until `notBefore`. They are not the platform poison queue. A sign-in or token failure completes the message and does not enqueue another. Hand-started collections do not use this queue and do not get those tries. A message is not dropped because the clock has moved past 05:15 UTC.

**Rationale:** Completing a message only after a successful pass would put the pass back on the queue when the host dies, which the spec forbids. Deleting before the walk, without a redelivery rule, would lose the crash signal. The `passExecuting` flag plus a short visibility timeout distinguishes "never started" from "died mid-pass". The runner lease is what keeps two copies from walking GitHub at once. KEDA keeps the replica for the active invocation, then scales to zero when the queue is idle.

**Alternatives considered:**

- **In-memory queue inside the Functions host.** Rejected. A restart would drop people who had not started, and a second copy would not share it.
- **`maxDequeueCount` as the three product tries.** Rejected. Platform redelivery is also how a crash is observed. Those are different outcomes.
- **Web peeking the queue to render "queued".** Rejected. The snapshot document holds `queuedOccasion`. `web` already reads that blob and does not need the queue client.

## Durable sign-in record

**Decision:** On each completed hosted sign-in, `web` writes blob `{sanitised-login}.json` in `hosted-sign-ins`. The cookie remains the interactive session. The blob is what the worker reads when the operator is signed out and `web` is scaled to zero. Sign-out clears the cookie and does not delete the blob. The record stores the login, `completedAt`, the organisation logins saved at that sign-in, the refresh token, and `credentialUnusable`. The refresh token is never written to the snapshot document, the queue message, or logs.

Token exchange moves behind an Application port implemented in Infrastructure, shared by the existing web gateway and the worker. The Functions trigger methods do not call GitHub. A refresh that returns a new refresh token is written back with the blob etag. `credentialUnusable` is set when a daily pass fails with the sign-in or token reason, and cleared only when a later hosted sign-in completes.

Eligibility at fill time is Application code: list sign-in blobs, keep those with `completedAt` no older than 30 days at that occasion (exactly 30 days is inside), drop `credentialUnusable`, then apply the admission lists from configuration. Organisation allowance compares the saved logins with `HostedAdmissionControl__AllowedOrganisationLogins` as injected at fill time. There is no GitHub membership request. The same record is read again when a queued message is about to run. That check uses the current UTC time and the allow lists as they stand then. It does not use the 05:00 UTC occasion instant.

**Rationale:** Today's refresh token and organisation logins live in the auth cookie (`HostedGitHubAuthGateway`). A scaled-to-zero web app and a signed-out operator have no cookie for the worker to read. A second store is the smallest way to keep the schedule off the site. Private blob RBAC plus the existing storage account matches ADR-0045 (no new in-app Key Vault client).

**Alternatives considered:**

- **Reading the data-protected cookie from disk.** Rejected. The cookie is per browser, and the Container App disk is ephemeral. Scale-to-zero does not leave a shared cookie jar.
- **Key Vault secret per operator.** Rejected. Key Vault is the operator's app secrets (ADR-0045), not a per-user token directory.
- **Putting the refresh token in the snapshot JSON.** Rejected. The snapshot is a catalogue document. Feature 001 forbids tokens in it.

## Snapshot document and interruption copy

**Decision:** Keep `schemaVersion` 1 and add fields. A reader that understands version 1 must implement the new interruption rule in the same release. The web read path must not treat every `inProgress` blob with no in-memory live task as `siteStopped`.

| Run | Who may mark it interrupted | Reason stored | Operator sentence |
| --- | --- | --- | --- |
| Hand-started, web process died or `ApplicationStopping` | Existing web live-task registry | `siteStopped` | The site stopped before the collection finished. |
| Daily pass executing, worker died or `passLeaseUntil` has passed | Worker redelivery, or the next web read | `scheduledCollectionStopped` (new) | The scheduled collection stopped before it finished. |
| Daily, queued or retry waiting, worker down | Nobody | unchanged | Not interrupted. The queue still runs when the worker is back. |
| Daily pass, web scaled to zero, worker still renewing `passLeaseUntil` | Nobody | unchanged | Not interrupted. |

`web` stopping is not a reason to interrupt a daily pass. The existing `siteStopped` sentence stays for a collection the operator started.

**Rationale:** `BlobOperatorSnapshotStore.MaybeRewriteInterruptedAsync` currently rewrites any `inProgress` document that this process is not running. A daily walk runs in the Functions process, so the next Repositories visit would otherwise record "the site stopped", which FR-012 forbids.

**Alternatives considered:**

- **Bump to `schemaVersion` 2.** Rejected. The only reader ships in the same change. Additive fields keep the feature 001 document readable. The behaviour change is in the read path, which this feature replaces.
- **A second snapshot for the daily run.** Rejected by the spec.

## Packages

**Decision:** Planning does not add packages. Implementation adds, at the 17.0.0 family already in `Directory.Packages.props`:

- `Aspire.Hosting.Azure.Functions` 17.0.0 on `SoloDevBoard.AppHost`
- `Aspire.Azure.Storage.Queues` 17.0.0 on `SoloDevBoard.Functions`

Functions worker packages (`Microsoft.Azure.Functions.Worker`, `Microsoft.Azure.Functions.Worker.Sdk`, timer extension, storage queues extension) are resolved at implementation with the repo's package inspection. Do not guess those versions in the plan. Do not add an HTTP trigger package unless a later change adds an HTTP function. Do not add EF Core, SQL, Service Bus, or Durable Task packages.

**Rationale:** Constitution and `AGENTS.md` require an explicit package add at implementation time, on one Aspire release family. The user asked this plan to use Azure Functions, so the add is in scope for implementation, not for this planning command.

## Tests and docs

**Decision:** Occasion window, single fill, one pass at a time, redelivery-versus-queue, the three further tries, the minimum waits, run-rule removal without a failed collection, admission from saved organisations, and the 30-day boundary at fill time and at dequeue time are Application unit tests with `TimeProvider`. Store tests cover the new interruption branch and `retainedRun` restore. bUnit covers the Repositories copy, including the no-schedule sentences after a queued entry is removed. Do not unit-test the AppHost graph. Do not add a Playwright journey that walks GitHub. Update `website/content/docs/repositories.md` and the locking tests in the same implementation change, because the page gains schedule copy.

**Rationale:** Principle III. The schedule rules are deterministic once the clock and the stores are fakes.
