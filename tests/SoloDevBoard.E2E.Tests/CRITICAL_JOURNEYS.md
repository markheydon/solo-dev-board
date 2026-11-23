# Critical user journeys

This document defines the highest-priority SoloDevBoard user journeys covered by Playwright end-to-end tests in CI. It satisfies the journey-identification acceptance criterion for issue [#255](https://github.com/markheydon/solo-dev-board/issues/255) and complements [DEC-016](../../adr/adr-0042-formalised-testing-standard-xunit-v3-nsubstitute-playwright.md).

User guides stay accurate via the User Guide plus locking unit or bUnit tests in the same change; this file defines **CI browser journeys** only. See [USER_DOCS_ALIGNMENT.md](USER_DOCS_ALIGNMENT.md) for the guide inventory, locking checks, and screenshot hygiene.

## CI constraints

CI runs two Playwright matrix jobs in [`.github/workflows/playwright.yml`](../../.github/workflows/playwright.yml):

| Job | Auth mode | What it validates |
|-----|-----------|-------------------|
| `pat` | PAT (`HostedSignInEnabled=false`, `ci-e2e-placeholder` token) | Shell rendering, navigation, and empty or error states without live GitHub data. |
| `hosted` | Hosted sign-in (`HostedSignInEnabled=true`, placeholder GitHub App credentials) | Login gate: `/` and protected routes redirect to `/welcome`; sign-in CTA visible; Blazor circuit negotiates before authentication. |

Neither job uses live GitHub secrets. Repository-dependent PAT journeys assert empty or error states rather than live GitHub catalogue data.

For data-driven journeys against a real GitHub account, run the PAT suite locally with a valid token — see [README.md](README.md).

## Priority tiers

### Tier 1 — Application shell and entry

| Journey | Spec | What CI validates |
|---------|------|-------------------|
| Health endpoint responds before browser tests run | `SmokeTests` | `GET /health` returns `Healthy`. |
| Home dashboard renders and lists all feature entry points | `NavigationTests`, `SmokeTests` | Title, navigation shell, and all eight feature cards. |
| Drawer navigation reaches every primary feature route | `NavigationTests` | URL and page title for each route in `fixtures/navigation.ts`. |
| Appearance theme control cycles modes and persists preference | `AppearanceTests` | Automatic → Light → Dark cycle and browser storage persistence per [Appearance](../../website/content/docs/app-shell/appearance.md). |
| About page shows deployment metadata | `AboutTests` | Version, auth mode, and repository link from the shell menu. |
| PAT-mode welcome redirect and connectivity error page | `AuthEntryTests` | `/welcome` redirects to home; connectivity error page renders guidance. |
| Hosted sign-in login gate (unauthenticated redirect and welcome landing) | `AuthEntryHostedTests` | `/` and protected routes redirect to `/welcome`; sign-in CTA visible; Blazor negotiate succeeds. |
| WCAG 2.1 AA shell and route audit (no critical/serious axe violations) | `AccessibilityTests` | axe-core scan of Tier 1–2 routes in light and dark mode; skip link, labelled shell controls, and isolated snackbar scan. See [plan/ACCESSIBILITY_AUDIT.md](../../plan/ACCESSIBILITY_AUDIT.md). |

### Tier 2 — Feature shells without live GitHub data

| Journey | Spec | What CI validates |
|---------|------|-------------------|
| Audit Dashboard repository selector failure | `AuditDashboardTests` | Feedback region and unable-to-load message. |
| Repositories | `RepositoriesTests` | Refresh control, search field, catalogue filter toggle group, error state with retry, and no horizontal overflow at 390px. |
| One-Click Migration setup shell | `MigrateTests` | Workflow controls, columns scope switch, nested ignore `area/*` control (default on), Overwrite keep `area/*` control when repositories load, disabled preview, and API failure feedback. |
| Board Rules selector, compare mode, and Reload from GitHub | `BoardRulesTests` | Selector region, compare toggle, Reload from GitHub when the catalogue loads (placeholder auth keeps repository error retry), and repository load failure. Compare-mode reload and unsupported-board Try again keep-selection are covered in bUnit. |
| Label Manager taxonomy tabs, bulk delete shell, keep `area/*` controls, remap wireframe controls, and repository load failure feedback | `LabelsTests` | Tab strip, disabled bulk delete, nested keep `area/*` checkbox on Recommended taxonomy when remove-outside is enabled, Synchronise keep control, repository load failure in the feedback region, and remap wireframe `data-testid`s (`labels-recommended-remap-table`, `labels-recommended-remap-apply-button`) when preview is available. Remap destination pickers, keep-label behaviour, delete-without-remap confirmation, and populated remap apply summaries are covered by `LabelsTests` and `docs-capture`, not CI placeholder auth. |
| Workflow template browse, filter, select, and custom source shell | `ActionsTemplatesTests` | Built-in templates load with an empty custom source; custom source region and manual field shells; invalid custom source error leaves built-ins visible; apply repository selector shows error ([#499](https://github.com/markheydon/solo-dev-board/issues/499)). |
| Triage shell, Reload from GitHub, and disposition action surface | `TriageTests` | Session-scope reload control, inline repository load failure with retry, and disposition controls hidden before a session starts. |
| Planning — Daily Focus, Backlog Review, Iteration, and Repo Management shells | `PlanningTests` | Shared chrome, Daily Focus occupancy, recommendations, stalled Up Next, and stalled-review regions or empty/error/warning copy, Backlog Review filters and all grouping panels (urgent, ready, awaiting triage, blocked, epics near completion, neglected repositories) or empty/error/warning copy, Iteration Planning Up Next, capacity region, conditional stall gate and pause line, and candidate regions or empty/error/warning copy, Repos tab thresholds, exclusions, and per-repository summary or chrome error with retry. Dedicated `Daily Focus`, `Backlog Review`, `Iteration Planning`, and `Repo Management` describe blocks assert route shells and tab-specific no-board/empty/error states. |

### Tier 3 — Out of CI scope (manual or future)

| Journey | Rationale |
|---------|-----------|
| Live GitHub OAuth sign-in and post-login feature journeys | Requires real GitHub App credentials and an interactive OAuth flow; validated manually on staging. |
| Admission-control denial after sign-in | Requires an authenticated session with a disallowed GitHub identity. |
| Live GitHub catalogue mutations (labels, migration apply, workflow deploy) | Requires a valid token and test repositories; covered by unit and component tests. |
| Azure Container Apps probe behaviour | Validated at deploy time; see [OPERATIONAL_HARDENING_TEST_COVERAGE.md](../../plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md). |

## Adding a new journey

1. **Behaviour-only changes:** update the User Guide and locking unit or bUnit tests; do not add new Tier 2 feature-shell specs unless shell chrome or routes change.
2. **New user-facing routes:** add Tier 1 navigation and axe coverage; update [USER_DOCS_ALIGNMENT.md](USER_DOCS_ALIGNMENT.md); add docs-capture when a screenshot is required.
3. Confirm remaining browser journeys belong in Tier 1 or Tier 2 for CI, or document them in Tier 3 with rationale.
4. Prefer accessible names; use `data-testid` when roles or labels are unstable for E2E or bUnit.
5. Update the coverage table in [README.md](README.md) and this document when CI browser coverage changes.
6. Keep assertions resilient to placeholder auth unless the CI job is extended with secrets.

## Related documentation

- [tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md](USER_DOCS_ALIGNMENT.md) - user guide accuracy inventory, locking checks, and screenshot hygiene.
- [tests/SoloDevBoard.E2E.Tests/README.md](README.md) — local run and CI overview.
- [plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md](../../plan/OPERATIONAL_HARDENING_TEST_COVERAGE.md) — health and operational E2E expectations.
- [CONTRIBUTING.md](../../CONTRIBUTING.md) — contributor testing guidance.
