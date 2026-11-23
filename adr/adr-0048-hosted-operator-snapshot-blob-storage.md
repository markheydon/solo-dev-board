---
title: "ADR-0048: Hosted operator snapshot blob storage"
status: "Accepted"
date: "2026-11-23"
authors: "Solo Dev Board maintainers"
tags: ["architecture", "storage", "aspire"]
supersedes: ""
superseded_by: ""
---

# ADR-0048: Hosted operator snapshot blob storage

## Status

**Accepted**

## Context

The operator repository snapshot feature stores one JSON document per GitHub login. Lean local trusted mode and lean personal Azure deploy must stay web-only without Azurite, SQL, Functions, or extra infrastructure on the lean AppHost graph. Hosted sign-in operators need the same snapshot to survive sign-out, sign-in, and Container Apps restart. Container Apps attached disk is ephemeral; a file on that disk is not a durable hosted store.

Withdrawn ADR-0046 chose Azure SQL and Functions for overnight inventory. That path is not implemented and must not be revived for this JSON document.

## Decision

- Add **one** Azure Storage account resource and **one** blob container (`operator-snapshots`) to the **hosted sign-in** AppHost graph only, using Aspire 17.0.0 APIs: `AddAzureStorage("operator-snapshot-storage")`, `AddBlobContainer("operator-snapshots", "operator-snapshots")`, `WithReference` from the `web` resource, and `RunAsEmulator()` for local hosted sign-in only.
- Lean AppHost graph (`LeanAppHostGraph`) remains web compute only with no storage, Azurite, SQL, or Functions.
- The web host registers `AddAzureBlobContainerClient("operator-snapshots")` only when Aspire injects that connection. Application and Infrastructure receive `BlobContainerClient` by injection and do not read connection strings.
- Local and lean deploy paths keep the file adapter under `{content root}/data/operator-snapshots/`. Hosted sign-in durability is the blob, not the container disk.
- Do not add Azure SQL, EF Core, Azure Functions, a scheduled Container App Job, or an `apiservice` for this feature.

## Consequences

### Positive

- **POS-001**: Hosted operators keep one current snapshot across restart and re-authentication without a database.
- **POS-002**: Lean `aspire start` stays a single web process with on-demand collection and a local file.
- **POS-003**: Aligns with constitution Principle VI (Aspire client in Web) and Principle VII (lean path).

### Negative

- **NEG-001**: Hosted local development needs the storage emulator when exercising the blob path.
- **NEG-002**: Two adapters (file and blob) must share one store contract and concurrency rules.
- **NEG-003**: Lean personal Azure deploy without the storage resource can lose the file on replica restart; the UI must say so.

## Alternatives Considered

### Azure SQL or EF Core (ADR-0046)

- **ALT-001**: **Description**: Relational store for snapshot rows.
- **ALT-002**: **Rejection Reason**: ADR-0046 is withdrawn. One JSON document per operator does not justify SQL for this feature.

### Azurite or storage on the lean graph

- **ALT-001**: **Description**: Run blob storage locally for PAT-only mode.
- **ALT-002**: **Rejection Reason**: Violates Principle VII; PAT-only `aspire start` must remain web-only.

### Container Apps disk as hosted durability

- **ALT-001**: **Description**: Rely on the replica file system without a blob.
- **ALT-002**: **Rejection Reason**: Disk is ephemeral; restarts can drop the snapshot.

## Implementation Notes

- **IMP-001**: Accept this ADR before any AppHost storage edit.
- **IMP-002**: Package versions: `Aspire.Hosting.Azure.Storage` 17.0.0, `Aspire.Azure.Storage.Blobs` 17.0.0, `Azure.Storage.Blobs` 12.29.2 (aligned to the Aspire client).
- **IMP-003**: Publish hosted sign-in omits `RunAsEmulator()`.

## References

- **REF-001**: [ADR-0046](adr-0046-overnight-inventory-functions-and-sql.md) (Withdrawn)
- **REF-002**: [ADR-0047](adr-0047-apphost-blazor-resource-name-web.md)
- **REF-003**: Feature spec `specs/001-operator-snapshot-scanner/`