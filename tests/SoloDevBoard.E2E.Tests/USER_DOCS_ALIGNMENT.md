# User guide accuracy inventory

This document maps published User Guide pages in `website/content/docs/` to **locking checks** (unit and bUnit), optional Playwright shell and axe coverage, and Hugo screenshot assets. It is the canonical inventory for keeping documentation accurate; feature workflows are proved in `tests/SoloDevBoard.Web.Tests` and layer unit tests, not in CI Playwright under placeholder auth.

The Hugo **product landing** at `/` on the public site is not the Blazor in-app home. Playwright asserts the in-app shell only; Hugo route checks run in `hugo-build.yml`.

## Alignment contract

Four artefacts stay in sync for every shipped end-user feature:

1. **Application behaviour** - what SoloDevBoard actually does in the UI.
2. **User guide** - what `website/content/docs/<feature>.md` tells users they can do (shipped behaviour only).
3. **Locking checks** - unit or bUnit tests updated in the same change that specify that behaviour.
4. **Optional Playwright and docs-capture** - CI shell, navigation, auth entry, layout, axe scans, and manual screenshots where listed below. E2E does **not** exercise every workflow the guide describes when CI uses placeholder auth.

Rules:

- **User guides describe real behaviour only.** Do not document planned or stubbed capabilities without a scope note. Remove or update docs when behaviour changes.
- **Locking checks are the primary regression contract** for guide accuracy. A guide section with no named locking check is documentation debt.
- **Playwright updates** follow constitution triggers: routes, navigation, auth entry, theme persistence, viewport or layout checks, axe-scanned pages, or docs-capture assets. Loaded-state workflows use locking tests and optional `DocsCapture/` with a real PAT.
- **CI uses placeholder auth.** Playwright feature specs assert shells, navigation, and empty or error states unless a job is extended with live secrets.
- **Screenshots follow public-only hygiene.** Never commit images showing private repositories, private Projects v2 boards, tokens, or other confidential information. Capture with `DocsCapture:Enabled=true` and the conventions in [plan/DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md#screenshot-convention).

When you add or change end-user behaviour, update the user guide and locking tests in the same pull request, and update this inventory row. Update Playwright or [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md) only when browser CI coverage changes.

## Coverage matrix

Locking checks are unit or bUnit classes in `tests/SoloDevBoard.Web.Tests` or layer unit test projects unless Notes say otherwise. When the same class name exists in E2E (for example `RepositoriesTests`), this column always means the Web.Tests class.

| User guide / site page | App route(s) | Locking checks | Playwright spec (CI) | Docs-capture screenshot | CI tier | Notes |
|------------|--------------|----------------|----------------------|-------------------------|---------|-------|
| [Product landing](../../website/content/_index.md) (Hugo `/` only) | - | - | - | - | - | Product landing on the public site; not the Blazor app. Hugo route check: `.github/workflows/hugo-build.yml`. |
| [In-app home dashboard](../../website/content/docs/) | `/` | `DashboardTests` | `SmokeTests`, `NavigationTests`, `AccessibilityTests` | `dashboard/home.png` | Tier 1 | In-app home lists eight feature cards; About and Appearance are reached via the app bar. |
| [Audit Dashboard](../../website/content/docs/audit-dashboard.md) | `/audit-dashboard` | `AuditTests` | `AuditDashboardTests`, `AccessibilityTests` | `audit-dashboard/overview.png` | Tier 2 | KPIs, health sections, export, and loaded catalogue behaviour: `AuditTests`. CI Playwright: shell and repository load failure. |
| [Repositories](../../website/content/docs/repositories.md) | `/repositories` | `RepositoriesTests`, `OperatorSnapshotStatusCopyTests`, `DailyOccasionServiceTests`, `OperatorSnapshotOperatorIsolationTests`, `HostedCollectionEligibilityEvaluatorTests`, `DailySnapshotCollectionPassServiceTests` | `RepositoriesTests`, `AccessibilityTests` | `repositories/overview.png` | Tier 2 | Shipped guide covers catalogue, filters, **Collect snapshot**, shared feedback status, and hosted daily schedule copy at 05:00 UTC (no schedule in PAT-only mode), including catch-up fill, unmet fill after technical failure, dequeue admission-only run rules, and no-schedule reasons when a queued entry is removed. Locking tests: open and reload do not collect, hosted schedule sentences, success and failure copy, never-run versus zero-success, in-progress polling, operator isolation, and no run history. CI Playwright: command strip, **Collect snapshot** at default and 390px widths, error retry. Recapture `repositories/overview.png` with DocsCapture when refreshing screenshots (PAT, public repos only). |
| [One-Click Migration](../../website/content/docs/one-click-migration.md) | `/migrate` | `MigrationTests` | `MigrateTests`, `AccessibilityTests` | `one-click-migration/overview.png` | Tier 2 | Migration workflows: `MigrationTests`. CI Playwright: setup shell and API failure feedback. |
| [Label Manager](../../website/content/docs/label-manager.md) | `/labels` | `LabelsTests`, `LabelServiceTests` (Application) | `LabelsTests`, `AccessibilityTests` | `label-manager/overview.png`, `label-manager/remap-extras.png` | Tier 2 | Bulk delete, remap, apply: bUnit and Application tests ([#444](https://github.com/markheydon/solo-dev-board/issues/444), [#520](https://github.com/markheydon/solo-dev-board/issues/520)). CI Playwright: tab strip and placeholder-auth error shell. |
| [Board Rules Visualiser](../../website/content/docs/board-rules-visualiser.md) | `/board-rules` | `BoardRulesTests` | `BoardRulesTests`, `AccessibilityTests` | `board-rules-visualiser/overview.png` | Tier 2 | Compare mode, reload keep-selection, unsupported-board retry: bUnit ([#437](https://github.com/markheydon/solo-dev-board/issues/437)). CI Playwright: selector shell and repository error. |
| [Triage UI](../../website/content/docs/triage-ui.md) | `/triage` | `TriageTests` | `TriageTests`, `AccessibilityTests` | `triage-ui/overview.png` | Tier 2 | Session flow and dispositions: bUnit. CI Playwright: reload control and shell before session. |
| [Actions Templates](../../website/content/docs/actions-templates.md) | `/actions-templates` | `ActionsTemplatesTests` | `ActionsTemplatesTests`, `AccessibilityTests` | `actions-templates/overview.png` | Tier 2 | Template browse, apply, custom source: bUnit ([#436](https://github.com/markheydon/solo-dev-board/issues/436), [#499](https://github.com/markheydon/solo-dev-board/issues/499)). CI Playwright: built-in catalogue shell and repository error. |
| [Planning](../../website/content/docs/planning.md) | `/planning`, `/planning/daily-focus`, `/planning/backlog`, `/planning/iteration`, `/planning/repos`, `/planning/board-setup` (conditional) | `PlanningDailyFocusTests`, `PlanningBacklogTests`, `PlanningIterationTests`, `PlanningReposTests`, `PlanningBoardSetupTests`, `PlanningChromeCoordinatorTests` | `PlanningTests`, `AccessibilityTests` | `planning/daily-focus.png`, `planning/backlog.png`, `planning/iteration.png`, `planning/repos.png` | Tier 2 | Tab workflows ([#283](https://github.com/markheydon/solo-dev-board/issues/283)–[#286](https://github.com/markheydon/solo-dev-board/issues/286)): bUnit. CI Playwright: shared shell and placeholder empty or error regions. |
| [Appearance](../../website/content/docs/app-shell/appearance.md) | App bar (all shell pages) | `MainLayoutThemeTests`, `ThemePreferenceServiceTests`, `ThemePreferenceConstantsTests` | `AppearanceTests`, `AccessibilityTests` | `appearance/theme-toggle.png` | Tier 1 | Theme cycling and persistence: bUnit; reload persistence: Playwright. |
| [About (in-app)](../../website/content/docs/app-shell/about.md) | `/about` | `AboutTests` | `AboutTests`, `AccessibilityTests` | `about/overview.png` | Tier 1 | Metadata fields: bUnit. Not the Hugo `/about/` narrative section. |

### Auth and entry journeys (operator docs, not user guide)

These journeys are documented in `docs/hosted-authentication.md` and `docs/pat-connectivity.md` rather than the published user site:

| Journey | Locking checks | Playwright spec | CI job |
|---------|----------------|-----------------|--------|
| PAT-mode welcome redirect and connectivity error page | `WelcomePageTests`, `PatConnectivityErrorPageRendererTests` | `AuthEntryTests` | `pat` |
| Hosted sign-in login gate | `WelcomePageTests`, `HostedAuthenticationRecoveryRouteTests`, middleware tests in Infrastructure | `AuthEntryHostedTests` | `hosted` |

## Screenshot hygiene

- Use **light theme** at **1400×900** viewport with kebab-case PNG filenames under `website/static/images/<feature-slug>/`.
- Capture with `DocsCapture:Enabled=true` and the example repository `markheydon/solo-dev-board`.
- Never commit private repositories, tokens, or confidential project boards.

## When guides or journeys change

1. Update the User Guide page under `website/content/docs/`.
2. Update locking unit or bUnit tests in the same pull request.
3. Update `website/content/_index.md` and `website/content/docs/_index.md` when the published feature list changes.
4. Update this inventory row (locking checks, screenshots, Playwright column if shell coverage changed).
5. Update Playwright specs and [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md) only when routes, shell chrome, auth entry, axe routes, theme, layout checks, or docs-capture change.
6. Refresh docs-capture screenshots when the UI changes materially.
