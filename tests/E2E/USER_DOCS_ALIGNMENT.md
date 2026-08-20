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
| [Audit Dashboard](../../website/content/docs/audit-dashboard.md) | `/audit-dashboard`, `/audit` | `audit-dashboard.spec.ts`, `accessibility.spec.ts` | `audit-dashboard/overview.png` | Tier 2 | Guide describes KPI cards, health sections, auto-refresh, and export; CI asserts shell, feedback region, and `/audit` alias with load failure. |
| [Repositories](../../website/content/docs/repositories.md) | `/repositories` | `repositories.spec.ts`, `accessibility.spec.ts` | `repositories/overview.png` | Tier 2 | Guide describes command strip, search, and grid; CI asserts refresh, search, and error state with retry. |
| [One-Click Migration](../../website/content/docs/one-click-migration.md) | `/migrate` | `migrate.spec.ts`, `accessibility.spec.ts` | `one-click-migration/overview.png` | Tier 2 | Guide describes preview/apply workflow and conflict strategies; CI asserts setup shell, disabled preview, and API failure feedback. |
| [Label Manager](../../website/content/docs/label-manager.md) | `/labels` | `labels.spec.ts`, `accessibility.spec.ts` | `label-manager/overview.png` | Tier 2 | Guide describes three tabs and taxonomy workflows; CI asserts tab strip, disabled actions, and no-repositories message. |
| [Board Rules Visualiser](../../website/content/docs/board-rules-visualiser.md) | `/board-rules` | `board-rules.spec.ts`, `accessibility.spec.ts` | `board-rules-visualiser/overview.png` | Tier 2 | Guide describes compare mode and board selection; CI asserts selector region, compare toggle, and load failure. |
| [Triage UI](../../website/content/docs/triage-ui.md) | `/triage` | `triage.spec.ts`, `accessibility.spec.ts` | `triage-ui/overview.png` | Tier 2 | Guide describes session queue, shortcuts, milestones, and project board actions; CI asserts not-started region and no-repositories alert. |
| [Workflow Templates](../../website/content/docs/workflow-templates.md) | `/workflows` | `workflows.spec.ts`, `accessibility.spec.ts` | `workflow-templates/overview.png` | Tier 2 | Guide describes browse, parameterise, apply, and drift; CI asserts built-in template browse/filter/select and repository error state. |
| [Appearance](../../website/content/docs/appearance.md) | App bar (all shell pages) | `appearance.spec.ts`, `accessibility.spec.ts` | `appearance/theme-toggle.png` | Tier 1 | Guide describes Automatic → Light → Dark cycling and persistence; not a drawer route. |
| [About (in-app)](../../website/content/docs/about.md) | `/about` | `about.spec.ts`, `accessibility.spec.ts` | `about/overview.png` | Tier 1 | Guide describes More options menu access (User Guide external link and About route) and metadata fields. Not the Hugo `/about/` narrative section. |
| Cross-Repo PM Workflow (`draft: true`, partial) | `/pm-workflow`, `/pm-workflow/daily-focus`, `/pm-workflow/repos` | `pm-workflow.spec.ts`, `accessibility.spec.ts` | `pm-workflow/daily-focus.png` and `pm-workflow/repos.png` (pending) | Tier 2 | Draft guide documents Daily Focus occupancy, recommendations, stalled Up Next alerts, and Repo Management ([#273](https://github.com/markheydon/solo-dev-board/issues/273), [#274](https://github.com/markheydon/solo-dev-board/issues/274), [#275](https://github.com/markheydon/solo-dev-board/issues/275), [#287](https://github.com/markheydon/solo-dev-board/issues/287), [#288](https://github.com/markheydon/solo-dev-board/issues/288)); Backlog and Planning remain placeholders. CI asserts shell, tab strip, Daily Focus occupancy, recommendation, and stalled regions or empty/error/warning copy, and Repos regions (including per-repository summary) or chrome error. |

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
| KPI cards, health sections, auto-refresh, export | — | `docs-capture` + unit/component tests |

### Label Manager

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Three tabs (Labels, Recommended taxonomy, Synchronise) | Tab strip and headings | `docs-capture` |
| No repositories message | Empty-state text | — |

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

### Cross-Repo PM Workflow (partial — Daily Focus occupancy, recommendations, stalled Up Next, and Repo Management)

| Guide section | CI assertion | Loaded-state validation |
|---------------|--------------|-------------------------|
| Drawer or Home → PM Workflow → Daily Focus | `pm-workflow.spec.ts` URL, title, shell, tab strip | `docs-capture` (pending) |
| Planning board selector, status line, and refresh | `pm-workflow.spec.ts` shared chrome `data-testid`s | `docs-capture` (pending) |
| Occupancy chips, active load, empty board, or catalogue error | `pm-workflow-daily-focus-board-state` / empty or error copy, or chrome error | `docs-capture` (pending) |
| Stalled Up Next rows, none-stalled sentence, or catalogue error | `pm-workflow-daily-focus-stalled` when occupancy region is visible | `docs-capture` (pending) |
| Recommended today (all included repositories) list, empty copy, or catalogue error | `pm-workflow-daily-focus-recommendations` heading, empty or error copy when occupancy loaded | `docs-capture` (pending) |
| Planning thresholds | `pm-workflow-thresholds-region`, capacity field, or chrome error | `docs-capture` (pending) |
| Repository participation summary | `pm-workflow-participation-summary` | `docs-capture` (pending) |
| Included repositories table or empty state | `pm-workflow-included-table` / `pm-workflow-no-included-text` | `docs-capture` (pending) |
| Excluded repositories and quick exclude | `pm-workflow-exclusions-region`, exclude autocomplete | `docs-capture` (pending) |
| Per-repository summary table, empty state, load error, or partial failure | `pm-workflow-repository-summary-table` / `pm-workflow-repository-summary-empty` / `pm-workflow-repository-summary-error` / `pm-workflow-repository-summary-partial-failure` | `docs-capture` (pending) |
| Daily Focus stalled review PRs, Backlog, Planning | — | Not shipped |

## Screenshot hygiene

- Use **light theme** at **1400×900** viewport with kebab-case PNG filenames under `website/static/images/<feature-slug>/`.
- Capture with `DocsCapture:Enabled=true` and the example repository `markheydon/solo-dev-board`.
- Never commit private repositories, tokens, or confidential project boards.

## When guides or journeys change

1. Update the User Guide page under `website/content/docs/`.
2. Update `website/content/_index.md` and `website/content/docs/_index.md` when the published feature list changes.
3. Update the relevant Playwright spec(s) and this file.
4. Refresh docs-capture screenshots when the UI changes materially.
