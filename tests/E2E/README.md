# SoloDevBoard end-to-end tests

Playwright tests for key user journeys. These complement unit and bUnit component tests — they validate complete workflows in a real browser rather than replacing isolated unit coverage.

Critical journeys, priority tiers, and CI constraints are documented in [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md).

## Test coverage

| Spec | What it validates |
|------|-------------------|
| `smoke.spec.ts` | Health endpoint and home page render |
| `navigation.spec.ts` | Home feature cards and drawer navigation to all primary routes |
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
| `accessibility.spec.ts` | WCAG 2.1 AA axe-core scan of Tier 1–2 journeys in light and dark mode; labelled shell controls; isolated snackbar contrast scan |

Tests are designed to pass in CI with placeholder auth. The PAT job uses `GitHubAuth__PersonalAccessToken=ci-e2e-placeholder`. The hosted job uses placeholder GitHub App credentials and asserts the login gate without live OAuth. Repository-dependent features assert empty or error states rather than live GitHub data.

Accessibility findings and remediation notes for issue #253 live in [plan/ACCESSIBILITY_AUDIT.md](../../plan/ACCESSIBILITY_AUDIT.md).

## Prerequisites

- Node.js 20 or later.
- A running SoloDevBoard instance (see below).

## Local run

### PAT mode (default)

Start the app on HTTP (plain HTTP avoids dev-certificate issues in headless environments):

```bash
ASPNETCORE_URLS=http://localhost:5080 \
  ASPNETCORE_ENVIRONMENT=Development \
  GitHubAuth__PersonalAccessToken=local-e2e-placeholder \
  GitHubAuth__OwnerLogin=local-test-user \
  GitHubAuth__HostedSignInEnabled=false \
  HostedAdmissionControl__Enabled=false \
  dotnet run --project src/App/SoloDevBoard.App --no-launch-profile
```

In a second terminal:

```bash
cd tests/E2E
npm ci
npx playwright install --with-deps chromium
PLAYWRIGHT_BASE_URL=http://localhost:5080 E2E_AUTH_MODE=pat npm test
```

### Hosted mode (login gate only)

Start the app with hosted sign-in enabled and placeholder GitHub App credentials:

```bash
ASPNETCORE_URLS=http://localhost:5080 \
  ASPNETCORE_ENVIRONMENT=Development \
  GitHubAuth__PersonalAccessToken=- \
  GitHubAuth__HostedSignInEnabled=true \
  GitHubAuth__HostedGitHubAppClientId=ci-e2e-hosted-client-id \
  GitHubAuth__HostedGitHubAppClientSecret=ci-e2e-hosted-client-secret \
  HostedAdmissionControl__Enabled=true \
  HostedAdmissionControl__AllowedUserLogins=local-test-user \
  HostedAdmissionControl__AllowedOrganisationLogins=- \
  dotnet run --project src/App/SoloDevBoard.App --no-launch-profile
```

In a second terminal:

```bash
cd tests/E2E
PLAYWRIGHT_BASE_URL=http://localhost:5080 E2E_AUTH_MODE=hosted npx playwright test auth-entry-hosted.spec.ts
```

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

Images are written to `user-docs/static/images/<feature-slug>/`. See [DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md) for the screenshot convention and composition rules (prefer loaded states after selecting `markheydon/solo-dev-board`, not empty shells).

## CI

Two jobs in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) run Playwright against placeholder auth configuration:

- **`e2e-pat`** — full suite with PAT mode (`E2E_AUTH_MODE=pat`).
- **`e2e-hosted`** — hosted login-gate suite (`auth-entry-hosted.spec.ts`) with placeholder GitHub App credentials and no live OAuth.

Both jobs capture application logs separately from Playwright output.
