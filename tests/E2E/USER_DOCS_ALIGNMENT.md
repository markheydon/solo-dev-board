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
| [Product landing](../../website/content/_index.md) (Hugo `/` only) | — | — (Hugo `hugo-build.yml` route check) | — | — | Short product-and-project landing (Learn more → About; whole-card feature tiles link to User Guide pages); not the Blazor app. |
| [In-app home dashboard](../../website/content/docs/) | `/` | `smoke.spec.ts`, `navigation.spec.ts`, `accessibility.spec.ts` | `dashboard/home.png` | Tier 1 | In-app home lists eight feature cards; About and Appearance are reached via the app bar. |
| [Audit Dashboard](../../website/content/docs/audit-dashboard.md) | `/audit-dashboard` | `audit-dashboard.spec.ts`, `accessibility.spec.ts` | `audit-dashboard/overview.png` | Tier 2 | Guide describes KPI cards, health sections (including label consistency), auto-refresh, and export; CI asserts shell and feedback region with load failure. |
| [Repositories](../../website/content/docs/repositories.md) | `/repositories` | `repositories.spec.ts`, `accessibility.spec.ts` | `repositories/overview.png` | Tier 2 | Guide is Partially Available: live catalogue view/search/refresh and Open source / Not open source filters; Add, Remove, Bulk actions, Edit, and More remain stubs ([#435](https://github.com/markheydon/solo-dev-board/issues/435)). CI asserts refresh, search, catalogue filter controls, error-state retry, placeholder snackbar a11y, and no horizontal overflow at 390px. |
| [One-Click Migration](../../website/content/docs/one-click-migration.md) | `/migrate` | `migrate.spec.ts`, `accessibility.spec.ts` | `one-click-migration/overview.png` | Tier 2 | Guide is Available: labels, milestones, and Projects v2 Status columns with keep/ignore `area/*` controls; CI asserts setup shell, columns scope, nested ignore/keep `area/*` controls, disabled preview, and API failure feedback. |
| [Label Manager](../../website/content/docs/label-manager.md) | `/labels` | `labels.spec.ts`, `accessibility.spec.ts` | `label-manager/overview.png` | Tier 2 | Guide describes three tabs, Labels-tab bulk delete ([#444](https://github.com/markheydon/solo-dev-board/issues/444)), taxonomy workflows, optional **Remove labels outside taxonomy** strict delete ([#380](https://github.com/markheydon/solo-dev-board/issues/380)), and nested **Keep `area/*` labels** ([#446](https://github.com/markheydon/solo-dev-board/issues/446)); CI asserts tab strip, remove-outside control, sync keep-area control, disabled bulk delete, and repository load failure feedback. Empty-repository copy is covered by `LabelsTests`. |
| [Board Rules Visualiser](../../website/content/docs/board-rules-visualiser.md) | `/board-rules` | `board-rules.spec.ts`, `accessibility.spec.ts` | `board-rules-visualiser/overview.png` | Tier 2 | Guide is Partially Available: compare mode and board selection now; full GitHub automation-rule retrieval later ([#437](https://github.com/markheydon/solo-dev-board/issues/437)). CI asserts selector region, compare toggle, and load failure. |
| [Triage UI](../../website/content/docs/triage-ui.md) | `/triage` | `triage.spec.ts`, `accessibility.spec.ts` | `triage-ui/overview.png` | Tier 2 | Guide describes session queue, shortcuts, milestones, and project board actions; CI asserts not-started region and inline repository load failure with retry. |
| [Actions Templates](../../website/content/docs/actions-templates.md) | `/actions-templates` | `actions-templates.spec.ts`, `accessibility.spec.ts` | `actions-templates/overview.png` | Tier 2 | Guide is Partially Available: browse, parameterise, apply, and drift now; custom template repositories ([#292](https://github.com/markheydon/solo-dev-board/issues/292)) and persisted parameter profiles ([#436](https://github.com/markheydon/solo-dev-board/issues/436)) later. CI asserts built-in template browse/filter/select and repository error state. |
| [Planning](../../website/content/docs/planning.md) | `/planning`, `/planning/daily-focus`, `/planning/backlog`, `/planning/iteration`, `/planning/repos`, `/planning/board-setup` (conditional) | `planning.spec.ts`, `accessibility.spec.ts` | `planning/daily-focus.png`, `planning/backlog.png`, `planning/iteration.png`, `planning/repos.png` | Tier 2 | Guide documents Daily Focus occupancy, recommendations, stalled Up Next, stalled review pull requests, Backlog Review grouping, Iteration (capacity, stall gate, Up Next batch, bulk milestone, candidate picker; [#283](https://github.com/markheydon/solo-dev-board/issues/283)–[#286](https://github.com/markheydon/solo-dev-board/issues/286)), conditional Board setup compatibility ([#445](https://github.com/markheydon/solo-dev-board/issues/445)), and Repo Management. User Guide sidebar and feature table follow app drawer order; Appearance and About live under a separate **App shell** Hugo section so prev/next stops at the last feature. CI asserts shell, tab strip, Daily Focus occupancy, recommendation, stalled Up Next, and stalled-review regions or empty/error/warning copy, Backlog filters and all grouping panels (including awaiting triage, epics near completion, and neglected repositories) or empty/error/warning copy, Planning Up Next and candidate regions or empty/error/warning copy, and Repos regions (including per-repository summary) or chrome error. |
| [Appearance](../../website/content/docs/app-shell/appearance.md) | App bar (all shell pages) | `appearance.spec.ts`, `accessibility.spec.ts` | `appearance/theme-toggle.png` | Tier 1 | Guide describes Automatic → Light → Dark cycling and persistence; not a drawer route. Lives under User Guide **App shell** (`/docs/app-shell/`) with About; prev/next does not continue from feature guides. |
| [About (in-app)](../../website/content/docs/app-shell/about.md) | `/about` | `about.spec.ts`, `accessibility.spec.ts` | `about/overview.png` | Tier 1 | Guide describes More options menu access (User Guide external link and About route) and metadata fields. Not the Hugo `/about/` narrative section. Lives under User Guide **App shell** with Appearance. |

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
| Accessing (`/audit-dashboard`) | `audit-dashboard.spec.ts` URL and title | `docs-capture` |
| Repository selector and load failure | Feedback region, "Unable to load repositories" | `prepareAuditDashboardForCapture` |
| KPI cards, health sections (including label consistency), auto-refresh, export | — | `docs-capture` + unit/component tests |

### Label Manager

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Three tabs (Labels, Recommended taxonomy, Synchronise) | Tab strip and headings | `docs-capture` |
| Labels tab bulk delete (select rows, confirm, cancel) | Disabled bulk delete button on shell | Component tests in `LabelsTests` ([#444](https://github.com/markheydon/solo-dev-board/issues/444)) |
| Optional remove labels outside taxonomy | — | `docs-capture` + unit/component tests ([#380](https://github.com/markheydon/solo-dev-board/issues/380)) |
| Confirm apply shows in-progress feedback | — | Component tests in `LabelsTests` (loading Confirm button and progress indicator) |
| No repositories message | Empty-state text | — |

### Label Manager keep `area/*` automated coverage ([#457](https://github.com/markheydon/solo-dev-board/issues/457))

Issue [#457](https://github.com/markheydon/solo-dev-board/issues/457) closes the test slice for story [#446](https://github.com/markheydon/solo-dev-board/issues/446) (keep `area/*` labels out of built-in taxonomy cleanup). Keep this mapping aligned with [label-manager.md](../../website/content/docs/label-manager.md).

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Built-in recommended catalogue excludes `area/*` names | `LabelServiceTests.GetRecommendedTaxonomyAsync` | — |
| Remove-outside off leaves extras undeleted | `LabelServiceTests.PreviewRecommendedTaxonomyAsync_WhenRemoveOutsideTaxonomyDisabled_ReturnsNoLabelsToDelete` | — |
| Remove-outside on with keep on/off and Synchronise extra deletes | `LabelServiceTests` preview/apply/sync matrix | — |
| Nested keep checkbox enablement, default checked, preview caption | `LabelsTests` Recommended taxonomy and Synchronise tabs | `labels.spec.ts` remove-outside enables `keep-area-labels-checkbox` when repositories are selected; Synchronise `sync-keep-area-labels-checkbox` |

### Label Manager bulk delete automated coverage ([#459](https://github.com/markheydon/solo-dev-board/issues/459))

Issue [#459](https://github.com/markheydon/solo-dev-board/issues/459) closes the test slice for story [#444](https://github.com/markheydon/solo-dev-board/issues/444) (Labels-tab bulk delete). Keep this mapping aligned with [label-manager.md](../../website/content/docs/label-manager.md).

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Bulk delete continues after per-repository GitHub errors | `LabelServiceTests.BulkDeleteLabelsAsync_OneDeleteFails_RecordsErrorAndContinues` | — |
| Bulk delete skips repositories without the selected label | `LabelServiceTests.BulkDeleteLabelsAsync_LabelMissingInRepository_CountsAsSkippedAndContinues` | — |
| Bulk Delete disabled with no selection; confirm lists names/repos; cancel keeps selection; success refreshes grid; disable while in flight | `LabelsTests` bulk delete describe | `labels.spec.ts` disabled bulk delete button on shell |

### Repositories

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Accessing (`/repositories`) | `repositories.spec.ts` URL and title | `docs-capture` |
| Command strip, search, catalogue filter, and load failure | Refresh control, search field, catalogue filter toggle group, error state with retry | `prepareRepositoriesForCapture` |
| Filter open-source project repositories (All / Open source / Not open source) | `repositories.spec.ts` catalogue filter `data-testid`s on the shell | `RepositoriesTests` (bUnit) for filtered rows and search AND filter |
| Empty and error states (filter-empty copy) | — | `RepositoriesTests` (bUnit) for Open source, Not open source, and combined search empty messages |
| Stub Add / Remove / Bulk actions / Edit / More | `accessibility.spec.ts` placeholder snackbar on Add | — (tracked by [#435](https://github.com/markheydon/solo-dev-board/issues/435)) |
| Phone-width stacked grid without horizontal overflow | `repositories.spec.ts` 390×844 viewport overflow check (error-state shell in CI) | `docs-capture` plus component tests for long names and chips |

### OSS catalogue identification automated coverage ([#443](https://github.com/markheydon/solo-dev-board/issues/443))

Issue [#443](https://github.com/markheydon/solo-dev-board/issues/443) closes the test slice for feature [#440](https://github.com/markheydon/solo-dev-board/issues/440) (parent feature for open-source catalogue classification and Repositories filters). Keep this mapping aligned with [repositories.md](../../website/content/docs/repositories.md).

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Canonical `open-source` topic matcher (case-insensitive; `oss` alone does not match) | `OpenSourceTopicTests` | — |
| GitHub list-repos `topics` mapped onto domain `Repository` | `GitHubServiceTests` | — |
| `RepositoryDto.IsOpenSource` and catalogue filter helpers | `RepositoryServiceTests`, `RepositoryCatalogueFiltersTests` | — |
| Default All list unchanged; Open source and Not open source filters | `RepositoriesTests` | `repositories.spec.ts` catalogue filter controls visible on shell |
| Filter combined with name search | `RepositoriesTests` | — |
| Filter-empty and combined search-and-filter empty copy | `RepositoriesTests` | — |
| Repositories overview screenshot (catalogue filter strip visible) | — | `docs-capture` (`repositories/overview.png`; refresh manually when the filter strip changes materially) |

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
| Overwrite keep `area/*` preview and apply behaviour | `MigrationServiceTests` | — |
| Ignore source `area/*` preview and apply behaviour | `MigrationServiceTests` | — |
| Missing Status field and inaccessible boards | `MigrationServiceTests`, `GitHubServiceTests` | — |
| GraphQL discovery, `createProjectV2`, and update payload retains existing option ids | `GitHubServiceTests` | — |
| Columns scope switch, board selectors, preview locked until boards chosen | `MigrationTests` | `migrate.spec.ts` columns scope switch |
| Overwrite warning copy, keep `area/*` nested control, and inaccessible-board alert | `MigrationTests` | `migrate.spec.ts` keep `area/*` control when Overwrite selected |
| Ignore `area/*` nested control (default on) for Labels scope | `MigrationTests` | `migrate.spec.ts` ignore `area/*` control when Labels selected |
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

### Planning (Daily Focus, Backlog Review, Iteration, and Repo Management)

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Drawer or Home → Planning → Daily Focus | `planning.spec.ts` URL, title, shell, tab strip | `docs-capture` (`planning/daily-focus.png`) |
| Planning board selector, status line, and refresh | `planning.spec.ts` shared chrome `data-testid`s | `docs-capture` |
| Occupancy chips, active load, empty board, or catalogue error | `planning-daily-focus-board-state` / empty or error copy, or chrome error | `docs-capture` |
| Stalled Up Next rows, none-stalled sentence, or catalogue error | `planning-daily-focus-stalled` when occupancy region is visible | `docs-capture` |
| Recommended today (all included repositories) list, empty copy, warning, or catalogue error | `planning-daily-focus-recommendations` heading, empty or error copy when occupancy loaded | `docs-capture` |
| Stalled review pull requests, empty stall copy, or stall load error | `planning-daily-focus-stalled-reviews` / empty or error copy when occupancy loaded | `docs-capture` |
| Planning thresholds | `planning-thresholds-region`, capacity field, or chrome error | `docs-capture` (`planning/repos.png`) |
| Repository participation summary | `planning-participation-summary` | `docs-capture` |
| Included repositories table or empty state | `planning-included-table` / `planning-no-included-text` | `docs-capture` |
| Excluded repositories and quick exclude | `planning-exclusions-region`, exclude autocomplete | `docs-capture` |
| Per-repository summary table, empty state, load error, or partial failure | `planning-repository-summary-table` / `planning-repository-summary-empty` / `planning-repository-summary-error` / `planning-repository-summary-partial-failure` | `docs-capture` |
| Backlog Review filters, urgency panels, empty copy, warning, or catalogue error | `planning.spec.ts` `Planning` describe — `planning-backlog-filters` / `planning-backlog-panels` / empty or error copy, or chrome error | `docs-capture` (`planning/backlog.png`) |
| Awaiting triage, epics near completion, and neglected repositories | `planning.spec.ts` `Backlog Review` describe — `planning-backlog-awaiting-triage`, `planning-backlog-epics`, and `planning-backlog-neglected` when panels load | `docs-capture` |
| Iteration Planning Up Next, candidates, empty copy, warning, or catalogue error | `planning.spec.ts` `Iteration Planning` describe — `planning-planning-up-next` / `planning-planning-candidates` / empty or error copy, or chrome error | `docs-capture` (`planning/iteration.png`) |
| Capacity, stalled gate, and bulk milestone | `PlanningIterationTests` (bUnit); CI shell asserts capacity region and conditional stall gate | `docs-capture` |

### Iteration Planning stall versus capacity automated coverage ([#458](https://github.com/markheydon/solo-dev-board/issues/458))

Issue [#458](https://github.com/markheydon/solo-dev-board/issues/458) closes the test slice for story [#445](https://github.com/markheydon/solo-dev-board/issues/445) (separate stall gate from capacity). Keep this mapping aligned with [planning.md](../../website/content/docs/planning.md).

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Stalled Up Next disables Add; error stall alert without capacity wording; candidate pause line | `PlanningIterationTests` stall gate describe | `planning.spec.ts` conditional stall gate and pause line when loaded |
| Capacity at limit keeps Add enabled; exceed-capacity confirm still shown | `PlanningIterationTests` at-capacity describe | — |
| Under capacity without stall shows no stall alert and enabled Add | `PlanningIterationTests.PlanningIteration_WhenUnderCapacityWithoutStall_DoesNotShowStallGateAlertAndEnablesAdd` | — |
| Soft capacity copy in Up Next does not require resolving items before add | `PlanningIterationTests.PlanningIteration_WhenAtCapacityWithoutStall_ShowsSoftCapacityStatusInUpNext` | — |

Placeholder-auth CI usually loads Iteration Planning without a stalled Up Next item, so stall-gate alert and pause-line assertions in `planning.spec.ts` are conditional when a loaded board happens to show stalled work. Populated stall UI and disable-Add behaviour are covered in `PlanningIterationTests` (bUnit). PAT docs-capture can validate loaded stall screenshots when needed.

| Board setup compatibility tab, chrome summary, and Recheck | `PlanningBoardSetupTests` (bUnit); Playwright shell in board setup work | `docs-capture` (future) |

### Daily Focus automated coverage ([#385](https://github.com/markheydon/solo-dev-board/issues/385))

Issue [#385](https://github.com/markheydon/solo-dev-board/issues/385) closes the Daily Focus test slice for stories [#273](https://github.com/markheydon/solo-dev-board/issues/273)–[#276](https://github.com/markheydon/solo-dev-board/issues/276). Keep this mapping alongside the Planning rows above.

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Column counts and active load (Up Next + In Progress) | `DailyFocusBoardStateMapperTests`, `DailyFocusBoardStateServiceTests` | `planning.spec.ts` `Daily Focus` describe — occupancy region or empty/error copy |
| Inclusive 3-day Up Next stall | `DailyFocusBoardStateMapperTests` | `planning.spec.ts` `Planning` describe — `planning-daily-focus-stalled` when occupancy loads |
| Top-three priority ranking | `DailyFocusRecommendationMapperTests`, `DailyFocusRecommendationServiceTests` | `planning.spec.ts` `Planning` describe — recommendations region or error/warning |
| Stalled review pull requests | `DailyFocusStalledReviewDetectorTests`, `DailyFocusStalledReviewServiceTests` | `planning.spec.ts` `Planning` describe — `planning-daily-focus-stalled-reviews` or error |
| Route shell, loading, empty board, GitHub retry, inaccessible-board warning | `PlanningDailyFocusTests` | `planning.spec.ts` `Daily Focus` describe — route shell and no-board/empty/error copy |

Docs-capture screenshots for all four Planning tabs live under `website/static/images/planning/` (`daily-focus.png`, `backlog.png`, `planning.png`, `repos.png`). Capture prefers the public **SoloDevBoard Roadmap** board with `DocsCapture:Enabled=true`.

### Repo Management automated coverage ([#388](https://github.com/markheydon/solo-dev-board/issues/388))

Issue [#388](https://github.com/markheydon/solo-dev-board/issues/388) closes the Repo Management test slice for stories [#287](https://github.com/markheydon/solo-dev-board/issues/287) and [#288](https://github.com/markheydon/solo-dev-board/issues/288). Keep this mapping alongside the Planning rows above.

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Planning thresholds (capacity, stall days, neglect days) | `PlanningSettingsDefaultsTests`, `PlanningSettingsServiceTests` | `planning.spec.ts` `Repo Management` describe — threshold fields and regions |
| Repository participation and exclusions | `PlanningReposTests` | `planning.spec.ts` `Repo Management` describe — participation summary, included table, exclusions |
| Per-repository summary table, empty state, load error, or partial failure | `PlanningReposTests` | `planning.spec.ts` `Repo Management` and `Planning` describes — summary region or error/empty/loading |
| Route shell and no-board instructional copy | `PlanningReposTests` | `planning.spec.ts` `Repo Management` describe — route shell and board selector alert |

### Backlog Review automated coverage ([#386](https://github.com/markheydon/solo-dev-board/issues/386))

Issue [#386](https://github.com/markheydon/solo-dev-board/issues/386) closes the Backlog Review test slice for stories [#280](https://github.com/markheydon/solo-dev-board/issues/280)–[#279](https://github.com/markheydon/solo-dev-board/issues/279) (parent feature [#277](https://github.com/markheydon/solo-dev-board/issues/277), delivered in PR [#419](https://github.com/markheydon/solo-dev-board/pull/419)). Keep this mapping alongside the Planning rows above.

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Urgent, Ready to start, and Blocked/deferred panels | `BacklogReviewGroupingTests`, `BacklogReviewServiceTests` | `planning.spec.ts` `Planning` and `Backlog Review` describes — urgency panel `data-testid`s |
| Awaiting triage (missing `type/` or `priority/`) | `BacklogReviewGroupingTests` | `planning.spec.ts` `Backlog Review` describe — `planning-backlog-awaiting-triage` |
| Urgent items deduplicated from Ready to start | `BacklogReviewGroupingTests` | Unit/component only (row membership) |
| Issue versus pull request kind chips | `PlanningBacklogTests` | Unit/component only (row chips); `docs-capture` when table rows render |
| Epics near completion | `BacklogReviewGroupingTests`, `PlanningBacklogTests` | `planning.spec.ts` `Backlog Review` describe — `planning-backlog-epics` panel or empty/unavailable copy |
| Neglected repositories | `BacklogReviewGroupingTests`, `PlanningBacklogTests` | `planning.spec.ts` `Backlog Review` describe — `planning-backlog-neglected` panel or empty copy |
| Route shell, no-board copy, catalogue empty, filter empty, partial-failure warning, GitHub retry | `PlanningBacklogTests` | `planning.spec.ts` `Backlog Review` describe — route shell and no-board/empty/error/warning copy |

### Iteration Planning automated coverage ([#387](https://github.com/markheydon/solo-dev-board/issues/387))

Issue [#387](https://github.com/markheydon/solo-dev-board/issues/387) closes the Iteration Planning test slice for stories [#283](https://github.com/markheydon/solo-dev-board/issues/283)–[#286](https://github.com/markheydon/solo-dev-board/issues/286). Keep this mapping alongside the Planning rows above.

| Behaviour | Unit / component | Playwright (CI placeholder auth) |
|-----------|------------------|----------------------------------|
| Focus Order sequencing for story, enabler, and test labels | `PlanningFocusOrderSequencerTests` | Unit/component only |
| Up Next and candidate mapping | `IterationPlanningViewMapperTests` | Unit/component only |
| Add to Up Next writes (status, triage add, Focus Order) | `IterationPlanningServiceTests` | Unit/component only |
| Active load capacity flag and at-capacity warning | `IterationPlanningViewMapperTests`, `PlanningIterationTests` | Unit/component only |
| Capacity exceeded confirmation dialog before add | `PlanningIterationTests` | Unit/component only |
| Stalled Up Next gate disables add | `PlanningIterationTests` | Unit/component only |
| Bulk milestone skip when milestone missing on a repository | `PlanningBulkMilestoneAssignerTests`, `IterationPlanningServiceTests` | Unit/component only |
| Add to Up Next success and failure snackbars | `PlanningIterationTests` | Unit/component only |
| Route shell, no-board copy, Up Next and candidate regions, empty copy, load error, partial failure | `PlanningIterationTests` | `planning.spec.ts` `Iteration Planning` describe — route shell and no-board/empty/error/warning copy |

See [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md) Tier 2 — Planning row — for the journey-level summary.

## Screenshot hygiene

- Use **light theme** at **1400×900** viewport with kebab-case PNG filenames under `website/static/images/<feature-slug>/`.
- Capture with `DocsCapture:Enabled=true` and the example repository `markheydon/solo-dev-board`.
- Never commit private repositories, tokens, or confidential project boards.

## When guides or journeys change

1. Update the User Guide page under `website/content/docs/`.
2. Update `website/content/_index.md` and `website/content/docs/_index.md` when the published feature list changes.
3. Update the relevant Playwright spec(s) and this file.
4. Refresh docs-capture screenshots when the UI changes materially.
