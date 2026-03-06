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
- [x] As a solo developer, I want the application to authenticate with GitHub using my Personal Access Token so that I can access my repositories. _(#6 done — merged PR #13, 2026-03-06; #7 status/todo)_
- [ ] As a solo developer, I want service interfaces to resolve user identity via an `ICurrentUserContext` abstraction so that the application can support multiple users in the future without structural rework. _(Phase 2 enabler — see ADR-0007)_
- [ ] As a solo developer, I want to authenticate with a GitHub App so that I can use fine-grained permissions without a long-lived PAT. _(Phase 5 — see ADR-0007)_
- [ ] As a solo developer, I want to see a list of all my GitHub repositories so that I can select which ones to manage. _(planned: #8 — status/todo)_
- [ ] As a solo developer, I want my GitHub token to be stored securely in Azure Key Vault so that it is never exposed in configuration files.
- [ ] As a solo developer, I want the application to be deployed to Azure App Service so that I can access it from any device.

---

## Epic 1: Audit Dashboard

Labels: `type/epic`, `area/dashboard`

- [ ] As a solo developer, I want to see a summary of open issues across all my repositories so that I can quickly identify where work is needed.
- [ ] As a solo developer, I want to see all open pull requests across my repositories so that I can track code review activity.
- [ ] As a solo developer, I want to see which issues have no labels so that I can identify items that need triage.
- [ ] As a solo developer, I want to see which pull requests are stale (no activity in the last 14 days) so that I can follow them up.
- [ ] As a solo developer, I want to see the status of GitHub Actions workflows across my repositories so that I can quickly spot build failures.
- [ ] As a solo developer, I want to filter the Audit Dashboard by repository so that I can focus on one project at a time.
- [ ] As a solo developer, I want the Audit Dashboard to refresh automatically so that I always see current data.
- [ ] As a solo developer, I want to export an audit summary as a Markdown report so that I can paste it into a planning document.

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

- [ ] As a solo developer, I want to see all labels across my selected repositories in a single list so that I can identify inconsistencies.
- [ ] As a solo developer, I want to create a new label and push it to multiple repositories at once so that I can maintain a consistent taxonomy efficiently.
- [ ] As a solo developer, I want to rename a label across multiple repositories simultaneously so that I can refactor my taxonomy without visiting each repo.
- [ ] As a solo developer, I want to change a label's colour across multiple repositories so that visual consistency is maintained.
- [ ] As a solo developer, I want to delete a label from multiple repositories at once so that I can clean up obsolete labels.
- [ ] As a solo developer, I want to apply the SoloDevBoard recommended label taxonomy (from `plan/LABEL_STRATEGY.md`) to any repository so that I can start with a sensible default set of labels.
- [ ] As a solo developer, I want to see which repositories do not have a specific label so that I can identify gaps.
- [ ] As a solo developer, I want to synchronise a target repository's labels to exactly match a source repository's labels so that they stay in lockstep.

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

## Infrastructure & Cross-Cutting

Labels: `type/chore`, `area/infrastructure`

- [ ] Set up Bicep infrastructure for Azure App Service, Key Vault, and managed identity.
- [ ] Configure OIDC authentication for GitHub Actions to Azure (no long-lived credentials).
- [ ] Implement response caching for GitHub API calls to respect rate limits.
- [ ] Add health check endpoints for Azure App Service monitoring.
- [ ] Configure logging with structured output (Azure Application Insights integration).
- [ ] Set up Dependabot for automated dependency updates.
