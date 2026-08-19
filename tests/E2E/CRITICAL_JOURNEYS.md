# Critical user journeys

This document defines the highest-priority SoloDevBoard user journeys covered by Playwright end-to-end tests in CI. It satisfies the journey-identification acceptance criterion for issue [#255](https://github.com/markheydon/solo-dev-board/issues/255) and complements [DEC-016](../../plan/DECISIONS.md#dec-016-formalised-testing-standard--xunit-v3-nsubstitute-playwright-e2e).

Published user guides in `website/content/docs/` must map to these journeys like-for-like. See [USER_DOCS_ALIGNMENT.md](USER_DOCS_ALIGNMENT.md) for the full guide-to-spec inventory and screenshot hygiene rules.

## CI constraints

CI runs two Playwright jobs in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml):

| Job | Auth mode | What it validates |
|-----|-----------|-------------------|
| `e2e-pat` | PAT (`HostedSignInEnabled=false`, `ci-e2e-placeholder` token) | Shell rendering, navigation, and empty or error states without live GitHub data. |
| `e2e-hosted` | Hosted sign-in (`HostedSignInEnabled=true`, placeholder GitHub App credentials) | Login gate: `/` and protected routes redirect to `/welcome`; sign-in CTA visible; Blazor circuit negotiates before authentication. |

Neither job uses live GitHub secrets. Repository-dependent PAT journeys assert empty or error states rather than live GitHub catalogue data.

For data-driven journeys against a real GitHub account, run the PAT suite locally with a valid token — see [README.md](README.md).

## Priority tiers

### Tier 1 — Application shell and entry

| Journey | Spec | What CI validates |
|---------|------|-------------------|
| Health endpoint responds before browser tests run | `smoke.spec.ts` | `GET /health` returns `Healthy`. |
| Home dashboard renders and lists all feature entry points | `navigation.spec.ts`, `smoke.spec.ts` | Title, navigation shell, and all eight feature cards. |
| Drawer navigation reaches every primary feature route | `navigation.spec.ts` | URL and page title for each route in `fixtures/navigation.ts`. |
| Appearance theme control cycles modes and persists preference | `appearance.spec.ts` | Automatic → Light → Dark cycle and browser storage persistence per [Appearance](../../website/content/docs/appearance.md). |
| About page shows deployment metadata | `about.spec.ts` | Version, auth mode, and repository link from the shell menu. |
| PAT-mode welcome redirect and connectivity error page | `auth-entry.spec.ts` | `/welcome` redirects to home; connectivity error page renders guidance. |
| Hosted sign-in login gate (unauthenticated redirect and welcome landing) | `auth-entry-hosted.spec.ts` | `/` and protected routes redirect to `/welcome`; sign-in CTA visible; Blazor negotiate succeeds. |
| WCAG 2.1 AA shell and route audit (no critical/serious axe violations) | `accessibility.spec.ts` | axe-core scan of Tier 1–2 routes in light and dark mode; skip link, labelled shell controls, and isolated snackbar scan. See [plan/ACCESSIBILITY_AUDIT.md](../../plan/ACCESSIBILITY_AUDIT.md). |

### Tier 2 — Feature shells without live GitHub data

| Journey | Spec | What CI validates |
|---------|------|-------------------|
| Audit Dashboard repository selector failure | `audit-dashboard.spec.ts` | Feedback region and unable-to-load message; `/audit` alias. |
| Repositories command strip and load failure | `repositories.spec.ts` | Refresh control, search field, and error state with retry. |
| One-Click Migration setup shell | `migrate.spec.ts` | Workflow controls, disabled preview, and API failure feedback. |
| Board Rules selector and compare mode | `board-rules.spec.ts` | Selector region, compare toggle, and repository load failure. |
| Label Manager taxonomy tabs and empty repository state | `labels.spec.ts` | Tab strip, disabled actions, and no-repositories message. |
| Workflow template browse, filter, and select | `workflows.spec.ts` | Built-in templates load; repository selector shows error. |
| Triage not-started region without repositories | `triage.spec.ts` | Shell heading and no-repositories alert. |
| PM Workflow Daily Focus and Repo Management shells | `pm-workflow.spec.ts` | Shared chrome, Daily Focus occupancy or empty/error copy, Repos tab content or chrome error with retry. |

### Tier 3 — Out of CI scope (manual or future)

| Journey | Rationale |
|---------|-----------|
| Live GitHub OAuth sign-in and post-login feature journeys | Requires real GitHub App credentials and an interactive OAuth flow; validated manually on staging. |
| Admission-control denial after sign-in | Requires an authenticated session with a disallowed GitHub identity. |
| Live GitHub catalogue mutations (labels, migration apply, workflow deploy) | Requires a valid token and test repositories; covered by unit and component tests. |
| Azure Container Apps probe behaviour | Validated at deploy time; see [OPERATIONAL_HARDENING_TEST_COVERAGE.md](../../plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md). |

## Adding a new journey

1. Confirm the journey belongs in Tier 1 or Tier 2 for CI, or document it in Tier 3 with rationale.
2. Add or extend a spec under `tests/`.
3. Prefer `data-testid` attributes on loading, error, and empty states — mirror existing feature pages.
4. Update the coverage table in [README.md](README.md) and this document.
5. Keep assertions resilient to placeholder auth unless the CI job is extended with secrets.

## Related documentation

- [tests/E2E/USER_DOCS_ALIGNMENT.md](USER_DOCS_ALIGNMENT.md) — user guide to Playwright spec mapping and screenshot hygiene.
- [tests/E2E/README.md](README.md) — local run and CI overview.
- [plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md](../../plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md) — health and operational E2E expectations.
- [CONTRIBUTING.md](../../CONTRIBUTING.md) — contributor testing guidance.
