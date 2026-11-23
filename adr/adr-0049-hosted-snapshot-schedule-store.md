---
title: "ADR-0049: Hosted snapshot schedule store"
status: "Accepted"
date: "2026-11-23"
authors: "Solo Dev Board maintainers"
tags: ["architecture", "storage", "security"]
supersedes: ""
superseded_by: ""
---

# ADR-0049: Hosted snapshot schedule store

## Status

**Accepted**

## Context

Feature 002 queues operator snapshot collection once per UTC day in hosted sign-in. Eligible operators may be signed out at 05:00 UTC. The product must persist organisation logins and a refresh token per operator without placing secrets in the snapshot JSON or in Application DTOs. Multiple Container Apps replicas must share one data-protection key ring so any replica can unprotect tokens written by another. Withdrawn ADR-0046 chose SQL and Functions for overnight work; that path must not be revived. Accepted ADR-0048 already provides one blob container for hosted snapshots.

## Decision

- Extend the existing `operator-snapshots` container with a `schedule/` prefix: sign-in records at `schedule/sign-ins/{sanitised}.json`, the occasion queue at `schedule/occasion.json`, and the data-protection key ring at `schedule/data-protection-keys.xml`.
- Protect refresh tokens with ASP.NET Core Data Protection before blob write. Persist the key ring in the same container via `Azure.Extensions.AspNetCore.DataProtection.Blobs` when the hosted blob connection is present.
- Run the scheduler as a `BackgroundService` in the existing `web` project only when hosted sign-in and blob durability are active. Do not add an AppHost resource, Azure Functions, or SQL.
- Lean PAT-only local trusted mode does not write schedule blobs and does not register the scheduler.

This ADR extends ADR-0048. It does not supersede or revive ADR-0046.

## Consequences

### Positive

- **POS-001**: Unattended daily collection can authenticate without an open browser session.
- **POS-002**: One container and one Aspire reference keep Principle VI and proportionality.
- **POS-003**: Shared key ring allows token rotation across replicas and restarts.

### Negative

- **NEG-001**: Container compromise exposes ciphertext; operators must treat storage RBAC as a security boundary.
- **NEG-002**: A new NuGet package (`Azure.Extensions.AspNetCore.DataProtection.Blobs`) is required in central package management.
- **NEG-003**: Conditional blob writes and leases add operational complexity on top of snapshot etags.

## Alternatives Considered

### Plaintext refresh tokens in the blob

- **ALT-001**: Store tokens without data protection.
- **ALT-002**: **Rejection Reason**: Container access is wider than the web process secret boundary.

### Second storage account or AppHost resource for schedule state

- **ALT-001**: Dedicated schedule storage.
- **ALT-002**: **Rejection Reason**: `schedule/` cannot collide with `{login}.json`; a second resource violates proportionality and Principle VI.

### Azure Functions timer for 05:00 UTC

- **ALT-001**: Separate compute for the occasion.
- **ALT-002**: **Rejection Reason**: Withdrawn ADR-0046; splits "product running" from queue fill semantics.

## Implementation Notes

- **IMP-001**: Accept this ADR before writing refresh tokens to blob storage.
- **IMP-002**: Sign-in records are written only on completed OAuth callback, not on silent cookie refresh.
- **IMP-003**: Lean startup must not require the blob connection for data-protection registration.

## References

- **REF-001**: [ADR-0048](adr-0048-hosted-operator-snapshot-blob-storage.md)
- **REF-002**: [ADR-0046](adr-0046-overnight-inventory-functions-and-sql.md) (Withdrawn)
- **REF-003**: `specs/002-scheduled-snapshot-collection/`