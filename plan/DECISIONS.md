# SoloDevBoard — Decision Log

<!-- AI Collaborator Instructions: Do not create new ADR files. Route decisions per repo-decision-log skill: constitution (AGENTS.md / instructions), this log, or feature issues. -->

Active technical decisions for SoloDevBoard. Always-on rules live in [`AGENTS.md`](../AGENTS.md) and path-scoped [`.github/instructions/`](../.github/instructions/). Full historical ADR prose is in [`adr/archive/`](../adr/archive/).

A formal migration to GitHub Spec Kit is planned — see [`plan/SPEC_KIT_MIGRATION.md`](SPEC_KIT_MIGRATION.md) (parked).

---

## Active decisions

### DEC-001: Blazor Server for the front-end

**Status:** Active  
**Date:** 2026-02-15  
**Legacy:** [ADR-0001](../adr/archive/0001-blazor-server.md)  
**Constitution:** [AGENTS.md — Language & Framework](../AGENTS.md#language--framework)  
**Summary:** SoloDevBoard uses Blazor Server (.NET 10) for the presentation layer. Reject SPA frameworks or Blazor WebAssembly unless a future decision log entry and constitution update explicitly change the stack.

---

### DEC-002: Layered / clean architecture

**Status:** Active  
**Date:** 2026-02-15  
**Legacy:** [ADR-0004](../adr/archive/0004-layered-architecture.md)  
**Constitution:** [AGENTS.md — Architecture](../AGENTS.md#architecture)  
**Summary:** Four projects (App, Application, Domain, Infrastructure) with strict dependency direction. Domain has no external dependencies; App never references Infrastructure directly. Reject service locator patterns and cross-layer shortcuts.

---

### DEC-003: GitHub API strategy — REST, GraphQL, PAT, and GitHub App

**Status:** Active  
**Date:** 2026-02-20  
**Legacy:** [ADR-0005](../adr/archive/0005-github-api-strategy.md)  
**Summary:** Use GitHub REST and GraphQL as appropriate. Support PAT for local trusted development and GitHub App credentials for hosted deployments. Reject ad-hoc unauthenticated API access or undocumented token storage.

---

### DEC-004: Moq for test mocking

**Status:** Superseded by DEC-016  
**Date:** 2026-02-28  
**Legacy:** [ADR-0006](../adr/archive/0006-moq-mocking-library.md)  
**Constitution:** [AGENTS.md — Testing](../AGENTS.md#testing)  
**Summary:** Use Moq as the sole mocking library in test projects. Reject NSubstitute and other mocking frameworks (supersedes legacy ADR-0002).

---

### DEC-005: Multi-tenancy authentication — phased approach

**Status:** Active  
**Date:** 2026-03-01  
**Legacy:** [ADR-0007](../adr/archive/0007-multi-tenancy-authentication-phased-approach.md)  
**Summary:** Prepare multi-user hosting in phases: single-user PAT mode locally, hosted sign-in with admission control, and deferred full multi-tenancy. Reject bolting multi-tenant data isolation onto the schema without an explicit phase plan.

---

### DEC-006: No FluentAssertions — xUnit built-in assertions only

**Status:** Active  
**Date:** 2026-03-05  
**Legacy:** [ADR-0008](../adr/archive/0008-remove-fluentassertions.md)  
**Constitution:** [AGENTS.md — Testing](../AGENTS.md#testing)  
**Summary:** Use xUnit `Assert.*` methods exclusively. FluentAssertions requires a commercial licence and is prohibited in this open-source project. Reject adding FluentAssertions or similar licensed assertion libraries.

---

### DEC-007: bUnit for Blazor component testing

**Status:** Active (amended by DEC-016)  
**Date:** 2026-03-06  
**Legacy:** [ADR-0010](../adr/archive/0010-bunit-component-testing.md)  
**Summary:** Use bUnit for Blazor component tests in the App test project. Reject Playwright-only coverage where a bUnit unit/component test suffices for logic and rendering contracts. Playwright end-to-end tests complement bUnit for cross-page user journeys (see DEC-016).

---

### DEC-008: Boundary data shapes — DTOs at Application→App boundary

**Status:** Active  
**Date:** 2026-03-08  
**Legacy:** [ADR-0011](../adr/archive/0011-boundary-data-shapes.md)  
**Constitution:** [AGENTS.md — Boundary Data Shapes](../AGENTS.md#boundary-data-shapes-dec-008)  
**Summary:** Repository interfaces return domain entities; public Application service interfaces return DTO records only. Domain entities must never appear in App-layer service signatures. Mapping happens in Application services, not Razor components or AutoMapper.

---

### DEC-009: MudBlazor as the sole UI component library

**Status:** Active  
**Date:** 2026-03-09  
**Legacy:** [ADR-0012](../adr/archive/0012-switch-to-mudblazor-component-library.md)  
**Constitution:** [`.github/instructions/blazor.instructions.md`](../.github/instructions/blazor.instructions.md)  
**Summary:** Use MudBlazor for all Blazor UI. Prefer MudBlazor layout primitives and utility classes before custom CSS. Reject Fluent UI Blazor and raw HTML controls where a MudBlazor equivalent exists (supersedes legacy ADR-0009).

---

### DEC-010: One-click migration scope and preview strategy

**Status:** Active  
**Date:** 2026-03-12  
**Legacy:** [ADR-0013](../adr/archive/0013-one-click-migration-scope-and-preview-strategy.md)  
**Summary:** Migration features use preview-then-apply patterns. Initial scope covers labels and milestones; project board configuration migration requires future GraphQL work. Reject exposing domain entities at the Application→App boundary in migration flows.

---

### DEC-011: Hosted access control for public deployments

**Status:** Active  
**Date:** 2026-03-12  
**Legacy:** [ADR-0014](../adr/archive/0014-hosted-access-control-for-public-deployments.md)  
**Summary:** Public hosted deployments enforce operator-controlled admission (allow-lists). Reject open sign-in without admission checks on public endpoints.

---

### DEC-012: GitHub App-first hosted authentication

**Status:** Active  
**Date:** 2026-03-13  
**Legacy:** [ADR-0015](../adr/archive/0015-github-app-first-hosted-authentication.md)  
**Summary:** Hosted sign-in prioritises GitHub App user authentication with admission control layered on top. OAuth App and hybrid flows are demoted to fallback only when App auth cannot satisfy requirements.

---

### DEC-013: Aspire for local orchestration

**Status:** Active  
**Date:** 2026-05-03  
**Legacy:** [ADR-0016](../adr/archive/0016-consider-aspire-for-local-orchestration-and-future-hosting.md)  
**Summary:** .NET Aspire AppHost orchestrates local development, dev containers, and Codespaces. Reject maintaining parallel hand-run scripts as the primary local path when Aspire covers the scenario.

---

### DEC-014: GitHub Actions bridge for roadmap project sync

**Status:** Active  
**Date:** 2026-03-20  
**Legacy:** [ADR-0017](../adr/archive/0017-github-actions-bridge-for-roadmap-project-sync.md)  
**Summary:** Repository automation that cannot use `GITHUB_TOKEN` alone uses a scoped bridge token in GitHub Secrets for roadmap/project sync workflows. Reject embedding long-lived tokens in source control.

---

### DEC-015: Aspire Azure Container Apps deployment

**Status:** Active  
**Date:** 2026-07-14  
**Legacy:** [ADR-0018](../adr/archive/0018-aspire-azure-container-apps-deployment.md)  
**Constitution:** [AGENTS.md — Infrastructure](../AGENTS.md#infrastructure)  
**Summary:** Production deploys via `aspire deploy` from AppHost to Azure Container Apps. Aspire generates deployment Bicep at deploy time. Reject hand-authored `infra/*.bicep` for production hosting (supersedes legacy ADR-0003).

---

### DEC-016: Formalised testing standard — xUnit v3, NSubstitute, Playwright E2E

**Status:** Active  
**Date:** 2026-07-21  
**Supersedes:** DEC-004  
**Constitution:** [AGENTS.md — Testing](../AGENTS.md#testing)  
**Summary:** Use xUnit v3 for all automated unit and component tests. Use NSubstitute for mocks, stubs, and test doubles. Use built-in xUnit `Assert.*` methods only. Use bUnit for Blazor component tests (DEC-007). Use Playwright for end-to-end tests covering key user journeys — not as a replacement for unit tests. Do not test .NET Aspire AppHost modelling or orchestration. Reject FluentAssertions, AwesomeAssertions, Shouldly, Moq, NUnit, and MSTest.

---

### DEC-017: Key Vault-backed hosted auth secrets

**Status:** Active  
**Date:** 2026-07-24  
**Constitution:** [AGENTS.md — Infrastructure](../AGENTS.md#infrastructure)  
**Summary:** Hosted authentication secret parameters (`gh-pat`, `gh-app-client-secret`) are persisted in an Aspire-provisioned Azure Key Vault at deploy time and injected into Azure Container Apps as Key Vault references. Local development continues to bind secret parameters directly from Aspire user secrets. Non-secret hosted auth configuration remains plain environment variables. Reject storing hosted auth secrets in container images, committed files, or plain-text Container App settings in production.

---

### DEC-018: GitHub API response caching in Infrastructure

**Status:** Active  
**Date:** 2026-07-25  
**Legacy:** [ADR-0005](../adr/archive/0005-github-api-strategy.md)  
**Summary:** Cache read-heavy GitHub catalogue responses (repositories, labels, milestones) in Infrastructure using scoped `IMemoryCache` keys tied to the current user's owner login. Invalidate label and milestone catalogue entries on corresponding CRUD mutations. Reject Application-layer DTO caching, distributed cache for this tranche, and caching issues, pull requests, workflow runs, or GraphQL project board queries until a follow-up performance pass.

Test coverage expectations for cache-hit, cache-miss, invalidation, TTL expiry, and configuration validation are documented in [OPERATIONAL_HARDENING_TEST_COVERAGE.md](OPERATIONAL_HARDENING_TEST_COVERAGE.md).

---

### DEC-019: Hugo (Hextra) for end-user docs on GitHub Pages

**Status:** Active  
**Date:** 2026-07-26  
**Constitution:** [AGENTS.md — Documentation Sync](../AGENTS.md#documentation-sync)  
**Summary:** End-user documentation is authored as a Hugo site with the Hextra theme under `user-docs/` and published to GitHub Pages via the official Hugo GitHub Actions workflow. Repository-centric developer and operator documentation remains in `docs/` and is not served by Pages. Local preview uses `scripts/Invoke-HugoSite.ps1` with Podman or Docker so contributors do not need a local Hugo or Go toolchain. Reject returning to Jekyll/`docs/` as the Pages source, and reject mixing operator deployment guides into the published end-user site.

---

## Superseded legacy (archive only)

| Legacy ADR | Superseded by | Notes |
|------------|---------------|-------|
| [ADR-0002](../adr/archive/0002-testing-framework.md) | DEC-016, DEC-006 | Testing stack standardised on xUnit v3 and NSubstitute |
| DEC-004 | DEC-016 | Moq replaced by NSubstitute per formalised testing standard |
| [ADR-0003](../adr/archive/0003-bicep-infrastructure.md) | DEC-015 | Hand-maintained Bicep replaced by Aspire-generated deployment |
| [ADR-0009](../adr/archive/0009-fluent-ui-blazor-component-library.md) | DEC-009 | Fluent UI Blazor replaced by MudBlazor |
