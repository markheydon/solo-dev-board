# SoloDevBoard — Feature Backlog

<!-- AI Collaborator Instructions: When adding items to this backlog, follow the label strategy in LABEL_STRATEGY.md. Each user story should be formatted as "As a solo developer, I want [goal] so that [benefit]." New items should be added to the relevant epic. When an item is implemented, move it to the Done section of the relevant epic or tick it off. -->

This backlog is organised by the six core features (epics) of SoloDevBoard. For the phased implementation plan, see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

---

## Foundation (Cross-Cutting)

Labels: `type/epic`, `area/infrastructure`

<!-- Epic #4: Phase 1 — Foundation Completion (status/todo, milestone: v0.1.0) -->
<!-- Feature #5: GitHub API Integration with PAT Authentication -->

- [x] Scaffold solution structure (`App`, `Application`, `Domain`, `Infrastructure` projects) — _done_
- [x] Configure nullable reference types, implicit usings, and coding conventions — _done_
- [x] Set up xUnit test projects — _done_
- [x] Set up CI workflow (`.github/workflows/ci.yml`) — _done_
- [x] As a solo developer, I want the application to authenticate with GitHub using my Personal Access Token so that I can access my repositories. _(#6 done — merged PR #13, 2026-03-06; #7 done — merged PR #17, 2026-03-06)_
- [x] As a solo developer, I want service interfaces to resolve user identity via an `ICurrentUserContext` abstraction so that the application can support multiple users in the future without structural rework. _(Phase 2 enabler — see ADR-0007 — **done: Feature #21, Enablers #22 #23 #24, Test #25, milestone v0.2.0, PR #26**)_
- [ ] As a solo developer, I want to authenticate with a GitHub App so that I can use fine-grained permissions without a long-lived PAT. _(Phase 5 — see ADR-0007)_
- [x] As a solo developer, I want to see a list of all my GitHub repositories so that I can select which ones to manage. _(issue #8 done — merged PR #18, 2026-03-07)_
- [x] As a solo developer, I want an empty dashboard shell page with navigation cards for each feature so that the application has a clear entry point. _(issue #9 done — merged PR #19, 2026-03-07)_
- [x] As a solo developer, I want comprehensive unit tests for `GitHubService` and `GitHubAuthHandler` so that infrastructure reliability is verified. _(issue #10 done — merged to main, 2026-03-07)_
- [x] As a solo developer, I want Blazor component tests using bUnit so that UI behaviour is verified automatically without a browser. _(issue #11 done — merged PR #19, 2026-03-07)_
- [ ] As a solo developer, I want my GitHub token to be stored securely in Azure Key Vault so that it is never exposed in configuration files.
- [ ] As a solo developer, I want the application to be deployed to Azure App Service so that I can access it from any device.

---

## Epic 1: Audit Dashboard

Labels: `type/epic`, `area/dashboard`

<!-- Feature #40: Audit Dashboard (planned 2026-03-08, milestone v0.2.0) -->
<!-- Enablers: #41 WorkflowRun domain + IGitHubService workflow runs, #42 Full AuditDashboardService -->
<!-- Stories: #43 Open issues + PR summary, #44 Health indicators, #45 Filter by repository -->
<!-- Tests: #46 AuditDashboardService unit tests, #47 GitHubService workflow run tests, #48 bUnit UI tests -->

- [ ] As a solo developer, I want to see a summary of open issues across all my repositories so that I can quickly identify where work is needed. _(#43 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to see all open pull requests across my repositories so that I can track code review activity. _(#43 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to see which issues have no labels so that I can identify items that need triage. _(#44 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to see which pull requests are stale (no activity in the last 14 days) so that I can follow them up. _(#44 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to see the status of GitHub Actions workflows across my repositories so that I can quickly spot build failures. _(#44 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to filter the Audit Dashboard by repository so that I can focus on one project at a time. _(#45 status/todo — v0.2.0)_
- [ ] As a solo developer, I want the Audit Dashboard to refresh automatically so that I always see current data.
- [ ] As a solo developer, I want to export an audit summary as a Markdown report so that I can paste it into a planning document.
- [ ] As a solo developer, I want to see my project board state at a glance (item count per status column and current active load) so that I can assess capacity before committing to more work.
- [ ] As a solo developer, I want to see which repositories have had no open issue or PR activity in the last 14 days so that neglected repos are surfaced rather than silently forgotten.
- [ ] As a solo developer, I want to see open issues and pull requests flagged as `priority-high` across all repositories so that urgent work is never buried in per-repository noise.

---

## Epic 2: One-Click Migration

Labels: `type/epic`, `area/migration`

- [ ] As a solo developer, I want to select a source repository and a target repository for a migration so that I can copy configurations between them.
- [ ] As a solo developer, I want to see a preview (diff) of the changes that will be made before applying a migration so that I can avoid unintended overwrites.
- [ ] As a solo developer, I want to migrate all labels from a source repository to a target repository so that both repositories share the same label taxonomy.
- [ ] As a solo developer, I want to migrate all milestones from a source repository to a target repository so that I can maintain consistent milestone structures.
- [ ] As a solo developer, I want to choose a conflict resolution strategy (skip, overwrite, or merge) so that I have control over how existing items in the target are handled.
- [ ] As a solo developer, I want to see a post-migration summary so that I know exactly what was created, updated, or skipped.
- [ ] As a solo developer, I want to migrate project board column configurations so that new repositories start with the same board structure.

---

## Epic 3: Label Manager

Labels: `type/epic`, `area/labels`

<!-- Feature #27: Label Manager (planned 2026-03-07, milestone v0.2.0) -->
<!-- Enablers: #28 Label domain + ILabelRepository (done — PR #39, 2026-03-08), #29 GitHubLabelRepository, #31 LabelService, #49 ADR-0011 retrospective — LabelDto + ILabelManagerService update (in-progress, 2026-03-08) -->
<!-- Stories: #35 View labels, #33 CRUD labels, #32 Synchronise labels, #34 Apply taxonomy -->
<!-- Tests: #38 LabelService unit tests, #37 GitHubLabelRepository unit tests, #36 bUnit UI tests -->

- [ ] As a solo developer, I want to see all labels across my selected repositories in a single list so that I can identify inconsistencies. _(#35 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to create a new label and push it to multiple repositories at once so that I can maintain a consistent taxonomy efficiently. _(#33 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to rename a label across multiple repositories simultaneously so that I can refactor my taxonomy without visiting each repo. _(#33 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to change a label's colour across multiple repositories so that visual consistency is maintained. _(#33 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to delete a label from multiple repositories at once so that I can clean up obsolete labels. _(#33 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to apply the SoloDevBoard recommended label taxonomy (from `plan/LABEL_STRATEGY.md`) to any repository so that I can start with a sensible default set of labels. _(#34 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to see which repositories do not have a specific label so that I can identify gaps. _(#35 status/todo — v0.2.0)_
- [ ] As a solo developer, I want to synchronise a target repository's labels to exactly match a source repository's labels so that they stay in lockstep. _(#32 status/todo — v0.2.0)_

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

- [ ] Set up Bicep infrastructure for Azure App Service, Key Vault, and managed identity.
- [ ] Configure OIDC authentication for GitHub Actions to Azure (no long-lived credentials).
- [ ] Implement response caching for GitHub API calls to respect rate limits.
- [ ] Add health check endpoints for Azure App Service monitoring.
- [ ] Configure logging with structured output (Azure Application Insights integration).
- [ ] Set up Dependabot for automated dependency updates.
