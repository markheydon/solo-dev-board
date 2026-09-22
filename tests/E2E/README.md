# SoloDevBoard end-to-end tests

C# Playwright tests (`Microsoft.Playwright.Xunit.v3`) for key user journeys. These complement unit and bUnit component tests — they validate complete workflows in a real browser rather than replacing isolated unit coverage.

Critical journeys, priority tiers, and CI constraints are documented in [CRITICAL_JOURNEYS.md](CRITICAL_JOURNEYS.md).

Published user guides must stay aligned with these tests like-for-like. See [USER_DOCS_ALIGNMENT.md](USER_DOCS_ALIGNMENT.md) for the guide-to-test inventory and screenshot confidentiality rules.

## Test coverage

| Test class | What it validates |
|------------|-------------------|
| `SmokeTests` | Health endpoint and home page render |
| `NavigationTests` | Home feature cards and drawer navigation to all primary routes |
| `AppearanceTests` | Theme control cycles Automatic → Light → Dark and persists preference |
| `AboutTests` | About page metadata via the shell menu |
| `AuthEntryTests` | PAT-mode welcome redirect and connectivity error page |
| `AuthEntryHostedTests` | Hosted-mode login gate, welcome landing, and Blazor negotiate |
| `AuditDashboardTests` | Audit Dashboard shell and repository load failure |
| `RepositoriesTests` | Repositories command strip, load failure handling, and phone-width overflow guard |
| `MigrateTests` | One-Click Migration setup shell, columns scope switch, and API failure feedback |
| `BoardRulesTests` | Board Rules selector region, compare mode, Reload from GitHub when the catalogue loads, and repository load failure |
| `LabelsTests` | Label Manager shell, tabs, and repository load failure feedback |
| `ActionsTemplatesTests` | Built-in template browse/filter/select, custom source region and manual field, and repository error state |
| `TriageTests` | Triage shell, session-scope Reload from GitHub, inline repository load failure, and disposition controls hidden before a session starts |
| `PlanningTests` | Planning — Daily Focus occupancy, recommendations, stalled Up Next, stalled-review, Backlog Review, and Iteration shells, plus Repos tab threshold, exclusion, and summary regions or chrome error |
| `AccessibilityTests` | WCAG 2.1 AA axe-core scan of Tier 1–2 journeys in light and dark mode; labelled shell controls; isolated snackbar contrast scan |

Tests are designed to pass in CI with placeholder auth. The PAT job uses `GitHubAuth__PersonalAccessToken=ci-e2e-placeholder`. The hosted job uses placeholder GitHub App credentials and asserts the login gate without live OAuth. Repository-dependent features assert empty or error states rather than live GitHub data.

Accessibility findings and remediation notes for issue #253 live in [plan/ACCESSIBILITY_AUDIT.md](../../plan/ACCESSIBILITY_AUDIT.md).

## Prerequisites

- .NET 10 SDK only (no Node.js).
- PowerShell (`pwsh`) for the first Playwright browser install after build.

## Local run

Build the solution once. The E2E assembly fixture starts SoloDevBoard on HTTP port **5080** with the same placeholder auth configuration as CI.

### Full E2E suite (PAT mode, default)

```bash
dotnet build
dotnet test tests/E2E/SoloDevBoard.E2E.Tests/SoloDevBoard.E2E.Tests.csproj --filter "Category=E2E"
```

### Fast unit/component loop (exclude E2E)

```bash
dotnet test --filter "Category!=E2E&Category!=DocsCapture"
```

### Hosted mode (login gate only)

```bash
E2E_AUTH_MODE=hosted dotnet test tests/E2E/SoloDevBoard.E2E.Tests/SoloDevBoard.E2E.Tests.csproj --filter "Category=E2E&AuthMode=Hosted"
```

### Reusing an already-running app

By default the fixture starts its own placeholder-configured instance. To point tests at an app you started manually (for example Aspire or a real PAT), set `PLAYWRIGHT_REUSE_SERVER=1` and ensure `PLAYWRIGHT_BASE_URL` matches the running instance.

### Playwright browsers

After the first build, install Chromium if prompted:

```bash
pwsh tests/E2E/SoloDevBoard.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Documentation screenshots

Manual screenshot capture for the Hugo user guide lives in `SoloDevBoard.E2E.Tests/DocsCapture/` and is **not** part of the CI suite. Tests skip unless `DOCS_CAPTURE_ENABLED=1`.

Prerequisites:

1. Run SoloDevBoard locally with a **real** GitHub PAT (not the CI placeholder).
2. Build the app project first (`dotnet build src/App/SoloDevBoard.App/SoloDevBoard.App.csproj`) so Blazor framework assets are available.
3. Start the app in **Development** on HTTP (for example `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/App/SoloDevBoard.App --no-launch-profile --no-build`).
4. Enable docs capture mode so only public repositories and public Projects v2 boards appear:

```bash
dotnet user-secrets set "DocsCapture:Enabled" "true" --project src/App/SoloDevBoard.App
```

5. Capture screenshots:

```bash
DOCS_CAPTURE_ENABLED=1 dotnet test tests/E2E/SoloDevBoard.E2E.Tests/SoloDevBoard.E2E.Tests.csproj --filter "Category=DocsCapture"
```

Images are written to `website/static/images/<feature-slug>/`. See [DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md) for the screenshot convention and composition rules (prefer loaded states after selecting `markheydon/solo-dev-board`, not empty shells).

## CI

[`.github/workflows/playwright.yml`](../../.github/workflows/playwright.yml) runs two matrix jobs in parallel with **Build and Test** in [`ci.yml`](../../.github/workflows/ci.yml):

- **`pat`** — full E2E suite with PAT mode (`E2E_AUTH_MODE=pat`, filter `Category=E2E`).
- **`hosted`** — hosted login-gate suite (`AuthEntryHostedTests`, filter `Category=E2E&AuthMode=Hosted`) with placeholder GitHub App credentials and no live OAuth.

The assembly fixture starts the app on HTTP **port 5080** (not Aspire on 5074). CI installs Chromium with `pwsh …/playwright.ps1 install chromium`. CI uploads the HTML report as a workflow artefact on every run when generated.
