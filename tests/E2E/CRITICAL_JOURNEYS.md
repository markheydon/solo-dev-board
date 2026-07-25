# Critical user journeys

This document defines the highest-priority SoloDevBoard user journeys covered by Playwright end-to-end tests in CI. It satisfies the journey-identification acceptance criterion for issue [#255](https://github.com/markheydon/solo-dev-board/issues/255) and complements [DEC-016](../../plan/DECISIONS.md#dec-016-formalised-testing-standard--xunit-v3-nsubstitute-playwright-e2e).

## CI constraints

The `e2e` job in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) starts the app with a placeholder personal access token (`GitHubAuth__PersonalAccessToken=ci-e2e-placeholder`) and hosted admission control disabled. Tests therefore assert **shell rendering, navigation, and empty or error states** rather than live GitHub catalogue data.

For data-driven journeys against a real GitHub account, run the suite locally with a valid token — see [README.md](README.md).

## Priority tiers

### Tier 1 — Application shell and entry

| Journey | Spec | What CI validates |
|---------|------|-------------------|
| Health endpoint responds before browser tests run | `smoke.spec.ts` | `GET /health` returns `Healthy`. |
| Home dashboard renders and lists all feature entry points | `navigation.spec.ts`, `smoke.spec.ts` | Title, navigation shell, and all seven feature cards. |
| Drawer navigation reaches every primary feature route | `navigation.spec.ts` | URL and page title for each route in `fixtures/navigation.ts`. |
| About page shows deployment metadata | `about.spec.ts` | Version, auth mode, and repository link from the shell menu. |
| PAT-mode welcome redirect and connectivity error page | `auth-entry.spec.ts` | `/welcome` redirects to home; connectivity error page renders guidance. |

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

### Tier 3 — Out of CI scope (manual or future)

| Journey | Rationale |
|---------|-----------|
| Hosted sign-in and admission-control denial | CI runs PAT mode with `HostedAdmissionControl__Enabled=false`. |
| Live GitHub catalogue mutations (labels, migration apply, workflow deploy) | Requires a valid token and test repositories; covered by unit and component tests. |
| Azure Container Apps probe behaviour | Validated at deploy time; see [OPERATIONAL_HARDENING_TEST_COVERAGE.md](../../plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md). |

## Adding a new journey

1. Confirm the journey belongs in Tier 1 or Tier 2 for CI, or document it in Tier 3 with rationale.
2. Add or extend a spec under `tests/`.
3. Prefer `data-testid` attributes on loading, error, and empty states — mirror existing feature pages.
4. Update the coverage table in [README.md](README.md) and this document.
5. Keep assertions resilient to placeholder auth unless the CI job is extended with secrets.

## Related documentation

- [tests/E2E/README.md](README.md) — local run and CI overview.
- [plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md](../../plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md) — health and operational E2E expectations.
- [CONTRIBUTING.md](../../CONTRIBUTING.md) — contributor testing guidance.
