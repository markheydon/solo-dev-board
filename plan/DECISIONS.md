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
**Summary:** Hosted sign-in uses GitHub App user authentication with admission control layered on top. The legacy OAuth App fallback boundary has been removed. PAT-only local trusted mode remains for development and trusted personal self-hosting.

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

**Clarification (2026-08-19):** The AppHost opts into the Aspire CLI bundle (`AspireUseCliBundle=true`). Dashboard and DCP orchestration binaries come from the Aspire CLI (or the SDK-paired `Aspire.Cli` via `dnx` when no CLI is on `PATH`). Pin the CLI to the same version as `Aspire.AppHost.Sdk` (`13.5.0` today). Reject suppressing `ASPIRE010` while staying on NuGet-restored orchestration.

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

**Follow-up (issue #254, completed):** The V1 performance pass addressed Audit dashboard duplicate fetches via an Application-layer snapshot API and paginated workflow-run REST calls. Infrastructure caching was not expanded to issues, pull requests, or workflow runs.

Test coverage expectations for cache-hit, cache-miss, invalidation, TTL expiry, and configuration validation are documented in [OPERATIONAL_HARDENING_TEST_COVERAGE.md](OPERATIONAL_HARDENING_TEST_COVERAGE.md).

---

### DEC-019: Hugo (Hextra) for end-user docs on GitHub Pages

**Status:** Active  
**Date:** 2026-07-26  
**Constitution:** [AGENTS.md — Documentation Sync](../AGENTS.md#documentation-sync)  
**Summary:** End-user documentation is authored as a Hugo site with the Hextra theme (now under `website/` per DEC-023; formerly `user-docs/`) and published to GitHub Pages via the official Hugo GitHub Actions workflow. Repository-centric developer and operator documentation remains in `docs/` and is not served by Pages. Local preview uses `scripts/invoke-hugo-site.sh` (bash) or `scripts/Invoke-HugoSite.ps1` (PowerShell) with Podman or Docker so contributors do not need a local Hugo or Go toolchain. Reject returning to Jekyll/`docs/` as the Pages source, and reject mixing operator deployment guides into the published end-user site.

---

### DEC-020: Public-only docs capture mode for documentation screenshots

**Status:** Active  
**Date:** 2026-07-26  
**Constitution:** [AGENTS.md — Documentation Sync](../AGENTS.md#documentation-sync), [AGENTS.md — Open Source & Security](../AGENTS.md#open-source--security)  
**Summary:** Local documentation screenshot capture uses a `DocsCapture:Enabled` flag that restricts repository and Projects v2 catalogues to public GitHub content only. Filtering is applied at the Infrastructure catalogue chokepoints (`GitHubService` repository lists and project board discovery/definition). The mode defaults to disabled, is enabled only via local user secrets or `DocsCapture__Enabled`, and is intentionally not an Aspire AppHost deploy parameter. This is screenshot hygiene for published user-guide images, not a security boundary, and does not block write operations. Reject treating the flag as access control or exposing it as a hosted deployment switch.

---

### DEC-021: Two-tier CD pipeline

**Status:** Active  
**Date:** 2026-07-29  
**Constitution:** [AGENTS.md — Infrastructure](../AGENTS.md#infrastructure)  
**Summary:** GitHub Actions CD uses two protected GitHub Environments — `staging` and `production` — mapped to Aspire deploy environments `Staging` and `Production` respectively. The **app resource group** is an operator choice per GitHub Environment (`AZURE_RESOURCE_GROUP`); one RG for both tiers or separate RGs are both valid. Aspire `--environment` does not suffix Azure resource names; the AppHost must use distinct resource names for Staging (`aca-staging`, `app-staging`, `auth-secrets-staging`, `app-insights-staging`) while Production keeps the original names (`aca`, `app`, `auth-secrets`, `app-insights`) so an existing production Container App is not recreated when both tiers share an RG. **Staging** deploys automatically on push to `main` with GitHub App hosted sign-in for pre-release validation. **Production** deploys on `v*` release tags with GitHub App hosted sign-in and required environment reviewers. PAT-only authentication remains for local development and personal self-hosting via local `aspire deploy` only — not as a hosted CD tier, because a deployed PAT-mode instance exposes the token owner's full GitHub account to anyone who can reach the URL. End-user docs on GitHub Pages publish on `v*` tags only; pull requests validate Hugo builds without publishing. Reject deploying production on every `main` merge, publishing Pages on `main`, or a hosted CD tier that deploys PAT-only authentication.

**Clarification (2026-08-17):** An early production deploy overwrote staging because both tiers used Container App name `app` in the same RG. Staging isolation is the AppHost `AzureName` suffix. One app RG or two app RGs are operator choices; DEC-021 does not mandate either layout.

---

### DEC-025: Optional shared Azure Container Registry

**Status:** Active  
**Date:** 2026-08-17  
**Constitution:** [AGENTS.md — Infrastructure](../AGENTS.md#infrastructure)  
**Summary:** Hosted CD may opt in to an **existing** Azure Container Registry via AppHost parameters `acr-name` and `acr-resource-group` (`PublishAsExisting` + `WithAzureContainerRegistry` + `WithAcrPullIdentity`). The `acr-pull` identity receives `AcrPull` in a separate Bicep module so cross-resource-group registries deploy without Bicep scope errors ([Aspire #11256](https://github.com/dotnet/aspire/issues/11256)). When both parameters are omitted, Aspire keeps its default behaviour and provisions a registry in the app resource group — required for forks and self-hosters who need no extra setup. When both are set, Staging and Production share that registry; use the same `ACR_NAME` and `ACR_RESOURCE_GROUP` on both GitHub Environments (repository-level variables preferred). The GitHub Actions OIDC identity for shared-ACR layouts should live in the ACR resource group with AcrPush and User Access Administrator on the registry, plus Contributor and User Access Administrator on each app RG. Reject documenting shared ACR as mandatory, calling `WithPurgeTask` or SKU `ConfigureInfrastructure` on a registry owned by other projects, or granting AcrPush to the running Container App.

---

### DEC-022: Canonical pull request policy

**Status:** Active  
**Date:** 2026-08-16  
**Constitution:** [AGENTS.md — Contributing & Pull Requests](../AGENTS.md#contributing--pull-requests)  
**Summary:** Pull request title, body, labels, linking, draft state, assignee, milestone, and branch conventions are defined in [`plan/PULL_REQUEST_POLICY.md`](PULL_REQUEST_POLICY.md). That file is the single source of truth for humans and AI agents. Titles use `[<Type>] <Imperative summary> (#N)` (not Conventional Commits). Bodies must keep the GitHub PR template headings. PRs require `type/` and `priority/` labels (plus `status/in-review` while open) and must not be added as standalone Project #8 cards. Ready-for-review is the default when Verify gates pass; vendor draft defaults must be overridden. Reject ad-hoc agent title styles, template-free bodies, and unlabelled agent PRs.

---

### DEC-023: Public product site IA and canonical domain

**Status:** Active  
**Date:** 2026-08-16  
**Constitution:** [AGENTS.md — Documentation Sync](../AGENTS.md#documentation-sync)  
**Summary:** The Hugo/Hextra site lives under `website/` (renamed from `user-docs/`) and is the single public product site: marketing landing at `/`, narrative About pages at `/about/`, and User Guide at `/docs/`. Canonical URL is `https://solodevboard.com/` (apex) on GitHub Pages with tag-only publish (DEC-021). The landing is a short product-and-project page: an About-derived summary, one Learn more about the project CTA, and static capability tiles (icon + name + one-liner from published guide front matter, matching in-app nav icons). About is first in the site nav; GitHub is header chrome only. The User Guide is a how-to path, not the home-page narrative. Developer and operator documentation stays in `docs/` and is not served on the product domain (DEC-019). Reject a second Hugo site, a second publish pipeline, converting guide articles into marketing copy, or moving operator docs onto the product domain.

---

### DEC-024: Post-1.0 milestone numbering

**Status:** Superseded  
**Date:** 2026-08-17  
**Superseded by:** [DEC-027](#dec-027-post-10-milestone-and-work-item-hierarchy)  
**Summary:** After the public `v1.0.0` tag, do not ship a `v0.x` release. The original split (deferred slices on **v1.1.0**, Cross-Repo PM Workflow on **v1.2.0**) was replaced by a single active milestone model in DEC-027.

---

### DEC-027: Post-1.0 milestone and work-item hierarchy

**Status:** Active  
**Date:** 2026-08-18  
**Constitution:** [AGENTS.md — When Adding a New Feature](../AGENTS.md#when-adding-a-new-feature)  
**Related:** [DEC-024](#dec-024-post-10-milestone-numbering), [`plan/PROJECT_MANAGEMENT.md`](PROJECT_MANAGEMENT.md)  
**Summary:** After `v1.0.0`, maintain **one open GitHub milestone** at a time (currently **v1.1.0**). It holds deferred v1.0 slices ([#290](https://github.com/markheydon/solo-dev-board/issues/290)–[#292](https://github.com/markheydon/solo-dev-board/issues/292)), Cross-Repo PM Workflow as feature [#272](https://github.com/markheydon/solo-dev-board/issues/272) with stories [#273](https://github.com/markheydon/solo-dev-board/issues/273)–[#288](https://github.com/markheydon/solo-dev-board/issues/288), and dogfood fixes as they are raised. Further work stays **unmilestoned** until the next release is declared. Do not recreate a separate `v1.2.0` milestone for Cross-Repo PM Workflow — that increment ships under v1.1.0 (the brief unused v1.2.0 milestone was deleted on 2026-08-18). Platform-blocked work ([#293](https://github.com/markheydon/solo-dev-board/issues/293)) stays unmilestoned backlog. **Epic → Feature → Story** applies when each level names real product structure: Epics are shippable themes spanning multiple features (not milestone buckets); Features group related stories/enablers ([#272](https://github.com/markheydon/solo-dev-board/issues/272) is a catch-up exception — classified as a feature without a parent epic); Stories are delivery units. Use GitHub sub-issues for parent links. During catch-up, milestone stories may sit without Feature parents when they complete shipped features ([#40](https://github.com/markheydon/solo-dev-board/issues/40), [#88](https://github.com/markheydon/solo-dev-board/issues/88), etc.). Implementation **phases** in `IMPLEMENTATION_PLAN.md` are historical sequencing for the v1.0 release only; the Project board **Phase** field is legacy for closed pre-1.0 milestones. Reject bucket epics that duplicate milestones, fabricated Features, and multiple concurrent open milestones.

---

### DEC-026: Project #8 archive rule via Roadmap Sync

**Status:** Active  
**Date:** 2026-08-18  
**Related:** [DEC-014](#dec-014-github-actions-bridge-for-roadmap-project-sync), [`plan/PROJECT_BOARD_DESIGN.md`](PROJECT_BOARD_DESIGN.md)  
**Summary:** Closed, non-duplicate issues on Project #8 are archived by the Roadmap Sync bridge **14 calendar days after `closed_at`**, using `archiveProjectV2Item`. GitHub **Auto-archive items** stays off because its `updated:` filter tracks project-card activity, not issue closure, and bulk field writes reset that clock. Reopened issues are unarchived; duplicates are removed from the project, not archived. Eligible cards are archived without a preceding field-sync pass to limit API churn on catch-up runs. Reject enabling GitHub Auto-archive alongside Roadmap Sync or archiving by card `updated` timestamp.

---

### DEC-028: Blocked and Ice Box project Status options

**Status:** Active  
**Date:** 2026-08-18  
**Related:** [DEC-014](#dec-014-github-actions-bridge-for-roadmap-project-sync), [`plan/PROJECT_BOARD_DESIGN.md`](PROJECT_BOARD_DESIGN.md), [`plan/LABEL_STRATEGY.md`](LABEL_STRATEGY.md)  
**Summary:** Project #8 **Status** includes **Blocked** (`9796fb74`) and **Ice Box** (`1c235cb1`) alongside Todo, Up Next, In Progress, and Done. **Up Next** remains the only project-only status (no issue label). **Blocked** maps to `status/blocked` (external dependency); **Ice Box** maps to `status/ice-box` (shelved, not in the active queue). Roadmap Sync sets board Status from these labels and clears Focus Order when parking. Ice Box clears Start/Target dates; Blocked preserves dates if work had started. Parent Feature/Epic roll-up: In Progress beats Blocked; Blocked beats Ice Box; all open children Ice Box → parent Ice Box. Roadmap view filter includes Todo, Up Next, In Progress, Blocked, and Done; excludes Ice Box. Reject adding Up Next as an issue label or leaving blocked/ice-box issues on Todo because sync does not infer parked states without labels.

---

### DEC-029: Cross-Repo PM Workflow board selection and local settings

**Status:** Active  
**Date:** 2026-08-18  
**Related:** [DEC-018](#dec-018-github-api-response-caching-in-infrastructure), [DEC-027](#dec-027-post-10-milestone-and-work-item-hierarchy), [`plan/GITHUB_PROJECTS_V2_ACCESS.md`](GITHUB_PROJECTS_V2_ACCESS.md)  
**Summary:** Cross-Repo PM Workflow ([#272](https://github.com/markheydon/solo-dev-board/issues/272)) uses a **user-selected Projects v2 board** as the planning board (discovered the same way as Triage/Board Rules). Do not hard-code SoloDevBoard Roadmap (Project #8) field ids in application code. PM settings (selected board node id, excluded `owner/name` repositories, capacity limit, stall and neglect day thresholds) persist in **browser localStorage**, following the theme-preference pattern, because the solution has no application database. Board-state counts include all cards on the selected board; Daily Focus recommendations, Backlog Review, Planning candidates, and per-repo summaries honour exclusions. Hosted GitHub App sign-in still cannot read some private user-owned boards ([#293](https://github.com/markheydon/solo-dev-board/issues/293)); reuse the existing inaccessible-board warning. Reject storing PATs in localStorage, treating Project #8 as the only supported board, or adding EF Core solely for these preferences.

**Future:** When an Aspire-provisioned backing store exists for first-class product data (see [`plan/ASPIRE_MULTI_PROCESS_FINDINGS.md`](ASPIRE_MULTI_PROCESS_FINDINGS.md) §3.4), migrate PM settings off `localStorage` per [#391](https://github.com/markheydon/solo-dev-board/issues/391). `localStorage` remains acceptable until that gate is met.

---

### DEC-030: Self-documenting CI workflow split

**Status:** Active  
**Date:** 2026-08-19  
**Related:** [DEC-016](#dec-016-formalised-testing-standard--xunit-v3-nsubstitute-playwright-e2e), [DEC-021](#dec-021-two-tier-cd-pipeline)  
**Summary:** Quality gates use one workflow file per concern with a self-documenting name. `ci.yml` runs .NET restore, format, build, and test only. `playwright.yml` follows the official Playwright GitHub Actions template (matrix for PAT and hosted auth modes, `npx playwright install chromium`, HTML report artefact upload). The official template uses `npx playwright install --with-deps` for all browsers; this repository omits `--with-deps` on GitHub-hosted runners because `apt` can hang during install. The Blazor app is started by Playwright `webServer` in `tests/E2E/playwright.config.ts` so local `npm test` matches CI. Other gates use `{subject}-validate.yml` (for example `bash-validate.yml`, `powershell-validate.yml`, `github-actions-validate.yml`, `github-scripts-validate.yml`, `hugo-validate.yml`). Reject monolithic CI files that mix unrelated jobs or opaque workflow names such as `automation-lint.yml` or `shell.yml`.

---

## Superseded legacy (archive only)

| Legacy ADR | Superseded by | Notes |
|------------|---------------|-------|
| [ADR-0002](../adr/archive/0002-testing-framework.md) | DEC-016, DEC-006 | Testing stack standardised on xUnit v3 and NSubstitute |
| DEC-004 | DEC-016 | Moq replaced by NSubstitute per formalised testing standard |
| [ADR-0003](../adr/archive/0003-bicep-infrastructure.md) | DEC-015 | Hand-maintained Bicep replaced by Aspire-generated deployment |
| [ADR-0009](../adr/archive/0009-fluent-ui-blazor-component-library.md) | DEC-009 | Fluent UI Blazor replaced by MudBlazor |
