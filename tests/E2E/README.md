# SoloDevBoard end-to-end tests

Playwright tests for key user journeys. These complement unit and bUnit component tests — they validate complete workflows in a real browser rather than replacing isolated unit coverage.

## Prerequisites

- Node.js 20 or later.
- A running SoloDevBoard instance (see below).

## Local run

Start the app on HTTP (plain HTTP avoids dev-certificate issues in headless environments):

```bash
ASPNETCORE_URLS=http://localhost:5080 \
  ASPNETCORE_ENVIRONMENT=Development \
  GitHubAuth__PersonalAccessToken=local-e2e-placeholder \
  GitHubAuth__OwnerLogin=local-test-user \
  HostedAdmissionControl__Enabled=false \
  dotnet run --project src/App/SoloDevBoard.App --no-launch-profile
```

In a second terminal:

```bash
cd tests/E2E
npm ci
npx playwright install --with-deps chromium
PLAYWRIGHT_BASE_URL=http://localhost:5080 npm test
```

## CI

The `e2e` job in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) starts the app with placeholder auth configuration and runs the smoke tests.
