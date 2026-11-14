# ADR-0013: One-Click Migration Scope and Preview/Conflict Strategy

**Date:** 2026-03-12
**Status:** Accepted

## Context
The One-Click Migration epic (Epic #87) aims to provide a streamlined workflow for migrating repository configurations between GitHub repositories. The planned backlog scope spans labels, milestones, and project board column configuration. The current architecture already supports preview/apply patterns for label migration, and milestone migration can be implemented using similar approaches. However, project board configuration migration would require integration with GitHub Projects v2 and GraphQL APIs, which are not yet present in the codebase. See ADR-0011 for boundary data shape rules.

## Decision
- The v0.3.0 delivery slice covers label and milestone migration only.
- Project board column configuration migration is deferred to a later slice, pending GitHub Projects v2/GraphQL support.
- The migration flow supports one source repository and one or more target repositories.
- A preview-first flow is mandatory before any apply operation.
- Conflict resolution options are skip, overwrite, and merge.
- The Application-to-App boundary uses DTOs exclusively, aligned with ADR-0011.
- UI work will reuse MudBlazor layout primitives and existing repository-selection patterns wherever practical.

## Rationale
- Limiting the initial delivery to labels and milestones ensures a manageable scope for v0.3.0 and leverages existing architectural patterns.
- Deferring project board migration avoids premature complexity and aligns with the need for GitHub Projects v2/GraphQL integration.
- Preview-first and explicit conflict resolution options reduce risk and improve user confidence.
- Consistent use of DTOs at the Application-to-App boundary maintains architectural clarity and testability.
- Reusing MudBlazor primitives and repository-selection patterns accelerates UI development and ensures consistency.

## Consequences
- Users will be able to migrate labels and milestones between repositories in v0.3.0, but project board configuration migration will not be available until a later release.
- The migration UI will provide a preview and require explicit confirmation before applying changes.
- Conflict resolution will be handled transparently, with clear options and outcomes.
- Future slices will require additional architectural work to support project board migration, including ADR updates and new API integrations.

## Alternatives Considered
- Including project board migration in v0.3.0: Rejected due to lack of GitHub Projects v2/GraphQL support and increased complexity.
- Allowing apply without preview: Rejected to minimise risk and ensure user control.
- Using domain entities at the Application-to-App boundary: Rejected in favour of DTOs per ADR-0011.
