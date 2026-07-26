# SoloDevBoard — Implementation Plan

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file before making changes to this plan. -->


This document describes the phased implementation of SoloDevBoard. Each phase has a clear goal, a set of key tasks, and defined dependencies.

**Note on sequencing:**
Phases remain the primary sequence for feature delivery. Unfinished work from earlier phases remains open until completed, regardless of progress in later phases. However, certain public-release prerequisites from Phase 6 (such as hosted authentication and admission control) may be pulled forward out of sequence when required to enable safe hosted validation. This does not imply that earlier phases are complete or that the product has reached v1.0.0 readiness.

**Current roadmap status (2026-07-18):**
- Phases 1–4 are complete. All six core feature areas are delivered (Label Manager, Audit Dashboard, One-Click Migration, Triage UI, Board Rules Visualiser, Workflow Templates).
- Phase 5 (Cross-Repo PM Workflow, v0.5.0) is parked until after v1.0.0 ships — tracked in [#272](https://github.com/markheydon/solo-dev-board/issues/272).
- Phase 6 (Production Ready, v1.0.0) is the active release-closure phase.
- Deferred follow-on slices from Phases 1–4 are tracked in [#289](https://github.com/markheydon/solo-dev-board/issues/289) and child issues.

For the full feature scope, see [SCOPE.md](SCOPE.md). For open work, see [GitHub Issues](https://github.com/markheydon/solo-dev-board/issues) and [Project #8](https://github.com/users/markheydon/projects/8). The backlog index is at [BACKLOG.md](BACKLOG.md).

All planned front-end delivery after ADR-0012 uses MudBlazor as the sole UI component library.

---

## Phase 1 — Foundation

**Goal:** Establish a working Blazor Server application with GitHub authentication, a basic repository listing, and an empty dashboard shell. This phase produces a deployable skeleton.

**Milestone:** v0.1.0

**Status:** Complete.

### Key Tasks

- [x] Scaffold solution structure: `App`, `Application`, `Domain`, `Infrastructure` projects
- [x] Configure nullable reference types, implicit usings, and coding conventions across all projects
- [x] Implement GitHub Personal Access Token (PAT) authentication flow
- [x] Implement GitHub App authentication flow (optional in this phase, required before v1.0 — delivered in Phase 6 hosted-auth side-step)
- [x] Implement `IGitHubService` interface in `Application` layer
- [x] Implement `GitHubService` in `Infrastructure` layer (using `HttpClient` + `System.Text.Json`)
- [x] Implement basic repository listing: fetch and display all repositories the authenticated user has access to _(issue #8 done — 2026-03-07)_
- [x] Create a MudBlazor home page with navigation to all feature areas _(evolved from the original empty dashboard shell)_
- [x] Configure `appsettings.json` and user secrets for local development
- [x] Set up xUnit test projects; write smoke tests for `GitHubService`
- [x] Set up CI workflow (`.github/workflows/ci.yml`) — build and test on every PR
- [x] Deploy to Azure Container Apps using Aspire (`aspire deploy` from `SoloDevBoard.AppHost`) — confirm the app runs in the cloud

### Dependencies

- GitHub PAT or GitHub App credentials for local development
- Azure subscription for deployment validation

---

## Phase 2 — Label Manager + Audit Dashboard

**Goal:** Deliver the first two user-facing features: the ability to manage labels across repositories, and an audit view of repository health.

**Milestone:** v0.2.0

**Status:** Complete. Core Label Manager and Audit Dashboard features are delivered, including Audit Dashboard auto-refresh ([#258](https://github.com/markheydon/solo-dev-board/issues/258)) and Markdown export ([#259](https://github.com/markheydon/solo-dev-board/issues/259)). Label consistency warnings remain deferred — see [#289](https://github.com/markheydon/solo-dev-board/issues/289) and child issues.

### Key Tasks

#### Architecture Preparation (Multi-Tenancy Readiness)
- [x] Define `ICurrentUserContext` interface in `SoloDevBoard.Application` to represent the authenticated user's identity and API token (see ADR-0007)
- [x] Implement a single-user adapter for `ICurrentUserContext` in `SoloDevBoard.Infrastructure` (reads from `IOptions<GitHubAuthOptions>` — no behaviour change from Phase 1)
- [x] Refactor `IGitHubService` and all Application-layer services to inject and use `ICurrentUserContext` — no service may access `IOptions<GitHubAuthOptions>` directly

#### Label Manager
- [x] Design `Label` domain record and `ILabelRepository` interface
- [x] Implement `GitHubLabelRepository` in `Infrastructure`
- [x] Implement `LabelService` in `Application` (CRUD, sync operations)
- [x] Build MudBlazor UI components for the Label Manager
- [x] Implement label synchronisation logic (compare source and target, produce diff, apply changes)
- [x] Write unit tests for `LabelService` using Moq
- [x] Write infrastructure tests for `GitHubLabelRepository` (mocked HTTP)
- [x] Update `user-docs/content/docs/label-manager.md`

#### Audit Dashboard
- [x] Design `AuditReport` domain record
- [x] Implement `AuditDashboardService` in `Application` (aggregate data from multiple repositories)
- [x] Build MudBlazor UI components for the Audit Dashboard
- [x] Implement health indicators: unlabelled issues, stale PRs, failing workflows
- [ ] Implement label consistency warnings _(deferred — originally scoped in SCOPE.md but not yet built)_
- [x] Write unit tests for `AuditDashboardService`
- [x] Update `user-docs/content/docs/audit-dashboard.md`

### Dependencies

- Phase 1 complete (GitHub client, repository listing, authenticated session)

---

## Phase 3 — One-Click Migration + Triage UI

**Goal:** Allow users to migrate repository configuration (labels, milestones) and provide a streamlined issue triage experience.

**Milestone:** v0.3.0

**Status:** Complete. One-Click Migration and Triage UI are delivered, and v0.3.0 is ready for closure.

### Key Tasks

#### One-Click Migration
- [x] Design `MigrationPlan` and `MigrationResult` domain records.
- [x] Implement `MigrationService` in `Application` (diff, preview, apply).
- [x] Build MudBlazor UI: source/target repository selection, diff preview, confirmation, summary.
- [x] Support migration of labels and milestones for the current delivery slice.
- [ ] Support migration of project board columns as a later slice of this feature.
- [x] Write unit tests for `MigrationService`.
- [x] Update `user-docs/content/docs/one-click-migration.md`.

#### Triage UI
- [x] Design `TriageSession` and `TriageAction` domain records
- [x] Implement `TriageService` in `Application`
- [x] Build focused MudBlazor triage view with keyboard shortcut support
- [x] Implement quick actions: label, assign milestone, add to project, close as duplicate
- [x] Write unit tests for `TriageService`
- [x] Update `user-docs/content/docs/triage-ui.md`


### Dependencies

- Phase 2 complete (Label Manager, GitHub label/milestone API integration)

**Note:** v0.3.0 is complete. Project board column migration remains a deferred follow-on slice (ADR-0013).

---

## Phase 4 — Board Rules Visualiser + Workflow Templates

**Goal:** Deliver the remaining two features: a visual representation of project board automation rules, and a template library for GitHub Actions workflows.

**Milestone:** v0.4.0

**Status:** Complete. Board Rules Visualiser and Workflow Templates are delivered.

### Key Tasks

#### Board Rules Visualiser
- [x] Investigate GitHub Projects v2 GraphQL API for automation rule access
- [x] Design `BoardRule` and `BoardDiagram` domain records
- [x] Implement `BoardRuleService` in `Application`
- [x] Implement GraphQL client in `Infrastructure` (see ADR-0005)
- [x] Build interactive MudBlazor-based diagram component (compare mode now available)
- [x] Write unit tests for `BoardRuleService`, `BoardRulesComparer`, and compare mode functionality
- [x] Update `user-docs/content/docs/board-rules-visualiser.md` _(compare mode documented as available)_

#### Workflow Templates
- [x] Design `WorkflowTemplate` domain record
- [x] Implement `WorkflowTemplateService` in `Application`
- [x] Build MudBlazor UI: template browser, parameter editor, apply to repositories, staleness tracker
- [x] Include built-in templates: CI (dotnet), CD (Aspire deploy to Azure Container Apps), Dependabot
- [x] Write unit tests for `WorkflowTemplateService`
- [x] Update `user-docs/content/docs/workflow-templates.md`
- [ ] Support custom template repositories _(deferred follow-on slice — only built-in templates are available today)_

### Dependencies

- Phase 1 complete (GitHub GraphQL client infrastructure)
- Phase 2 complete (repository selection component)

---

## Phase 5 — Cross-Repo PM Workflow

**Goal:** Deliver Epic 7 — the UI-based implementation of the two-mode PM operating system from [markheydon/github-workflows](https://github.com/markheydon/github-workflows). This phase transforms SoloDevBoard from a collection of individual tools into a cohesive planning environment.

**Milestone:** v0.5.0

**Status:** Not started. Phases 1–4 are complete; this is the next active phase.

### Key Tasks

#### Daily Focus
- [ ] Design `DailyFocusReport` and `BoardSnapshot` domain records
- [ ] Implement `DailyFocusService` in `Application` (board state, stalled items, top-priority recommendations)
- [ ] Build MudBlazor Daily Focus view: board state summary, stalled item alerts, top-3 recommended work items
- [ ] Implement stalled item detection (Up Next for 3+ days; PRs In Review for 3+ days)
- [ ] Write unit tests for `DailyFocusService`
- [ ] Update `user-docs/content/docs/pm-workflow.md`

#### Backlog Review
- [ ] Implement `BacklogReviewService` in `Application` (cross-repo, priority-grouped, PR-aware)
- [ ] Build MudBlazor Backlog Review view: groups for urgent, ready, blocked, deferred; neglected repo alerts
- [ ] Implement neglected repo detection (no issue or PR activity in 14 days)
- [ ] Write unit tests for `BacklogReviewService`
- [ ] Update `user-docs/content/docs/pm-workflow.md`

#### Iteration Planning
- [ ] Design `IterationPlan` domain record
- [ ] Implement `IterationPlanningService` in `Application` (capacity enforcement, stale resolution, milestone assignment)
- [ ] Build MudBlazor Iteration Planning view: capacity indicator, stale item resolution, Up Next curation, optional milestone assignment
- [ ] Write unit tests for `IterationPlanningService`
- [ ] Update `user-docs/content/docs/pm-workflow.md`

#### Repo Management
- [ ] Implement excluded-repos configuration (persisted per user, applied to all cross-repo operations)
- [ ] Build MudBlazor settings UI for managing excluded repositories

### Dependencies

- Intended sequence: begin after Phases 1–4 are complete (repository selection, label management, board API integration, GitHub Projects v2 GraphQL client).

---


## Phase 6 — Polish, Testing, and Azure Deployment

**Goal:** Achieve production quality for public release: comprehensive test coverage, operational hardening, secure authentication, observability, and a stable Azure deployment pipeline.

**Milestone:** v1.0.0

**Status:** Partially complete. Hosted authentication, Aspire ACA deployment, and Dependabot are delivered. Operational hardening, release closure, and remaining auth polish are open. Begins in earnest after Phase 5.

### Key Tasks
- [ ] Achieve ≥80% unit test coverage across `Application` and `Domain` projects. _(#252)_
- [x] Perform accessibility audit of primary journey shells (WCAG 2.1 AA). _(#253; see plan/ACCESSIBILITY_AUDIT.md and tests/E2E/tests/accessibility.spec.ts.)_
- [ ] Conduct performance review: identify and address slow GitHub API calls (caching, pagination). _(#254)_
- [x] Complete Azure infrastructure baseline via Bicep (App Service, Key Vault, managed identity). _(#104 — superseded by ADR-0018 Aspire ACA migration.)_
- [x] Migrate production deployment to Aspire Azure Container Apps with scale-to-zero. _(ADR-0018.)_
- [x] Configure OIDC authentication for GitHub Actions deployment to Azure (no long-lived credentials). _(#105)_
- [x] Add health check endpoints for Azure Container Apps monitoring. _(#106 — readiness `/health`, liveness `/alive`, ACA probe via AppHost `WithHttpHealthCheck`)_
- [x] Implement response caching for GitHub API calls to respect rate limits. _(#108)_
- [x] Configure structured logging and Application Insights telemetry. _(#107)_
- [ ] Add operational hardening test coverage and validation expectations. _(#110; see plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md.)_
- [x] Set up Dependabot for automated dependency updates. _(#109)_
- [x] Formalise and document PAT-only local trusted mode and self-hoster deployment path. _(#247, #248; see docs/getting-started.md#pat-only-local-trusted-mode and docs/deployment.md#self-hoster-deployment-pat-mode.)_
- [x] Dedicated unauthenticated landing page for hosted deployments. _(#249)_
- [x] Surface GitHub auth connectivity problems before feature work fails (PAT-mode readiness). _(#314)_
- [x] Implement hosted authentication session boundaries and per-request user context for GitHub App-first hosted mode. _(#103, #112; implemented on 2026-03-13, see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md.)_
- [x] Integrate the real hosted sign-in gateway and session/callback handshake at `/auth/sign-in`, mapping required hosted claims before admission control and repository loading. _(#103, #123; implemented on 2026-03-16; see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md; closes the `/auth/sign-in` planning gap and unblocks #114 and #119.)_
- [x] Handle hosted installation context validation and token lifecycle checks (expiry and failure handling) for hosted requests. _(#103, #111; implemented on 2026-03-13; see plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md.)_
- [x] Restrict hosted access to operator-managed user and organisation allow-lists, with deny-by-default admission control. _(#103, #117; implemented on 2026-03-16; see ADR-0014, ADR-0015, and plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md.)_
- [x] Remove or demote the separate OAuth App dependency where GitHub App user authentication satisfies hosted sign-in requirements. _(#103, #113; implemented on 2026-03-16; see ADR-0015 and plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md.)_
- [x] Define and execute the migration and compatibility path away from the superseded hybrid hosted-authentication plan. _(#103, #118; strategy locked in plan/HOSTED_AUTH_MIGRATION_STRATEGY.md on 2026-03-13.)_
- [x] Persist hosted authentication material securely using Azure Key Vault-backed patterns where required. _(#250; see plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md.)_
- [x] Replace the single-user `ICurrentUserContext` adapter with a per-request, per-user implementation backed by the hosted authentication session when hosted mode is enabled. _(Implemented on 2026-03-13; PAT-only local trusted mode preserved.)_
- [ ] Enable CD pipeline with production environment gate (`.github/workflows/cd.yml` via `aspire deploy`). _(#251)_
- [x] Write end-to-end tests for critical user journeys. _(#255; see tests/E2E/CRITICAL_JOURNEYS.md.)_
- [x] Update hosted-authentication documentation for GitHub App-first hosted sign-in, admission-control allow-lists, PAT-only local trusted mode, and fallback boundaries. _(#119; completed on 2026-03-16; see docs/getting-started.md, infra/README.md, docs/hosted-authentication.md, and user-docs/content/_index.md.)_
- [x] Write comprehensive end-user documentation for all shipped features under `user-docs/`. _(#256; Hugo site + screenshots; `pm-workflow.md` remains draft until Phase 5)_
- [ ] Tag v1.0.0 release on GitHub with release notes. _(#257)_


### Dependencies

- Intended release sequence: complete Phase 5, then close out the remaining Phase 6 release work.

**Note:** Hosted authentication and admission control were advanced out of sequence to enable safe hosted validation. Phases 1–4 are complete; v1.0.0 readiness depends on Phase 5 and the remaining Phase 6 tasks above.


## AI Collaborator Instructions

### When Copilot Chat is asked to "Add feature X"

1. Check whether the feature is already in `plan/SCOPE.md`. If not, discuss with the developer whether it should be added to scope.
2. Identify which phase the feature belongs to (or create a new phase if necessary) and add it to this file.
3. Create a GitHub Issue using the `feature.yml` template with labels from `plan/LABEL_STRATEGY.md` and sync Project #8.
4. Create a stub page in `user-docs/content/docs/<feature>.md`.
5. Only then begin implementing the feature, following the architecture rules in `AGENTS.md`.

### Keeping Docs in Sync with Code

- When a phase task is completed, tick it off in this document.
- When a feature's user-facing doc is written, update the stub notice in `user-docs/content/docs/<feature>.md`.
- When a new decision is recorded, add it to `plan/DECISIONS.md` per `repo-decision-log`.
- When a new environment variable is introduced, update `docs/getting-started.md` and `docs/deployment.md`.
