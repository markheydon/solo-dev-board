# Accessibility audit — WCAG 2.1 AA

This document records the WCAG 2.1 AA accessibility audit for SoloDevBoard primary journeys (issue [#253](https://github.com/markheydon/solo-dev-board/issues/253)).

## Scope

| Included | Excluded |
|----------|----------|
| Tier 1 and Tier 2 journeys from [CRITICAL_JOURNEYS.md](../tests/E2E/CRITICAL_JOURNEYS.md) | Hosted sign-in / admission-control denial (CI PAT mode) |
| Application shell (app bar, drawer, skip link, theme) | Live GitHub catalogue mutations |
| PAT connectivity error page | Azure Container Apps probe UI |
| Light-mode default theme (axe in CI) | Manual screen-reader walkthrough of every dialog |

Standard: **WCAG 2.1 Level A and AA**, automated with [axe-core](https://github.com/dequelabs/axe-core) via `@axe-core/playwright`.

## Method

1. Run axe against each audited route with tags `wcag2a`, `wcag2aa`, `wcag21a`, and `wcag21aa`.
2. Treat **critical** and **serious** confirmed violations as blocking.
3. Exclude transient `.mud-snackbar` overlays from page scans (snackbars animate and can produce false colour-contrast failures mid-transition).
4. Keep regression coverage in `tests/E2E/tests/accessibility.spec.ts` (CI `e2e` job).

## Routes audited

| Route | Journey |
|-------|---------|
| `/` | Home dashboard |
| `/about` | About |
| `/auth/connectivity-error?reason=token-rejected` | PAT connectivity error |
| `/audit-dashboard` | Audit Dashboard shell |
| `/repositories` | Repositories shell |
| `/migrate` | One-Click Migration shell |
| `/labels` | Label Manager shell |
| `/board-rules` | Board Rules shell |
| `/triage` | Triage shell |
| `/workflows` | Workflow Templates shell |

## Findings and remediation

### Critical / serious — fixed

| Finding | Impact | Remediation |
|---------|--------|-------------|
| Shell icon buttons without accessible names (drawer toggle, dark mode) | Serious (button-name risk) | Added `aria-label` on `MudIconButton` controls in `MainLayout.razor`. |
| No skip link to main content | Serious (keyboard / landmark) | Added “Skip to main content” link targeting `#main-content`, with minimal `.razor.css` (MudBlazor has no skip-link primitive). |
| Insufficient colour contrast on Primary / Secondary / Success / Warning in light mode | Serious (`color-contrast`) | Retuned `SoloDevBoardTheme` light palette to WCAG AA ratios (for example Primary `#167c38`, Secondary `#0969da`, Warning `#9a6700`) and set explicit contrast text colours. |
| Dark-mode filled controls risked light-on-light text | Serious | Bright dark palette accents use dark contrast text (`#0d1117`). |
| Dark-mode error text on surface backgrounds below 4.5:1 | Serious (`color-contrast`) | Brightened dark `Error` to `#ff7b72` for WCAG AA on `#161b22` surfaces. |
| Triage repository load failure duplicated inline alert with an error snackbar | Serious (snackbar contrast during animation) | Rely on the existing `MudAlert` / `operationMessage` region; removed redundant snackbars across all Triage error paths. |

### Residual / incomplete (not blocking)

Axe reported **incomplete** (needs review) items after remediation — no remaining confirmed critical/serious violations on the audited shells:

| Rule | Notes |
|------|-------|
| `aria-prohibited-attr` (incomplete) | Typically MudBlazor structural nodes; revisit on MudBlazor upgrades. |
| `color-contrast` (incomplete) | Axe could not resolve contrast on some dynamic/overlay nodes; light theme fixed for confirmed cases. |
| `aria-valid-attr-value` (incomplete on `/migrate`) | Needs manual review against MudBlazor select/checkbox markup. |
| MudBlazor snackbars | Page audits exclude `.mud-snackbar` to avoid animation false positives. Snackbars use the outlined variant with zero transition duration (`Program.cs`) and are regression-tested in isolation via `accessibility.spec.ts` (Repositories placeholder action). Prefer inline `MudAlert` for persistent page errors. |

## Regression tests

| Spec | Asserts |
|------|---------|
| `tests/E2E/tests/accessibility.spec.ts` | Each audited route has no critical/serious axe violations in **light and dark** mode; shell exposes skip link and labelled navigation controls; warning snackbar passes an isolated axe scan. |
| `tests/E2E/fixtures/accessibility.ts` | Shared axe helpers, route list, shell-ready wait, and dark-mode toggle helper. |

## Operator notes

- Run accessibility E2E with the same host as other Playwright tests — see [tests/E2E/README.md](../tests/E2E/README.md).
- Palette changes live in `src/App/SoloDevBoard.App/Themes/SoloDevBoardTheme.cs`. Re-run the accessibility suite after theme edits.
- Dialog-heavy flows (label create/edit, board-rule detail) should be re-checked manually or with targeted axe scans when those UIs change substantially.

## Acceptance criteria mapping

| Criterion | Status |
|-----------|--------|
| Primary journeys audited | Done — Tier 1–2 routes above. |
| Critical issues logged or fixed | Done — contrast and shell labelling fixed; residuals logged here. |
