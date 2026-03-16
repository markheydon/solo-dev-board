# SoloDevBoard — Feature Backlog
<!-- AI Collaborator Instructions: When adding items to this backlog, follow the label strategy in LABEL_STRATEGY.md. Each user story should be formatted as "As a solo developer, I want [goal] so that [benefit]." New items should be added to the relevant epic. When an item is implemented, move it to the Done section of the relevant epic or tick it off. -->

This backlog is organised by the six core features (epics) of SoloDevBoard. For the phased implementation plan, see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

---

## Foundation (Cross-Cutting)

<!-- Production-Readiness Authentication and Admission Control -->
<!-- Story #117: Restrict hosted sign-in to authorised users -->

Labels: `type/epic`, `area/infrastructure`

<!-- Epic #4: Phase 1 — Foundation Completion (status/todo, milestone: v0.1.0) -->
<!-- Feature #5: GitHub API Integration with PAT Authentication -->


### MudBlazor Migration (Infrastructure Chore)

<!-- Planned 2026-03-09 per ADR-0012 — replaces Fluent UI Blazor with MudBlazor across the full application -->
<!-- Epic #63; area/infrastructure, priority/high, milestone v0.2.0 -->
<!-- Enablers: #64 NuGet swap + service wiring, #65 GitHub AI assets -->
<!-- Stories: #66 Dashboard refactor, #67 Repositories refactor, #68 Label Manager refactor -->
<!-- Tests: #69 bUnit test updates -->

- [x] As a solo developer, I want the application switched from Fluent UI Blazor to MudBlazor so that UI delivery by AI agents is more reliable and requires fewer workarounds. _(ADR-0012, Epic #63 — done, 2026-03-09)_

---

## Epic 101: Public-Release Authentication and Admission Control

Labels: `type/epic`, `area/infrastructure`, `priority/high`

<!-- Feature #103: GitHub App-first hosted authentication and admission control -->
<!-- Enabler #111: Installation and token lifecycle handling -->
<!-- Story #112: GitHub App user sign-in and per-request user context -->
<!-- Story #123: Integrate hosted sign-in gateway with /auth/sign-in -->
<!-- Enabler #113: Remove separate OAuth App dependency where feasible -->
<!-- Test #114: Authentication coverage for GitHub App-first hosted auth -->
<!-- Story #117: Map authenticated GitHub identity to hosted admission control -->
<!-- Chore #118: Define migration path from superseded hybrid auth plan -->
<!-- Docs #119: Update documentation for GitHub App-first hosted auth -->


- [x] Enabler: Installation and token lifecycle handling implemented for hosted authentication. _(Issue #111 — implemented on 2026-03-13; see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md.)_
- [x] Story: GitHub App user sign-in and per-request user context delivered for hosted mode. _(Issue #112 — implemented on 2026-03-13; see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md.)_
- [x] Story: Integrate the hosted sign-in handshake and session/callback boundary at `/auth/sign-in` so GitHub App-first hosted authentication can complete before admission control and data loading. _(Issue #123 — implemented on 2026-03-16; see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md; closes the `/auth/sign-in` planning gap and unblocks #114 and #119; see ADR-0015.)_
- [x] Story: Authenticated GitHub identities are now mapped to operator-managed user and organisation allow-lists for hosted admission control. Hosted access is deny-by-default and only permitted for identities explicitly listed. _(Issue #117 — implemented on 2026-03-16; see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md; see ADR-0014, ADR-0015.)_
- [x] Story: The separate OAuth App dependency is now demoted to an explicit fallback path, disabled by default, while preserving PAT-only local trusted mode and GitHub App-first hosted sign-in. _(Issue #113 — implemented on 2026-03-16; see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md; see ADR-0014, ADR-0015.)_
- [ ] As a solo developer, I want PAT-only local trusted mode preserved so that local development and trusted self-hosted use do not depend on hosted sign-in infrastructure.
- [x] Chore: As a SoloDevBoard maintainer, I want the hosted-auth branch and migration strategy locked before Feature #103 coding continues so that superseded hybrid work cannot drift into the GitHub App-first delivery path. _(Issue #118 — done, 2026-03-13; see plan/HOSTED_AUTH_MIGRATION_STRATEGY.md.)_

Labels: `type/epic`, `area/dashboard`

<!-- Feature #40: Audit Dashboard — DONE 2026-03-11 (closed via PR merge, all tests complete) -->
<!-- Enablers: #41 WorkflowRun domain + IGitHubService workflow runs (done — 2026-03-10), #42 Full AuditDashboardService (done — 2026-03-10) -->
<!-- Stories: #43 Open issues + PR summary (done — 2026-03-10), #44 Health indicators (done — 2026-03-10), #45 Filter by repository (done — 2026-03-10) -->
<!-- Tests: #46 AuditDashboardService unit tests (done — 2026-03-11), #47 GitHubService workflow run tests (done — 2026-03-11), #48 bUnit UI tests (done — 2026-03-11) -->

- [x] As a solo developer, I want to see a summary of open issues across all my repositories so that I can quickly identify where work is needed. _(#43 status/done — v0.2.0)_
- [x] As a solo developer, I want to see all open pull requests across my repositories so that I can track code review activity. _(#43 status/done — v0.2.0)_
- [x] As a solo developer, I want to see which issues have no labels so that I can identify items that need triage. _(#44 status/done — v0.2.0)_
- [x] As a solo developer, I want to see which pull requests are stale (no activity in the last 14 days) so that I can follow them up. _(#44 status/done — v0.2.0)_
- [x] As a solo developer, I want to see the status of GitHub Actions workflows across my repositories so that I can quickly spot build failures. _(#44 status/done — v0.2.0)_
- [x] As a solo developer, I want to filter the Audit Dashboard by repository so that I can focus on one project at a time. _(#44 status/done — v0.2.0; selection-first multi-select repository filter with shared RepositorySelector component; #45 merged into this story)_
- [x] Test: Unit tests for `AuditDashboardService` covering all public methods, empty-list paths, and stale/failing-workflow edge cases. _(#46 — done, 2026-03-11)_
- [x] Test: Unit tests for `GitHubService.GetWorkflowRunsAsync` — field mapping, empty response, guard clauses, and non-success HTTP error handling. _(#47 — done, 2026-03-11)_
- [x] Test: bUnit component tests for the Audit Dashboard page — loading state, empty state, summary table, health indicator sections, zero-state messages, and repository filter interaction. _(#48 — done, 2026-03-11)_
- [ ] As a solo developer, I want the Audit Dashboard to refresh automatically so that I always see current data.
- [ ] As a solo developer, I want to export an audit summary as a Markdown report so that I can paste it into a planning document.
- [ ] As a solo developer, I want to see my project board state at a glance (item count per status column and current active load) so that I can assess capacity before committing to more work.
- [ ] As a solo developer, I want to see which repositories have had no open issue or PR activity in the last 14 days so that neglected repos are surfaced rather than silently forgotten.
- [ ] As a solo developer, I want to see open issues and pull requests flagged as `priority-high` across all repositories so that urgent work is never buried in per-repository noise.

---

## Epic 2: One-Click Migration

Labels: `type/epic`, `area/migration`

<!-- Parent Epic #87: Phase 3 — One-Click Migration + Triage UI (planned 2026-03-12, milestone v0.3.0). -->
<!-- Feature #88: One-Click Migration (planned 2026-03-12, milestone v0.3.0; first delivery slice covers labels + milestones only). -->
<!-- Enabler: #89 Introduce migration contracts and milestone migration support. -->
<!-- Stories: #90 Source/target selection, #91 Conflict resolution, #92 Preview, #93 Label migration, #94 Milestone migration, #95 Post-migration summary. -->
<!-- Tests: #96 Migration orchestration unit tests, #97 GitHub milestone infrastructure tests, #98 Migration page bUnit tests. -->

- [x] As a solo developer, I want to select a source repository and a target repository for a migration so that I can copy configurations between them. _(Completed as #90 for v0.3.0. Source/target repository selection UI delivered; source cannot also be a target; preview blocked until valid selection is made.)_
- [x] As a solo developer, I want to see a preview (diff) of the changes that will be made before applying a migration so that I can avoid unintended overwrites. _(Completed as #92 for v0.3.0. Preview grouped by target repository and artefact type; label and milestone detail tables shown; apply button hidden when no actionable changes.)_
- [x] As a solo developer, I want to migrate all labels from a source repository to a target repository so that both repositories share the same label taxonomy. _(Completed as #93 for v0.3.0. Label migration create/update/skip/delete delivered per conflict strategy; partial failures reported per target.)_
- [x] As a solo developer, I want to migrate all milestones from a source repository to a target repository so that I can maintain consistent milestone structures. _(Completed as #94 for v0.3.0. Milestone migration create/update/skip/delete delivered; title, description, state, and due date included; GitHub 422 due_on null bug fixed.)_
- [x] As a solo developer, I want to choose a conflict resolution strategy (skip, overwrite, or merge) so that I have control over how existing items in the target are handled. _(Completed as #91 for v0.3.0. Conflict strategy selection delivered; includes explanatory text and overwrite warning.)_
- [x] Enabler: Introduce migration contracts and milestone migration support. _(Completed as #89 for v0.3.0. Migration contracts and milestone migration support delivered.)_
- [x] As a solo developer, I want to see a post-migration summary so that I know exactly what was created, updated, or skipped. _(Completed as #95 for v0.3.0. Per-target summary view delivered: created/updated/deleted/skipped counts shown for labels and milestones; partial failures reported per target.)_
- [x] Test: Unit tests for migration orchestration and milestone services. _(Completed as #96 for v0.3.0. Coverage added for guard clauses, multi-target aggregation, conflict handling, and partial failure behaviour.)_
- [x] Test: Infrastructure tests for GitHub milestone migration support. _(Completed as #97 for v0.3.0. Coverage added for response mapping, create/update request behaviour, non-success responses, and guard clauses.)_
- [x] Test: bUnit tests for the Migration page workflow. _(Completed as #98 for v0.3.0. Coverage added for empty state, selection and preview gating, duplicate apply prevention, and post-migration summary error rendering.)_
- [ ] As a solo developer, I want to migrate project board column configurations so that new repositories start with the same board structure. _(Deferred follow-on slice; requires GitHub Projects v2 support and is captured in ADR-0013.)_

---

## Epic 4: Board Rules Visualiser

Labels: `type/epic`, `area/board-rules`

- [ ] As a solo developer, I want to see all GitHub project boards for a selected repository so that I can choose which one to visualise.
- [ ] As a solo developer, I want to see an interactive diagram of a project board's columns and automation rules so that I can understand how issues flow.
- [ ] As a solo developer, I want to click on a rule in the diagram to see its full configuration so that I can understand what triggers it.
- [ ] As a solo developer, I want to see highlighted rules that may conflict or produce unexpected behaviour so that I can diagnose automation issues.
- [ ] As a solo developer, I want to compare the board rules of two repositories so that I can identify differences before a migration.

---

## Epic 5: Triage UI

Labels: `type/epic`, `area/triage`

- [ ] As a solo developer, I want to start a triage session that presents unlabelled issues one at a time so that I can work through them efficiently.
- [ ] As a solo developer, I want to apply labels to an issue with a single click or keyboard shortcut so that triage is fast.
- [ ] As a solo developer, I want to assign a milestone to an issue without leaving the triage view so that I can organise my work in context.
- [ ] As a solo developer, I want to add an issue to a GitHub project board column without leaving the triage view so that planning is seamless.
- [ ] As a solo developer, I want to mark an issue as a duplicate and close it with a reference so that duplicate tracking is clean.
- [ ] As a solo developer, I want to skip an issue and return to it later so that I am not blocked by difficult triage decisions.
- [ ] As a solo developer, I want to see a progress indicator during a triage session so that I know how many issues remain.
- [ ] As a solo developer, I want to see a summary at the end of a triage session showing all actions taken so that I have a record of what was done.
- [ ] As a solo developer, I want to triage unlabelled pull requests alongside issues so that my entire GitHub workload, including open PRs, is managed in one place.

---

## Epic 6: Workflow Templates

Labels: `type/epic`, `area/workflows`

- [ ] As a solo developer, I want to browse a library of GitHub Actions workflow templates so that I can find one suitable for my project.
- [ ] As a solo developer, I want to customise a template's parameters before applying it so that it is configured correctly for my repository.
- [ ] As a solo developer, I want to apply a workflow template to one or more repositories at once so that I can roll out CI/CD configurations efficiently.
- [ ] As a solo developer, I want to see which repositories already have a particular workflow template applied so that I have a clear overview of my CI/CD coverage.
- [ ] As a solo developer, I want to be alerted when a repository's workflow file differs from the canonical template so that I can update it.
- [ ] As a solo developer, I want built-in templates for .NET CI, Azure CD, and Dependabot so that I have useful starting points out of the box.
- [ ] As a solo developer, I want to define my own custom template repositories so that I can share templates across my own projects.

---

## Epic 7: Cross-Repo PM Workflow

Labels: `type/epic`, `area/dashboard`

> This epic represents the long-term destination of SoloDevBoard: a UI-based implementation of the two-mode PM operating system established in the companion [markheydon/github-workflows](https://github.com/markheydon/github-workflows) repository. That system uses AI agents and prompts to manage cross-repo workloads; SoloDevBoard replaces those text-based prompts with a proper visual interface, removing the dependency on VS Code and Copilot for day-to-day PM operations.

### Daily Focus

- [ ] As a solo developer, I want a Daily Focus view that shows my project board state (item count per status column and active load) so that I have an instant overview when I start my working day.
- [ ] As a solo developer, I want to see the top three unblocked work items I should focus on today, ranked by priority across all my repositories, so that I can start working without spending time deciding what to pick up.
- [ ] As a solo developer, I want to be alerted when items have been in my Up Next board column for three or more days without progressing so that I address stalling before it accumulates.
- [ ] As a solo developer, I want to see pull requests that have been in an In Review state for three or more days so that I can make a timely merge, close, or return-to-progress decision.

### Backlog Review

- [ ] As a solo developer, I want a cross-repository backlog view that groups work by priority (urgent, ready to start, blocked, deferred) so that I can see my full workload in one place without jumping between repositories.
- [ ] As a solo developer, I want to see open issues and pull requests with no core label flagged prominently so that items needing triage are never missed.
- [ ] As a solo developer, I want to identify epics that are close to completion (all child stories closed) so that I can close them and keep my backlog clean.
- [ ] As a solo developer, I want the backlog view to distinguish between issues and open pull requests so that I can see both work-to-do and work-awaiting-review at a glance.

### Iteration Planning

- [ ] As a solo developer, I want to select issues and pull requests from across my repositories and move them to my Up Next board column in a single planning session so that my work queue is populated quickly and intentionally.
- [ ] As a solo developer, I want the planning view to show my current active load and warn me when I exceed a configurable capacity limit so that I avoid over-committing.
- [ ] As a solo developer, I want to resolve stalled Up Next items (move to Ice Box, mark as Blocked, or re-commit) before adding new work so that my board remains an accurate picture of intent at all times.
- [ ] As a solo developer, I want to optionally assign a milestone to planned items in a single step so that iteration scoping is part of the planning flow rather than a separate task.

### Repo Management

- [ ] As a solo developer, I want to define a list of excluded repositories so that personal, archived, or irrelevant repos are not surfaced in cross-repo PM operations.
- [ ] As a solo developer, I want to see a summary of issue and PR counts per repository so that I can quickly identify where work is concentrated.

---

## Infrastructure & Cross-Cutting

Labels: `type/chore`, `area/infrastructure`

<!-- ADR-0011 IRepositoryService alignment (planned 2026-03-10, milestone v0.2.0) -->
<!-- Enabler: #54 Introduce RepositoryDto; migrate IRepositoryService to DTO boundary (done — 2026-03-10) -->
<!-- Test: #55 Verify RepositoryDto boundary compliance — RepositoryService mapping tests + bUnit consumer updates (done — 2026-03-10) -->

- [x] Enabler: Align `IRepositoryService` with ADR-0011 by introducing `RepositoryDto` as the Application→App boundary shape for repository operations; map domain `Repository` → `RepositoryDto` inside `RepositoryService`. _(#54 — done, 2026-03-10)_
- [x] Test: Verify `RepositoryDto` boundary compliance — unit tests for `RepositoryService` field-level DTO mapping and updated bUnit tests for `Labels` and `Repositories` components. _(#55 — done, 2026-03-10)_

<!-- Feature #56: Application Version Centralisation + About Page (planned 2026-03-08, milestone v0.2.0) -->
<!-- Enablers: #57 Centralise version in Directory.Build.props + IAppVersionService, #58 Dynamic user-agent from IAppVersionService -->
<!-- Story: #59 About page showing app version information -->
<!-- Tests: #60 AppVersionService unit tests (done — 2026-03-10), #61 About page bUnit component tests (done — 2026-03-10) -->

- [x] As a solo developer, I want the application version to be declared in a single place so that all version references (user-agent, About page, build artefacts) remain consistent automatically. _(#57 — done, 2026-03-10)_
- [x] As a solo developer, I want the GitHub API user-agent string to reflect the running application version so that it never drifts from the actual release. _(#58 — done, 2026-03-10)_
- [x] As a solo developer, I want an About page showing the application version and .NET runtime version so that I always know which version I am running. _(#59 — done, 2026-03-10)_


<!-- Production-Readiness Batch: Planned 2026-03-12 for v1.0.0 (Epic #101: Public Release Infrastructure and Authentication) -->
<!-- Feature #102: Operational hardening for public release -->
<!-- Feature #103: GitHub App and OAuth authentication for public release -->
<!-- Enabler #104: Complete Azure infrastructure baseline for public release -->
<!-- Enabler #105: Configure GitHub Actions OIDC deployment to Azure -->
<!-- Story #106: Add App Service health checks and monitoring endpoints -->
<!-- Enabler #107: Add structured logging and Application Insights telemetry -->
<!-- Story #108: Implement GitHub API response caching -->
<!-- Chore #109: Configure Dependabot for automated dependency updates -->
<!-- Test #110: Add operational hardening coverage -->
<!-- Enabler #111: Implement GitHub App authentication and installation token flow -->
<!-- Story #112: Add GitHub OAuth sign-in and per-request user context -->
<!-- Enabler #113: Persist per-user authentication secrets and token references securely -->
<!-- Test #114: Add authentication coverage for GitHub App and OAuth flow -->
<!-- Story #117: Restrict hosted sign-in to authorised users -->

- [x] Set up Bicep infrastructure for Azure App Service, Key Vault, and managed identity. _(done — Bicep baseline complete with managed identity RBAC, configurable Key Vault secret name, purge protection, health-check path, and optional CIDR inbound access restrictions (SKU-gated; unsupported on F1); guided PowerShell deployment script `infra/Deploy-SoloDevBoardInfra.ps1` added with auto-detected caller IP defaulting, F1 compatibility guard, and post-deployment next-step guidance; see issue #104.)_
- [x] Configure OIDC authentication for GitHub Actions to Azure (no long-lived credentials). _(done — `cd.yml` already uses OIDC with least-privilege permissions; OIDC trust prerequisites and protected-environment documentation added to `infra/README.md`; see issue #105.)_
- [ ] Implement response caching for GitHub API calls to respect rate limits. _(#108)_
- [ ] Add health check endpoints for Azure App Service monitoring. _(#106)_
- [ ] Configure logging with structured output (Azure Application Insights integration). _(#107)_
- [x] Set up Dependabot for automated dependency updates. _(done — `dependabot.yml` and auto-merge workflow added previously; see issue #109.)_

 
