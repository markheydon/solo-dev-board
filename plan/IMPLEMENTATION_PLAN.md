# SoloDevBoard — Implementation Plan

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file before making changes to this plan. -->

This document describes the phased implementation of SoloDevBoard. Each phase has a clear goal, a set of key tasks, and defined dependencies.

For the full feature scope, see [SCOPE.md](SCOPE.md). For individual feature backlogs, see [BACKLOG.md](BACKLOG.md).

All planned front-end delivery after ADR-0012 uses MudBlazor as the sole UI component library.

---

## Phase 1 — Foundation

**Goal:** Establish a working Blazor Server application with GitHub authentication, a basic repository listing, and an empty dashboard shell. This phase produces a deployable skeleton.

**Milestone:** v0.1.0

### Key Tasks

- [ ] Scaffold solution structure: `App`, `Application`, `Domain`, `Infrastructure` projects
- [ ] Configure nullable reference types, implicit usings, and coding conventions across all projects
- [ ] Implement GitHub Personal Access Token (PAT) authentication flow
- [ ] Implement GitHub App authentication flow (optional in this phase, required before v1.0)
- [ ] Implement `IGitHubService` interface in `Application` layer
- [ ] Implement `GitHubRestClient` in `Infrastructure` layer (using `Octokit` or `HttpClient` + `System.Text.Json`)
- [x] Implement basic repository listing: fetch and display all repositories the authenticated user has access to _(issue #8 done — 2026-03-07)_
- [ ] Create a basic MudBlazor dashboard shell page (empty panels for each of the 6 features)
- [ ] Configure `appsettings.json` and user secrets for local development
- [ ] Set up xUnit test projects; write smoke tests for `GitHubRestClient`
- [ ] Set up CI workflow (`.github/workflows/ci.yml`) — build and test on every PR
- [ ] Deploy to Azure App Service using Bicep (`infra/main.bicep`) — confirm the app runs in the cloud

### Dependencies

- GitHub PAT or GitHub App credentials for local development
- Azure subscription for deployment validation

---

## Phase 2 — Label Manager + Audit Dashboard

**Goal:** Deliver the first two user-facing features: the ability to manage labels across repositories, and an audit view of repository health.

**Milestone:** v0.2.0

### Key Tasks

#### Architecture Preparation (Multi-Tenancy Readiness)
- [x] Define `ICurrentUserContext` interface in `SoloDevBoard.Application` to represent the authenticated user's identity and API token (see ADR-0007)
- [x] Implement a single-user adapter for `ICurrentUserContext` in `SoloDevBoard.Infrastructure` (reads from `IOptions<GitHubAuthOptions>` — no behaviour change from Phase 1)
- [x] Refactor `IGitHubService` and all Application-layer services to inject and use `ICurrentUserContext` — no service may access `IOptions<GitHubAuthOptions>` directly

#### Label Manager
- [ ] Design `Label` domain record and `ILabelRepository` interface
- [ ] Implement `GitHubLabelRepository` in `Infrastructure`
- [ ] Implement `LabelService` in `Application` (CRUD, sync operations)
- [ ] Build MudBlazor UI components for the Label Manager
- [ ] Implement label synchronisation logic (compare source and target, produce diff, apply changes)
- [ ] Write unit tests for `LabelService` using Moq
- [ ] Write integration tests for `GitHubLabelRepository` (against GitHub API test org or mocked HTTP)
- [ ] Update `docs/user-guide/label-manager.md`

#### Audit Dashboard
- [ ] Design `AuditReport` domain record
- [ ] Implement `AuditService` in `Application` (aggregate data from multiple repositories)
- [ ] Build MudBlazor UI components for the Audit Dashboard
- [ ] Implement health indicators: unlabelled issues, stale PRs, failing workflows, label inconsistencies
- [ ] Write unit tests for `AuditService`
- [ ] Update `docs/user-guide/audit-dashboard.md`

### Dependencies

- Phase 1 complete (GitHub client, repository listing, authenticated session)

---

## Phase 3 — One-Click Migration + Triage UI

**Goal:** Allow users to migrate repository configuration (labels, milestones) and provide a streamlined issue triage experience.

**Milestone:** v0.3.0

### Key Tasks

#### One-Click Migration
- [ ] Design `MigrationPlan` and `MigrationResult` domain records
- [ ] Implement `MigrationService` in `Application` (diff, preview, apply)
- [ ] Build MudBlazor UI: source/target repository selection, diff preview, confirmation, summary
- [ ] Support migration of: labels, milestones (phase 1); project board columns (phase 2 of this feature)
- [ ] Write unit tests for `MigrationService`
- [ ] Update `docs/user-guide/one-click-migration.md`

#### Triage UI
- [ ] Design `TriageSession` and `TriageAction` domain records
- [ ] Implement `TriageService` in `Application`
- [ ] Build focused MudBlazor triage view with keyboard shortcut support
- [ ] Implement quick actions: label, assign milestone, add to project, close as duplicate
- [ ] Write unit tests for `TriageService`
- [ ] Update `docs/user-guide/triage-ui.md`

### Dependencies

- Phase 2 complete (Label Manager, GitHub label/milestone API integration)

---

## Phase 4 — Board Rules Visualiser + Workflow Templates

**Goal:** Deliver the remaining two features: a visual representation of project board automation rules, and a template library for GitHub Actions workflows.

**Milestone:** v0.4.0

### Key Tasks

#### Board Rules Visualiser
- [ ] Investigate GitHub Projects v2 GraphQL API for automation rule access
- [ ] Design `BoardRule` and `BoardDiagram` domain records
- [ ] Implement `BoardRuleService` in `Application`
- [ ] Implement GraphQL client in `Infrastructure` (see ADR-0005)
- [ ] Build interactive MudBlazor-based diagram component (consider using a JS interop charting library where needed)
- [ ] Write unit tests for `BoardRuleService`
- [ ] Update `docs/user-guide/board-rules-visualiser.md`

#### Workflow Templates
- [ ] Design `WorkflowTemplate` domain record
- [ ] Implement `WorkflowTemplateService` in `Application`
- [ ] Build MudBlazor UI: template browser, parameter editor, apply to repositories, staleness tracker
- [ ] Include built-in templates: CI (dotnet), CD (Azure App Service), Dependabot
- [ ] Write unit tests for `WorkflowTemplateService`
- [ ] Update `docs/user-guide/workflow-templates.md`

### Dependencies

- Phase 1 complete (GitHub GraphQL client infrastructure)
- Phase 2 complete (repository selection component)

---

## Phase 5 — Cross-Repo PM Workflow

**Goal:** Deliver Epic 7 — the UI-based implementation of the two-mode PM operating system from [markheydon/github-workflows](https://github.com/markheydon/github-workflows). This phase transforms SoloDevBoard from a collection of individual tools into a cohesive planning environment.

**Milestone:** v0.5.0

### Key Tasks

#### Daily Focus
- [ ] Design `DailyFocusReport` and `BoardSnapshot` domain records
- [ ] Implement `DailyFocusService` in `Application` (board state, stalled items, top-priority recommendations)
- [ ] Build MudBlazor Daily Focus view: board state summary, stalled item alerts, top-3 recommended work items
- [ ] Implement stalled item detection (Up Next for 3+ days; PRs In Review for 3+ days)
- [ ] Write unit tests for `DailyFocusService`
- [ ] Update `docs/user-guide/pm-workflow.md`

#### Backlog Review
- [ ] Implement `BacklogReviewService` in `Application` (cross-repo, priority-grouped, PR-aware)
- [ ] Build MudBlazor Backlog Review view: groups for urgent, ready, blocked, deferred; neglected repo alerts
- [ ] Implement neglected repo detection (no issue or PR activity in 14 days)
- [ ] Write unit tests for `BacklogReviewService`
- [ ] Update `docs/user-guide/pm-workflow.md`

#### Iteration Planning
- [ ] Design `IterationPlan` domain record
- [ ] Implement `IterationPlanningService` in `Application` (capacity enforcement, stale resolution, milestone assignment)
- [ ] Build MudBlazor Iteration Planning view: capacity indicator, stale item resolution, Up Next curation, optional milestone assignment
- [ ] Write unit tests for `IterationPlanningService`
- [ ] Update `docs/user-guide/pm-workflow.md`

#### Repo Management
- [ ] Implement excluded-repos configuration (persisted per user, applied to all cross-repo operations)
- [ ] Build MudBlazor settings UI for managing excluded repositories

### Dependencies

- Phases 1–4 complete (repository selection, label management, board API integration, GitHub Projects v2 GraphQL client)

---

## Phase 6 — Polish, Testing, and Azure Deployment


**Goal:** Achieve production quality for public release: comprehensive test coverage, operational hardening, secure authentication, observability, and a stable Azure deployment pipeline.

**Milestone:** v1.0.0

### Key Tasks
- [ ] Achieve ≥80% unit test coverage across `Application` and `Domain` projects.
- [ ] Perform accessibility audit of all Blazor components (WCAG 2.1 AA).
- [ ] Conduct performance review: identify and address slow GitHub API calls (caching, pagination).
- [ ] Complete Azure infrastructure baseline via Bicep (App Service, Key Vault, managed identity). _(#104)_
- [ ] Configure OIDC authentication for GitHub Actions deployment to Azure (no long-lived credentials). _(#105)_
- [ ] Add health check endpoints for Azure App Service monitoring. _(#106)_
- [ ] Implement response caching for GitHub API calls to respect rate limits. _(#108)_
- [ ] Configure structured logging and Application Insights telemetry. _(#107)_
- [ ] Set up Dependabot for automated dependency updates. _(#109)_
- [ ] Preserve PAT-only local trusted mode for development and trusted self-hosted use.
- [x] Implement hosted authentication session boundaries and per-request user context for GitHub App-first hosted mode. _(#103, #112; implemented on 2026-03-13, see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md.)_
- [x] Handle hosted installation context validation and token lifecycle checks (expiry and failure handling) for hosted requests. _(#103, #111; implemented on 2026-03-13, see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md.)_
- [ ] Restrict hosted access to operator-managed user and organisation allow-lists, with deny-by-default admission control. _(#103, #117; see ADR-0014 and ADR-0015)._
- [ ] Remove or demote the separate OAuth App dependency where GitHub App user authentication satisfies hosted sign-in requirements. _(#103, #113)_
- [x] Define and execute the migration and compatibility path away from the superseded hybrid hosted-authentication plan. _(#103, #118; strategy locked in plan/HOSTED_AUTH_MIGRATION_STRATEGY.md on 2026-03-13.)_
- [ ] Persist hosted authentication material securely using Azure Key Vault-backed patterns where required.
- [x] Replace the single-user `ICurrentUserContext` adapter with a per-request, per-user implementation backed by the hosted authentication session when hosted mode is enabled. _(Implemented on 2026-03-13; PAT-only local trusted mode preserved.)_
- [ ] Enable CD pipeline with production environment gate (`.github/workflows/cd.yml`).
- [ ] Write end-to-end tests for critical user journeys.
- [ ] Write comprehensive `docs/` content for all features. _(#119)_
- [ ] Tag v1.0.0 release on GitHub with release notes.

### Dependencies

- Phases 1–5 complete.


## AI Collaborator Instructions

### When Copilot Chat is asked to "Add feature X"

1. Check whether the feature is already in `plan/SCOPE.md`. If not, discuss with the developer whether it should be added to scope.
2. Identify which phase the feature belongs to (or create a new phase if necessary) and add it to this file.
3. Add the feature's epics and user stories to `plan/BACKLOG.md`.
4. Create a stub page in `docs/user-guide/<feature>.md`.
5. Open a GitHub Issue using the `feature.yml` template with the appropriate labels from `plan/LABEL_STRATEGY.md`.
6. Only then begin implementing the feature, following the architecture rules in `.github/copilot-instructions.md`.

### Keeping Docs in Sync with Code

- When a phase task is completed, tick it off in this document.
- When a feature's user-facing doc is written, update the stub notice in `docs/user-guide/<feature>.md`.
- When a new ADR is created, add it to `adr/README.md`.
- When a new environment variable is introduced, update `docs/getting-started.md` and `infra/README.md`.
