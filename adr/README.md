# Architecture Decision Records

This directory contains the Architecture Decision Records (ADRs) for SoloDevBoard. Each ADR documents a significant architectural decision, the context in which it was made, the decision itself, and its consequences.

## Format

Each ADR follows this format:

```
# ADR-XXXX: Title

**Date:** YYYY-MM-DD
**Status:** [Proposed | Accepted | Deprecated | Superseded by ADR-XXXX]

## Context
## Decision
## Rationale
## Consequences
```

## Index

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-0001](0001-blazor-server.md) | Use Blazor Server for the Front-End | Accepted |
| [ADR-0002](0002-testing-framework.md) | Use xUnit and NSubstitute for Testing | Superseded by ADR-0006 |
| [ADR-0003](0003-bicep-infrastructure.md) | Use Bicep for Azure Infrastructure as Code | Accepted |
| [ADR-0004](0004-layered-architecture.md) | Use Layered / Clean Architecture | Accepted |
| [ADR-0005](0005-github-api-strategy.md) | GitHub API Strategy — REST + GraphQL with PAT and GitHub App Authentication | Accepted |
| [ADR-0006](0006-moq-mocking-library.md) | Switch Mocking Library from NSubstitute to Moq | Accepted (assertion library aspect superseded by ADR-0008) |
| [ADR-0007](0007-multi-tenancy-authentication-phased-approach.md) | Multi-Tenancy Authentication — Phased Approach | Accepted |
| [ADR-0008](0008-remove-fluentassertions.md) | Remove FluentAssertions — Use xUnit Built-in Assertions Only | Accepted |
| [ADR-0009](0009-fluent-ui-blazor-component-library.md) | Use Microsoft Fluent UI Blazor Component Library | Accepted |
| [ADR-0010](0010-bunit-component-testing.md) | Use bUnit for Blazor Component Testing | Accepted |
| [ADR-0011](0011-boundary-data-shapes.md) | Boundary Data Shapes — DTOs at the Application→App Boundary | Accepted |
| [ADR-0012](0012-switch-to-mudblazor-component-library.md) | Switch UI Component Library from Fluent UI Blazor to MudBlazor | Accepted — supersedes ADR-0009 |

## Adding a New ADR

1. Create a new file: `adr/XXXX-<kebab-case-title>.md` (increment the number).
2. Use the format above.
3. Add an entry to the index table in this file.
4. Reference the ADR from relevant documentation (e.g. `docs/user-guide/<feature>.md`).

## AI Collaborator Instructions

When an architectural decision is made during development:
1. Create a new ADR file following the format above.
2. Add it to the index table in this file.
3. If the decision supersedes an existing ADR, update the existing ADR's **Status** field to `Superseded by ADR-XXXX`.
4. Reference the new ADR from `plan/IMPLEMENTATION_PLAN.md` if it affects a phase's approach.
