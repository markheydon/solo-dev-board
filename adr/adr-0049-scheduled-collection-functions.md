---
title: "ADR-0049: Scheduled snapshot collection Functions worker"
status: "Accepted"
date: "2026-11-23"
authors: "Solo Dev Board maintainers"
tags: ["architecture", "aspire", "functions", "storage"]
supersedes: ""
superseded_by: ""
---

# ADR-0049: Scheduled snapshot collection Functions worker

## Status

**Accepted**

## Context

Feature 002 queues and runs the operator snapshot collection once each UTC day at 05:00 in hosted sign-in. The operator-facing web app stays scaled to zero between visits. A collection must queue and finish while that app is not running. The spec requires a scheduled process separate from the site. Timer logic inside the Blazor Container App would either miss scale-to-zero runs or force minimum replicas above zero.

ADR-0048 added one blob container on the hosted graph for snapshot JSON. This feature adds a sign-in blob, schedule blobs, and a queue on the same storage account, plus an isolated Azure Functions worker on the hosted sign-in AppHost graph only. ADR-0046 (SQL and Functions for overnight inventory) stays withdrawn and is not reinstated.

## Decision

- Add `src/SoloDevBoard.Functions`, a .NET isolated Azure Functions worker that references `SoloDevBoard.Composition` and `SoloDevBoard.ServiceDefaults`. Triggers are thin: a UTC timer (`0 0 5 * * *`) for the daily fill ask and a storage queue trigger with `batchSize` 1 on `daily-snapshot-collection`. No HTTP trigger.
- Wire the worker only in `HostedSignInAppHostGraph` using Aspire 17.0.0: `AddAzureFunctionsProject`, `WithHostStorage` on `operator-snapshot-storage`, `AddBlobContainer` for `operator-snapshots`, `hosted-sign-ins`, `account-scan-status`, and `collection-schedule`, `AddQueues("operator-snapshot-queues")`, `WithReference`, and local `WaitFor` the emulator. `LeanAppHostGraph` is unchanged.
- Publish assigns `StorageBlobDataContributor`, `StorageQueueDataContributor`, `StorageTableDataContributor`, and `StorageAccountContributor` on `operator-snapshot-storage` to the Functions identity.
- `web` keeps `MinReplicas` 0 and `MaxReplicas` 1. `web` references `operator-snapshots`, `hosted-sign-ins`, and `account-scan-status`. It does not reference the queue or `collection-schedule`.
- Occasion fill, eligibility, queue handling, and the daily pass live in Application. Infrastructure holds blob, queue, and lease adapters. Tokens stay out of snapshot JSON, queue messages, and logs.
- `functionTimeout` in `host.json` is `02:00:00`. The timer does not set `WEBSITE_TIME_ZONE`; execution is UTC.

## Consequences

### Positive

- **POS-001**: Daily collection runs while `web` is scaled to zero.
- **POS-002**: Lean `aspire start` stays web-only without Functions, Azurite, or extra storage.
- **POS-003**: One storage account and one queue reuse ADR-0048 infrastructure instead of SQL or a second account.

### Negative

- **NEG-001**: Hosted local development requires Azurite and the Functions host for schedule validation.
- **NEG-002**: Two hosts (web and worker) must share Application ports and auth configuration.
- **NEG-003**: Concurrency relies on blob leases and queue semantics rather than a single process.

## Alternatives Considered

### Timer inside the web Container App

- **ALT-001**: **Description**: Run the fill and collection walk inside Blazor Server.
- **ALT-002**: **Rejection Reason**: The site must scale to zero; a timer there either does not run when scaled down or forces warm replicas.

### Scheduled Azure Container App Job

- **ALT-001**: **Description**: Cron batch job for the daily walk.
- **ALT-002**: **Rejection Reason**: Poor fit for a 15-minute grace window, durable queue draining, and retries across worker restarts.

### Durable Task Scheduler

- **ALT-001**: **Description**: Orchestrate the daily pass with DTS.
- **ALT-002**: **Rejection Reason**: Experimental Aspire integration without deployment support; disproportionate for one queue and one pass at a time.

### Reinstate ADR-0046 (SQL and Functions inventory)

- **ALT-001**: **Description**: Relational store and overnight inventory Functions.
- **ALT-002**: **Rejection Reason**: Withdrawn; the snapshot remains JSON per ADR-0048.

## Implementation Notes

- **IMP-001**: Accept this ADR before editing `HostedSignInAppHostGraph`.
- **IMP-002**: Add `Aspire.Hosting.Azure.Functions` and `Aspire.Azure.Storage.Queues` at 17.0.0 with the existing Aspire family.
- **IMP-003**: Unit-test occasion rules with `TimeProvider`; do not unit-test the AppHost graph.

## References

- **REF-001**: [ADR-0048](adr-0048-hosted-operator-snapshot-blob-storage.md)
- **REF-002**: [specs/002-scheduled-snapshot-collection/research.md](../specs/002-scheduled-snapshot-collection/research.md)