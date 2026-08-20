# User guide and Playwright E2E alignment

This document maps published User Guide pages in `website/content/docs/` to Playwright coverage in `tests/E2E/`. It is the canonical inventory for keeping documentation, application behaviour, and automated journeys aligned like-for-like.

The Hugo **product landing** at `/` on the public site is not the Blazor in-app home. Playwright asserts the in-app shell only; Hugo route checks run in `hugo-build.yml`.

## Alignment contract

Three artefacts must stay in sync for every shipped end-user feature:

1. **Application behaviour** — what SoloDevBoard actually does in the UI.
2. **User guide** — what `website/content/docs/<feature>.md` tells users they can do.
3. **Playwright E2E** — what `tests/E2E/tests/*.spec.ts` asserts in CI (and, where applicable, what `tests/E2E/docs-capture/` records for screenshots).

Rules:

- **User guides describe real behaviour only.** Do not document planned or stubbed capabilities without a scope note. Remove or update docs when behaviour changes.
- **E2E tests exercise what the guides claim.** Every published guide page must map to at least one Playwright spec. Assertions should target UI elements, routes, labels, and workflows described in the guide.
- **CI uses placeholder auth.** Repository-dependent journeys assert shells, navigation, and empty or error states unless a job is extended with live secrets. Loaded-state behaviour is validated via the docs-capture suite (manual, real PAT) and unit/component tests.
- **Screenshots follow public-only hygiene.** Never commit images showing private repositories, private Projects v2 boards, tokens, or other confidential information. Capture with `DocsCapture:Enabled=true` and the conventions in [plan/DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md#screenshot-convention).

When you add or change an end-user feature, update the user guide, the relevant Playwright spec(s), this mapping, and [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md) in the same pull request.

## Coverage matrix

| User guide / site page | App route(s) | Playwright spec (CI) | Docs-capture screenshot | CI tier | Notes |
|------------|--------------|----------------------|-------------------------|---------|-------|
| [Product landing](../../website/content/_index.md) (Hugo `/` only) | — | — (Hugo `hugo-build.yml` route check) | — | — | Short product-and-project landing (Learn more → About; icon tiles not docs links); not the Blazor app. |
| [In-app home dashboard](../../website/content/docs/) | `/` | `smoke.spec.ts`, `navigation.spec.ts`, `accessibility.spec.ts` | `dashboard/home.png` | Tier 1 | In-app home lists eight feature cards; About and Appearance are reached via the app bar. |
| [Audit Dashboard](../../website/content/docs/audit-dashboard.md) | `/audit-dashboard`, `/audit` | `audit-dashboard.spec.ts`, `accessibility.spec.ts` | `audit-dashboard/overview.png` | Tier 2 | Guide describes KPI cards, health sections (including label consistency), auto-refresh, and export; CI asserts shell, feedback region, and `/audit` alias with load failure. |
| [Repositories](../../website/content/docs/repositories.md) | `/repositories` | `repositories.spec.ts`, `accessibility.spec.ts` | `repositories/overview.png` | Tier 2 | Guide describes command strip, search, and a phone-safe stacked grid; CI asserts refresh, search, error-state retry, and no horizontal overflow at 390px. |
| [One-Click Migration](../../website/content/docs/one-click-migration.md) | `/migrate` | `migrate.spec.ts`, `accessibility.spec.ts` | `one-click-migration/overview.png` | Tier 2 | Guide describes labels, milestones, and Projects v2 Status column migration with board selectors and conflict strategies; CI asserts setup shell, columns scope control, disabled preview, and API failure feedback. |
| [Label Manager](../../website/content/docs/label-manager.md) | `/labels` | `labels.spec.ts`, `accessibility.spec.ts` | `label-manager/overview.png` | Tier 2 | Guide describes three tabs, taxonomy workflows, and optional **Remove labels outside taxonomy** strict delete ([#380](https://github.com/markheydon/solo-dev-board/issues/380)); CI asserts tab strip, disabled actions, and no-repositories message. |
| [Board Rules Visualiser](../../website/content/docs/board-rules-visualiser.md) | `/board-rules` | `board-rules.spec.ts`, `accessibility.spec.ts` | `board-rules-visualiser/overview.png` | Tier 2 | Guide describes compare mode and board selection; CI asserts selector region, compare toggle, and load failure. |
| [Triage UI](../../website/content/docs/triage-ui.md) | `/triage` | `triage.spec.ts`, `accessibility.spec.ts` | `triage-ui/overview.png` | Tier 2 | Guide describes session queue, shortcuts, milestones, and project board actions; CI asserts not-started region and no-repositories alert. |
| [Workflow Templates](../../website/content/docs/workflow-templates.md) | `/workflows` | `workflows.spec.ts`, `accessibility.spec.ts` | `workflow-templates/overview.png` | Tier 2 | Guide describes browse, parameterise, apply, and drift; CI asserts built-in template browse/filter/select and repository error state. |
| [Appearance](../../website/content/docs/appearance.md) | App bar (all shell pages) | `appearance.spec.ts`, `accessibility.spec.ts` | `appearance/theme-toggle.png` | Tier 1 | Guide describes Automatic → Light → Dark cycling and persistence; not a drawer route. |
| [About (in-app)](../../website/content/docs/about.md) | `/about` | `about.spec.ts`, `accessibility.spec.ts` | `about/overview.png` | Tier 1 | Guide describes More options menu access (User Guide external link and About route) and metadata fields. Not the Hugo `/about/` narrative section. |
| [Cross-Repo PM Workflow](../../website/content/docs/pm-workflow.md) | `/pm-workflow`, `/pm-workflow/daily-focus`, `/pm-workflow/backlog`, `/pm-workflow/planning`, `/pm-workflow/repos` | `pm-workflow.spec.ts`, `accessibility.spec.ts` | `pm-workflow/daily-focus.png`, `pm-workflow/repos.png` | Tier 2 | Guide documents Daily Focus occupancy, recommendations, stalled Up Next, stalled review pull requests, Backlog Review grouping, Iteration Planning (capacity, stall gate, Up Next batch, bulk milestone, candidate picker; [#283](https://github.com/markheydon/solo-dev-board/issues/283)–[#286](https://github.com/markheydon/solo-dev-board/issues/286)), and Repo Management. CI asserts shell, tab strip, Daily Focus occupancy, recommendation, stalled Up Next, and stalled-review regions or empty/error/warning copy, Backlog filters and all grouping panels (including awaiting triage, epics near completion, and neglected repositories) or empty/error/warning copy, Planning Up Next and candidate regions or empty/error/warning copy, and Repos regions (including per-repository summary) or chrome error. |

### Auth and entry journeys (operator docs, not user guide)

These journeys are documented in `docs/hosted-authentication.md` and `docs/pat-connectivity.md` rather than the published user site:

| Journey | Playwright spec | CI job |
|---------|-----------------|--------|
| PAT-mode welcome redirect and connectivity error page | `auth-entry.spec.ts` | `pat` |
| Hosted sign-in login gate | `auth-entry-hosted.spec.ts` | `hosted` |

## Guide section → assertion mapping

Use this when extending specs so assertions track documented workflows.

### Audit Dashboard

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Accessing (`/audit-dashboard`, `/audit`) | `audit-dashboard.spec.ts` URL and title | `docs-capture` |
| Repository selector and load failure | Feedback region, "Unable to load repositories" | `prepareAuditDashboardForCapture` |
| KPI cards, health sections (including label consistency), auto-refresh, export | — | `docs-capture` + unit/component tests |

### Label Manager

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Three tabs (Labels, Recommended taxonomy, Synchronise) | Tab strip and headings | `docs-capture` |
| Optional remove labels outside taxonomy | — | `docs-capture` + unit/component tests ([#380](https://github.com/markheydon/solo-dev-board/issues/380)) |
| No repositories message | Empty-state text | — |

### Repositories

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Accessing (`/repositories`) | `repositories.spec.ts` URL and title | `docs-capture` |
| Command strip, search, and load failure | Refresh control, search field, error state with retry | `prepareRepositoriesForCapture` |
| Phone-width stacked grid without horizontal overflow | `repositories.spec.ts` 390×844 viewport overflow check (error-state shell in CI) | `docs-capture` plus component tests for long names and chips |

### One-Click Migration

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Accessing (`/migrate`) | `migrate.spec.ts` URL and title | `docs-capture` |
| Migration setup shell and disabled preview | Workflow controls card, columns scope switch, disabled preview button | `docs-capture` |
| Repository load failure | Feedback region and GitHub API failure message | — |
| Project board columns scope, board selectors, preview lock, inaccessible-board warning | `MigrationTests` (bUnit) | `docs-capture` |
| Conflict strategies and Status overwrite warning | `MigrationTests` (bUnit) | `docs-capture` |
| Preview tables and apply summary for Status columns | — | `docs-capture` + Application tests |

### Project board column migration automated coverage ([#415](https://github.com/markheydon/solo-dev-board/issues/415))

Issue [#415](https://github.com/markheydon/solo-dev-board/issues/415) closes the test slice for feature [#291](https://github.com/markheydon/solo-dev-board/issues/291) (parent feature for Projects v2 Status column migration). Keep this mapping aligned with [one-click-migration.md](../../website/content/docs/one-click-migration.md).

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Skip / Merge / Overwrite conflict matrix for Status options | `MigrationServiceTests` | — |
| Create-new board path and preserve target option ids on name match | `MigrationServiceTests` | — |
| Overwrite does not delete options still referenced by items | `MigrationServiceTests` | — |
| Missing Status field and inaccessible boards | `MigrationServiceTests`, `GitHubServiceTests` | — |
| GraphQL discovery, `createProjectV2`, and update payload retains existing option ids | `GitHubServiceTests` | — |
| Columns scope switch, board selectors, preview locked until boards chosen | `MigrationTests` | `migrate.spec.ts` columns scope switch |
| Overwrite warning copy and inaccessible-board alert | `MigrationTests` | — |
| Setup shell, disabled preview, API failure feedback | — | `migrate.spec.ts` |
| Migration overview screenshot with board selectors | — | `docs-capture` (`one-click-migration/overview.png`) |

### Appearance

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Theme button in app bar | `appearance.spec.ts`, `accessibility.spec.ts` labelled control | `docs-capture` |
| Automatic → Light → Dark → Automatic cycle | `appearance.spec.ts` | — |
| Browser persistence | `appearance.spec.ts` localStorage check | — |

### About

| Guide section | CI assertion |
|---------------|--------------|
| More options → User Guide | `about.spec.ts` menu link to `https://solodevboard.com/docs/` |
| More options → About | `about.spec.ts` navigation |
| Version, build, .NET, auth mode, login, repository link | `about.spec.ts` `data-testid` fields |

### Cross-Repo PM Workflow (Daily Focus, Backlog Review, Iteration Planning, and Repo Management)

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Drawer or Home → PM Workflow → Daily Focus | `pm-workflow.spec.ts` URL, title, shell, tab strip | `docs-capture` (`pm-workflow/daily-focus.png`) |
| Planning board selector, status line, and refresh | `pm-workflow.spec.ts` shared chrome `data-testid`s | `docs-capture` |
| Occupancy chips, active load, empty board, or catalogue error | `pm-workflow-daily-focus-board-state` / empty or error copy, or chrome error | `docs-capture` |
| Stalled Up Next rows, none-stalled sentence, or catalogue error | `pm-workflow-daily-focus-stalled` when occupancy region is visible | `docs-capture` |
| Recommended today (all included repositories) list, empty copy, warning, or catalogue error | `pm-workflow-daily-focus-recommendations` heading, empty or error copy when occupancy loaded | `docs-capture` |
| Stalled review pull requests, empty stall copy, or stall load error | `pm-workflow-daily-focus-stalled-reviews` / empty or error copy when occupancy loaded | `docs-capture` |
| Planning thresholds | `pm-workflow-thresholds-region`, capacity field, or chrome error | `docs-capture` (`pm-workflow/repos.png`) |
| Repository participation summary | `pm-workflow-participation-summary` | `docs-capture` |
| Included repositories table or empty state | `pm-workflow-included-table` / `pm-workflow-no-included-text` | `docs-capture` |
| Excluded repositories and quick exclude | `pm-workflow-exclusions-region`, exclude autocomplete | `docs-capture` |
| Per-repository summary table, empty state, load error, or partial failure | `pm-workflow-repository-summary-table` / `pm-workflow-repository-summary-empty` / `pm-workflow-repository-summary-error` / `pm-workflow-repository-summary-partial-failure` | `docs-capture` |
| Backlog Review filters, urgency panels, empty copy, warning, or catalogue error | `pm-workflow.spec.ts` `PM Workflow` describe — `pm-workflow-backlog-filters` / `pm-workflow-backlog-panels` / empty or error copy, or chrome error | `docs-capture` |
| Awaiting triage, epics near completion, and neglected repositories | `pm-workflow.spec.ts` `Backlog Review` describe — `pm-workflow-backlog-awaiting-triage`, `pm-workflow-backlog-epics`, and `pm-workflow-backlog-neglected` when panels load | `docs-capture` |
| Iteration Planning Up Next, candidates, empty copy, warning, or catalogue error | `pm-workflow.spec.ts` `Iteration Planning` describe — `pm-workflow-planning-up-next` / `pm-workflow-planning-candidates` / empty or error copy, or chrome error | `docs-capture` |
| Capacity, stalled gate, and bulk milestone | `PmWorkflowPlanningTests` (bUnit); CI shell only | `docs-capture` |

### Daily Focus automated coverage ([#385](https://github.com/markheydon/solo-dev-board/issues/385))

Issue [#385](https://github.com/markheydon/solo-dev-board/issues/385) closes the Daily Focus test slice for stories [#273](https://github.com/markheydon/solo-dev-board/issues/273)–[#276](https://github.com/markheydon/solo-dev-board/issues/276). Keep this mapping alongside the PM Workflow rows above.

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Column counts and active load (Up Next + In Progress) | `DailyFocusBoardStateMapperTests`, `DailyFocusBoardStateServiceTests` | `pm-workflow.spec.ts` `Daily Focus` describe — occupancy region or empty/error copy |
| Inclusive 3-day Up Next stall | `DailyFocusBoardStateMapperTests` | `pm-workflow.spec.ts` `PM Workflow` describe — `pm-workflow-daily-focus-stalled` when occupancy loads |
| Top-three priority ranking | `DailyFocusRecommendationMapperTests`, `DailyFocusRecommendationServiceTests` | `pm-workflow.spec.ts` `PM Workflow` describe — recommendations region or error/warning |
| Stalled review pull requests | `DailyFocusStalledReviewDetectorTests`, `DailyFocusStalledReviewServiceTests` | `pm-workflow.spec.ts` `PM Workflow` describe — `pm-workflow-daily-focus-stalled-reviews` or error |
| Route shell, loading, empty board, GitHub retry, inaccessible-board warning | `PmWorkflowDailyFocusTests` | `pm-workflow.spec.ts` `Daily Focus` describe — route shell and no-board/empty/error copy |

Docs-capture screenshots for Daily Focus and Repo Management live under `website/static/images/pm-workflow/` (`daily-focus.png`, `repos.png`). Capture prefers the public **SoloDevBoard Roadmap** board with `DocsCapture:Enabled=true`.

### Repo Management automated coverage ([#388](https://github.com/markheydon/solo-dev-board/issues/388))

Issue [#388](https://github.com/markheydon/solo-dev-board/issues/388) closes the Repo Management test slice for stories [#287](https://github.com/markheydon/solo-dev-board/issues/287) and [#288](https://github.com/markheydon/solo-dev-board/issues/288). Keep this mapping alongside the PM Workflow rows above.

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Planning thresholds (capacity, stall days, neglect days) | `PmSettingsDefaultsTests`, `PmSettingsServiceTests` | `pm-workflow.spec.ts` `Repo Management` describe — threshold fields and regions |
| Repository participation and exclusions | `PmWorkflowReposTests` | `pm-workflow.spec.ts` `Repo Management` describe — participation summary, included table, exclusions |
| Per-repository summary table, empty state, load error, or partial failure | `PmWorkflowReposTests` | `pm-workflow.spec.ts` `Repo Management` and `PM Workflow` describes — summary region or error/empty/loading |
| Route shell and no-board instructional copy | `PmWorkflowReposTests` | `pm-workflow.spec.ts` `Repo Management` describe — route shell and board selector alert |

### Backlog Review automated coverage ([#386](https://github.com/markheydon/solo-dev-board/issues/386))

Issue [#386](https://github.com/markheydon/solo-dev-board/issues/386) closes the Backlog Review test slice for stories [#280](https://github.com/markheydon/solo-dev-board/issues/280)–[#279](https://github.com/markheydon/solo-dev-board/issues/279) (parent feature [#277](https://github.com/markheydon/solo-dev-board/issues/277), delivered in PR [#419](https://github.com/markheydon/solo-dev-board/pull/419)). Keep this mapping alongside the PM Workflow rows above.

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Urgent, Ready to start, and Blocked/deferred panels | `BacklogReviewGroupingTests`, `BacklogReviewServiceTests` | `pm-workflow.spec.ts` `PM Workflow` and `Backlog Review` describes — urgency panel `data-testid`s |
| Awaiting triage (missing `type/` or `priority/`) | `BacklogReviewGroupingTests` | `pm-workflow.spec.ts` `Backlog Review` describe — `pm-workflow-backlog-awaiting-triage` |
| Urgent items deduplicated from Ready to start | `BacklogReviewGroupingTests` | Unit/component only (row membership) |
| Issue versus pull request kind chips | `PmWorkflowBacklogTests` | Unit/component only (row chips); `docs-capture` when table rows render |
| Epics near completion | `BacklogReviewGroupingTests`, `PmWorkflowBacklogTests` | `pm-workflow.spec.ts` `Backlog Review` describe — `pm-workflow-backlog-epics` panel or empty/unavailable copy |
| Neglected repositories | `BacklogReviewGroupingTests`, `PmWorkflowBacklogTests` | `pm-workflow.spec.ts` `Backlog Review` describe — `pm-workflow-backlog-neglected` panel or empty copy |
| Route shell, no-board copy, catalogue empty, filter empty, partial-failure warning, GitHub retry | `PmWorkflowBacklogTests` | `pm-workflow.spec.ts` `Backlog Review` describe — route shell and no-board/empty/error/warning copy |

### Iteration Planning automated coverage ([#387](https://github.com/markheydon/solo-dev-board/issues/387))

Issue [#387](https://github.com/markheydon/solo-dev-board/issues/387) closes the Iteration Planning test slice for stories [#283](https://github.com/markheydon/solo-dev-board/issues/283)–[#286](https://github.com/markheydon/solo-dev-board/issues/286). Keep this mapping alongside the PM Workflow rows above.

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Focus Order sequencing for story, enabler, and test labels | `PlanningFocusOrderSequencerTests` | Unit/component only |
| Up Next and candidate mapping | `IterationPlanningViewMapperTests` | Unit/component only |
| Add to Up Next writes (status, triage add, Focus Order) | `IterationPlanningServiceTests` | Unit/component only |
| Active load capacity flag and at-capacity warning | `IterationPlanningViewMapperTests`, `PmWorkflowPlanningTests` | Unit/component only |
| Capacity exceeded confirmation dialog before add | `PmWorkflowPlanningTests` | Unit/component only |
| Stalled Up Next gate disables add | `PmWorkflowPlanningTests` | Unit/component only |
| Bulk milestone skip when milestone missing on a repository | `PlanningBulkMilestoneAssignerTests`, `IterationPlanningServiceTests` | Unit/component only |
| Add to Up Next success and failure snackbars | `PmWorkflowPlanningTests` | Unit/component only |
| Route shell, no-board copy, Up Next and candidate regions, empty copy, load error, partial failure | `PmWorkflowPlanningTests` | `pm-workflow.spec.ts` `Iteration Planning` describe — route shell and no-board/empty/error/warning copy |

See [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md) Tier 2 — PM Workflow row — for the journey-level summary.

## Screenshot hygiene

- Use **light theme** at **1400×900** viewport with kebab-case PNG filenames under `website/static/images/<feature-slug>/`.
- Capture with `DocsCapture:Enabled=true` and the example repository `markheydon/solo-dev-board`.
- Never commit private repositories, tokens, or confidential project boards.

## When guides or journeys change

1. Update the User Guide page under `website/content/docs/`.
2. Update `website/content/_index.md` and `website/content/docs/_index.md` when the published feature list changes.
3. Update the relevant Playwright spec(s) and this file.
4. Refresh docs-capture screenshots when the UI changes materially.
