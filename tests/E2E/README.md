# SoloDevBoard end-to-end tests

Playwright tests for key user journeys. These complement unit and bUnit component tests — they validate complete workflows in a real browser rather than replacing isolated unit coverage.

Critical journeys, priority tiers, and CI constraints are documented in [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md).

Published user guides must stay aligned with these tests like-for-like. See [USER_DOCS_ALIGNMENT.md](USER_DOCS_ALIGNMENT.md) for the guide-to-spec inventory and screenshot confidentiality rules.

## Test coverage

| Spec | What it validates |
|------|-------------------|
| `smoke.spec.ts` | Health endpoint and home page render |
| `navigation.spec.ts` | Home feature cards and drawer navigation to all primary routes |
| `appearance.spec.ts` | Theme control cycles Automatic → Light → Dark and persists preference |
| `about.spec.ts` | About page metadata via the shell menu |
| `auth-entry.spec.ts` | PAT-mode welcome redirect and connectivity error page |
| `auth-entry-hosted.spec.ts` | Hosted-mode login gate, welcome landing, and Blazor negotiate |
| `audit-dashboard.spec.ts` | Audit Dashboard shell and repository load failure; `/audit` alias |
| `repositories.spec.ts` | Repositories command strip and load failure handling |
| `migrate.spec.ts` | One-Click Migration setup shell and API failure feedback |
| `board-rules.spec.ts` | Board Rules selector region, compare mode, and load failure |
| `labels.spec.ts` | Label Manager shell, tabs, and empty-repository state |
| `workflows.spec.ts` | Built-in template browse/filter/select and repository error state |
| `triage.spec.ts` | Triage shell and no-repositories alert without a live GitHub connection |
| `pm-workflow.spec.ts` | PM Workflow Daily Focus occupancy and recommendations shell and Repos tab threshold/exclusion regions or chrome error |
| `accessibility.spec.ts` | WCAG 2.1 AA axe-core scan of Tier 1–2 journeys in light and dark mode; labelled shell controls; isolated snackbar contrast scan |

Tests are designed to pass in CI with placeholder auth. The PAT job uses `GitHubAuth__PersonalAccessToken=ci-e2e-placeholder`. The hosted job uses placeholder GitHub App credentials and asserts the login gate without live OAuth. Repository-dependent features assert empty or error states rather than live GitHub data.

Accessibility findings and remediation notes for issue #253 live in [plan/ACCESSIBILITY_AUDIT.md](../../plan/ACCESSIBILITY_AUDIT.md).

## Prerequisites

- Node.js 20 or later.
- .NET 10 SDK (Playwright starts the app via `webServer` in [`playwright.config.ts`](playwright.config.ts)).

## Local run

Build the application once, then run Playwright from `tests/E2E`. Playwright starts SoloDevBoard with the same placeholder auth configuration as CI.

### PAT mode (default)

```bash
dotnet build src/App/SoloDevBoard.App/SoloDevBoard.App.csproj
cd tests/E2E
npm ci
npx playwright install --with-deps chromium
npm test
```

### Hosted mode (login gate only)

```bash
dotnet build src/App/SoloDevBoard.App/SoloDevBoard.App.csproj
cd tests/E2E
E2E_AUTH_MODE=hosted npx playwright test auth-entry-hosted.spec.ts
```

### Reusing an already-running app

By default Playwright starts its own placeholder-configured instance. To point tests at an app you started manually (for example Aspire or a real PAT), set `PLAYWRIGHT_REUSE_SERVER=1` and ensure `PLAYWRIGHT_BASE_URL` matches the running instance.

### Viewing the HTML report

After a run, open the report locally:

```bash
cd tests/E2E
npx playwright show-report
```

In CI, download the `playwright-report-pat` or `playwright-report-hosted` artefact from the workflow run and run `npx playwright show-report` inside the extracted folder.

## Documentation screenshots

Manual screenshot capture for the Hugo user guide lives in `docs-capture/` and is **not** part of the CI suite.

Prerequisites:

1. Run SoloDevBoard locally with a **real** GitHub PAT (not the CI placeholder).
2. Build the app project first (`dotnet build src/App/SoloDevBoard.App/SoloDevBoard.App.csproj`) so Blazor framework assets are available.
3. Start the app in **Development** on HTTP (for example `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/App/SoloDevBoard.App --no-launch-profile --no-build`).
4. Enable docs capture mode so only public repositories and public Projects v2 boards appear:

```bash
dotnet user-secrets set "DocsCapture:Enabled" "true" --project src/App/SoloDevBoard.App
```

3. Capture screenshots:

```bash
cd tests/E2E
npm run capture:docs
```

Images are written to `website/static/images/<feature-slug>/`. See [DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md) for the screenshot convention and composition rules (prefer loaded states after selecting `markheydon/solo-dev-board`, not empty shells).

## CI

[`.github/workflows/playwright.yml`](../../.github/workflows/playwright.yml) runs two matrix jobs in parallel with **Build and Test** in [`ci.yml`](../../.github/workflows/ci.yml):

- **`pat`** — full suite with PAT mode (`E2E_AUTH_MODE=pat`).
- **`hosted`** — hosted login-gate suite (`auth-entry-hosted.spec.ts`) with placeholder GitHub App credentials and no live OAuth.

Playwright starts the app via `webServer` in `playwright.config.ts` on HTTP **port 5080** (not Aspire on 5074). CI installs Chromium with `npx playwright install chromium` only — it does **not** use `--with-deps`, so GitHub-hosted runners never call `apt` for browser OS packages. Local development should still use `npx playwright install --with-deps chromium` so system libraries are present on your machine. CI uploads the HTML report as a workflow artefact on every run.
