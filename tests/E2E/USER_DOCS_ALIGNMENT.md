# User guide and Playwright E2E alignment

This document maps published end-user guides in `user-docs/content/docs/` to Playwright coverage in `tests/E2E/`. It is the canonical inventory for keeping documentation, application behaviour, and automated journeys aligned like-for-like.

## Alignment contract

Three artefacts must stay in sync for every shipped end-user feature:

1. **Application behaviour** — what SoloDevBoard actually does in the UI.
2. **User guide** — what `user-docs/content/docs/<feature>.md` tells users they can do.
3. **Playwright E2E** — what `tests/E2E/tests/*.spec.ts` asserts in CI (and, where applicable, what `tests/E2E/docs-capture/` records for screenshots).

Rules:

- **User guides describe real behaviour only.** Do not document planned or stubbed capabilities without a scope note. Remove or update docs when behaviour changes.
- **E2E tests exercise what the guides claim.** Every published guide page must map to at least one Playwright spec. Assertions should target UI elements, routes, labels, and workflows described in the guide.
- **CI uses placeholder auth.** Repository-dependent journeys assert shells, navigation, and empty or error states unless a job is extended with live secrets. Loaded-state behaviour is validated via the docs-capture suite (manual, real PAT) and unit/component tests.
- **Screenshots follow public-only hygiene.** Never commit images showing private repositories, private Projects v2 boards, tokens, or other confidential information. Capture with `DocsCapture:Enabled=true` and the conventions in [plan/DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md#screenshot-convention).

When you add or change an end-user feature, update the user guide, the relevant Playwright spec(s), this mapping, and [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md) in the same pull request.

## Coverage matrix

| User guide | App route(s) | Playwright spec (CI) | Docs-capture screenshot | CI tier | Notes |
|------------|--------------|----------------------|-------------------------|---------|-------|
| [Home dashboard](https://github.com/markheydon/solo-dev-board/blob/main/user-docs/content/_index.md) (site) / in-app `/` | `/` | `smoke.spec.ts`, `navigation.spec.ts`, `accessibility.spec.ts` | `dashboard/home.png` | Tier 1 | In-app home lists seven feature cards; About and Appearance are reached via the app bar. |
| [Audit Dashboard](../../user-docs/content/docs/audit-dashboard.md) | `/audit-dashboard`, `/audit` | `audit-dashboard.spec.ts`, `accessibility.spec.ts` | `audit-dashboard/overview.png` | Tier 2 | Guide describes KPI cards, health sections, auto-refresh, and export; CI asserts shell, feedback region, and `/audit` alias with load failure. |
| [Repositories](../../user-docs/content/docs/repositories.md) | `/repositories` | `repositories.spec.ts`, `accessibility.spec.ts` | `repositories/overview.png` | Tier 2 | Guide describes command strip, search, and grid; CI asserts refresh, search, and error state with retry. |
| [One-Click Migration](../../user-docs/content/docs/one-click-migration.md) | `/migrate` | `migrate.spec.ts`, `accessibility.spec.ts` | `one-click-migration/overview.png` | Tier 2 | Guide describes preview/apply workflow and conflict strategies; CI asserts setup shell, disabled preview, and API failure feedback. |
| [Label Manager](../../user-docs/content/docs/label-manager.md) | `/labels` | `labels.spec.ts`, `accessibility.spec.ts` | `label-manager/overview.png` | Tier 2 | Guide describes three tabs and taxonomy workflows; CI asserts tab strip, disabled actions, and no-repositories message. |
| [Board Rules Visualiser](../../user-docs/content/docs/board-rules-visualiser.md) | `/board-rules` | `board-rules.spec.ts`, `accessibility.spec.ts` | `board-rules-visualiser/overview.png` | Tier 2 | Guide describes compare mode and board selection; CI asserts selector region, compare toggle, and load failure. |
| [Triage UI](../../user-docs/content/docs/triage-ui.md) | `/triage` | `triage.spec.ts`, `accessibility.spec.ts` | `triage-ui/overview.png` | Tier 2 | Guide describes session queue, shortcuts, milestones, and project board actions; CI asserts not-started region and no-repositories alert. |
| [Workflow Templates](../../user-docs/content/docs/workflow-templates.md) | `/workflows` | `workflows.spec.ts`, `accessibility.spec.ts` | `workflow-templates/overview.png` | Tier 2 | Guide describes browse, parameterise, apply, and drift; CI asserts built-in template browse/filter/select and repository error state. |
| [Appearance](../../user-docs/content/docs/appearance.md) | App bar (all shell pages) | `appearance.spec.ts`, `accessibility.spec.ts` | `appearance/theme-toggle.png` | Tier 1 | Guide describes Automatic → Light → Dark cycling and persistence; not a drawer route. |
| [About](../../user-docs/content/docs/about.md) | `/about` | `about.spec.ts`, `accessibility.spec.ts` | `about/overview.png` | Tier 1 | Guide describes More options menu access and metadata fields. |
| Cross-Repo PM Workflow (`draft: true`) | — | — | — | — | Not published; excluded until Phase 5. |

### Auth and entry journeys (operator docs, not user guide)

These journeys are documented in `docs/hosted-authentication.md` and `docs/pat-connectivity.md` rather than the published user site:

| Journey | Playwright spec | CI job |
|---------|-----------------|--------|
| PAT-mode welcome redirect and connectivity error page | `auth-entry.spec.ts` | `e2e-pat` |
| Hosted sign-in login gate | `auth-entry-hosted.spec.ts` | `e2e-hosted` |

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
| More options → About | `about.spec.ts` navigation |
| Version, build, .NET, auth mode, login, repository link | `about.spec.ts` `data-testid` fields |

## Screenshot and confidentiality

Documentation screenshots are **not** part of the CI Playwright suite. They are captured manually via `tests/E2E/docs-capture/`:

```bash
# Prerequisites: real PAT, DocsCapture:Enabled=true, app running locally
cd tests/E2E
npm run capture:docs
```

Mandatory hygiene (see [DEC-020](../../plan/DECISIONS.md#dec-020-public-only-docs-capture-mode-for-documentation-screenshots) and [Docs capture mode](../../docs/getting-started.md#docs-capture-mode)):

- Enable `DocsCapture:Enabled=true` so only **public** repositories and **public** Projects v2 boards appear.
- Use **`markheydon/solo-dev-board`** as the canonical example repository.
- Prefer **read-only** interactions (load, browse, inspect). Do not apply migrations, synchronise labels, close issues, or write workflow files for screenshots.
- Use **light theme** at **1400×900** viewport with kebab-case PNG filenames under `user-docs/static/images/<feature-slug>/`.
- **Do not commit** screenshots showing private repositories, private project boards, personal tokens, or other confidential information.

Docs capture mode is screenshot hygiene, not a security boundary. Leave it disabled for normal development and all hosted deployments.

## Adding or updating a feature

1. Implement or change the feature in the application layer.
2. Update `user-docs/content/docs/<feature>.md` and site indexes if the feature is user-facing.
3. Add or extend `tests/E2E/tests/<feature>.spec.ts` to assert guide-described routes, controls, and CI-appropriate states.
4. Extend `tests/E2E/docs-capture/` when the guide needs new or refreshed screenshots.
5. Update this file, [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md), and [README.md](README.md) coverage tables.
6. Run the PAT E2E suite locally before merging.

## Related documentation

- [tests/E2E/CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md) — priority tiers and CI constraints.
- [tests/E2E/README.md](README.md) — local run and screenshot capture.
- [plan/DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md) — documentation layers and screenshot convention.
- [AGENTS.md](../../AGENTS.md) — constitution rules for docs ↔ E2E alignment.
