# SoloDevBoard — Feature Backlog

<!-- AI Collaborator Instructions: When adding items to this backlog, follow the label strategy in LABEL_STRATEGY.md. Each user story should be formatted as "As a solo developer, I want [goal] so that [benefit]." New items should be added to the relevant epic. When an item is implemented, move it to the Done section of the relevant epic or tick it off. -->

This backlog is organised by the six core features (epics) of SoloDevBoard. For the phased implementation plan, see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

---

## Foundation (Cross-Cutting)

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
  - [x] Enabler: Remove Fluent UI NuGet packages; add MudBlazor; wire up services in `Program.cs` and providers in `MainLayout.razor`. _(#64 — done, merged PR #71, 2026-03-09)_
  - [x] Enabler: Verify/complete `.github/skills/mudblazor/` skill and `.github/instructions/blazor.instructions.md` replacement. _(#65 — done, merged PR #71, 2026-03-09)_
  - [x] Story: Refactor the Dashboard shell page (Fluent UI layout and navigation → MudBlazor equivalents). _(#66 — done, merged PR #72, 2026-03-09)_
  - [x] Story: Refactor the Repositories page (Fluent UI → MudBlazor components). _(#67 — done, merged PR #72, 2026-03-09)_
  - [x] Story: Refactor the Label Manager page and Label Operation dialog (Fluent UI → MudBlazor; replace hand-rolled colour picker with `MudColorPicker`). _(#68 — done, merged PR #73, 2026-03-09)_
  - [x] Test: Update bUnit test projects — replace `AddFluentUIComponents()` with MudBlazor service registration. _(#69 — done, merged PR #74, 2026-03-09)_

---

## Epic 1: Audit Dashboard

Labels: `type/epic`, `area/dashboard`

<!-- Feature #40: Audit Dashboard (planned 2026-03-08, milestone v0.2.0) -->
<!-- Enablers: #41 WorkflowRun domain + IGitHubService workflow runs (done — 2026-03-10), #42 Full AuditDashboardService (done — 2026-03-10) -->
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

<!-- Feature #27: Label Manager — DONE 2026-03-10 (closed via PR #81 merge) -->
<!-- Enablers: #28 Label domain + ILabelRepository (done — PR #39, 2026-03-08), #29 GitHubLabelRepository (done — 2026-03-08), #31 LabelService (done — 2026-03-08), #49 ADR-0011 retrospective — LabelDto + ILabelManagerService update (done — 2026-03-08) -->
<!-- Stories: #35 View labels (done), #33 CRUD labels (done), #32 Synchronise labels (done), #34 Apply taxonomy (done) -->
<!-- Tests: #38 LabelService unit tests (done), #37 GitHubLabelRepository unit tests (done), #36 bUnit UI tests (done) -->
- [x] Test: Unit tests for LabelService (#38) — completed.
  - All CRUD, synchronisation (diff/empty diff), validation, and partial failure scenarios covered.
  - Local build and test suite passing.
- [x] As a solo developer, I want to see all labels across my selected repositories in a single list so that I can identify inconsistencies. _(#35 done — 2026-03-08)_
- [x] As a solo developer, I want to create a new label and push it to multiple repositories at once so that I can maintain a consistent taxonomy efficiently. _(#33 done — 2026-03-08)_
- [x] As a solo developer, I want to rename a label across multiple repositories simultaneously so that I can refactor my taxonomy without visiting each repo. _(#33 done — 2026-03-08)_
- [x] As a solo developer, I want to change a label's colour across multiple repositories so that visual consistency is maintained. _(#33 done — 2026-03-08)_
- [x] As a solo developer, I want to delete a label from multiple repositories at once so that I can clean up obsolete labels. _(#33 done — 2026-03-08)_
- [x] As a solo developer, I want to apply recommended label taxonomy strategies to any repository so that I can start with a sensible default set of labels. _(#34 done — 2026-03-10; includes SoloDevBoard + GitHub default strategy options, preview, confirm/cancel, and per-repository summary)_
- [x] As a solo developer, I want to see which repositories do not have a specific label so that I can identify gaps. _(#35 done — 2026-03-08)_
- [x] As a solo developer, I want to synchronise a target repository's labels to exactly match a source repository's labels so that they stay in lockstep. _(#32 done — 2026-03-10; supports preview-first flow, multi-target selection, per-target summary, partial failure reporting, and duplicate submission prevention)_
  - [x] Test: bUnit component tests for Label Manager UI completed. _(#36 done — 2026-03-10)_

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
<!-- Tests: #60 AppVersionService unit tests (done — 2026-03-10), #61 About page bUnit component tests -->

- [x] As a solo developer, I want the application version to be declared in a single place so that all version references (user-agent, About page, build artefacts) remain consistent automatically. _(#57 — done, 2026-03-10)_
- [x] As a solo developer, I want the GitHub API user-agent string to reflect the running application version so that it never drifts from the actual release. _(#58 — done, 2026-03-10)_
- [ ] As a solo developer, I want an About page showing the application version and .NET runtime version so that I always know which version I am running. _(#59 status/todo — v0.2.0)_
- [ ] Set up Bicep infrastructure for Azure App Service, Key Vault, and managed identity.
- [ ] Configure OIDC authentication for GitHub Actions to Azure (no long-lived credentials).
- [ ] Implement response caching for GitHub API calls to respect rate limits.
- [ ] Add health check endpoints for Azure App Service monitoring.
- [ ] Configure logging with structured output (Azure Application Insights integration).
- [ ] Set up Dependabot for automated dependency updates.
