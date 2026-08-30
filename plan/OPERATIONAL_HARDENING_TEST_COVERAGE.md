# Operational Hardening Test Coverage

This document defines the automated validation expectations for the production-readiness operational hardening tranche delivered in issues [#106](https://github.com/markheydon/solo-dev-board/issues/106)–[#108](https://github.com/markheydon/solo-dev-board/issues/108) and tracked by test issue [#110](https://github.com/markheydon/solo-dev-board/issues/110).

The scope covers response caching, health endpoints and hosting probes, structured logging and telemetry redaction, and proportionate CI validation. It deliberately excludes the separate authentication tranche (see [HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md](HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md) for issue [#114](https://github.com/markheydon/solo-dev-board/issues/114)).

---

## Test Coverage Expectations (Issue #110)

### GitHub API response caching ([#108](https://github.com/markheydon/solo-dev-board/issues/108), [DEC-018](DECISIONS.md#dec-018-github-api-response-caching-in-infrastructure))

| Scenario | Test layer | Location |
|---|---|---|
| Cache hit on repeated read-heavy catalogue calls (repositories, labels, milestones) | Unit (mocked HTTP) | `tests/Infrastructure.Tests/.../GitHubApiCachingTests.cs` |
| Cache key scoping by current user owner login | Unit | `GitHubApiCachingTests.cs`, `GitHubResponseCacheTests.cs` |
| Case-insensitive owner/repo key normalisation | Unit | `GitHubApiCachingTests.cs` |
| Defensive copy of cached catalogues returned to callers | Unit | `GitHubApiCachingTests.cs`, `GitHubResponseCacheTests.cs` |
| Shared cache key between repository and service collaborators | Unit | `GitHubApiCachingTests.cs` |
| Label and milestone catalogue invalidation after create, update, and delete mutations | Unit | `GitHubApiCachingTests.cs`, `GitHubResponseCacheTests.cs` |
| TTL expiry and refetch after configured lifetime (labels and milestones) | Unit | `GitHubResponseCacheTests.cs` |
| Invalid TTL configuration rejected at startup | Unit | `GitHubCacheOptionsValidatorTests.cs`, `InfrastructureServiceExtensionsTests.cs` |
| Invalid pagination configuration rejected at startup | Unit | `GitHubPaginationOptionsValidatorTests.cs`, `InfrastructureServiceExtensionsTests.cs` |
| `GitHubResponseCache` registered in DI composition root | Unit | `InfrastructureServiceExtensionsTests.cs`, `Composition.Tests/.../SoloDevBoardServiceCollectionExtensionsTests.cs` |
| App project does not reference Infrastructure (architecture guard) | Unit | `Composition.Tests/.../AppArchitectureGuardTests.cs` |

Out of scope for this tranche: caching issues, pull requests, workflow runs, GraphQL project board queries, distributed cache, and Application-layer DTO caching (see DEC-018).

### GitHub API performance review ([#254](https://github.com/markheydon/solo-dev-board/issues/254))

| Scenario | Test layer | Location |
|---|---|---|
| Audit dashboard snapshot fetches issues, pull requests, and workflow runs once per repository | Unit (mocked `IGitHubService`) | `tests/Application.Tests/.../AuditDashboardServiceTests.cs` |
| Snapshot derives summary counters and health-indicator lists from a single fetch | Unit | `AuditDashboardServiceTests.cs` |
| Workflow runs paginate via `Link: rel="next"` headers | Unit (mocked HTTP) | `tests/Infrastructure.Tests/.../GitHubServiceTests.cs` |
| Workflow run pagination honours configured max page limit | Unit (mocked HTTP) | `GitHubServiceTests.cs` |
| Audit dashboard snapshot returns immutable collections | Unit | `AuditDashboardServiceTests.cs` |
| Audit dashboard snapshot skips unavailable repository resources and continues | Unit | `AuditDashboardServiceTests.cs` |
| Audit page loads dashboard data via snapshot API | Component (bUnit) | `tests/App.Tests/.../AuditTests.cs` |

Volatile-data Infrastructure caching (issues, pull requests, workflow runs) remains out of scope for V1. GraphQL project board `first: 50` limits are documented in [getting-started.md](../docs/getting-started.md#github-api-performance).

### Health checks and hosting configuration ([#106](https://github.com/markheydon/solo-dev-board/issues/106))

| Scenario | Test layer | Location |
|---|---|---|
| Readiness (`/health`) and liveness (`/alive`) endpoints return healthy in Development and Production | Unit (in-process host) | `tests/ServiceDefaults.Tests/.../HealthEndpointTests.cs` |
| Liveness excludes non-`live` health checks while readiness includes all checks | Unit | `HealthEndpointTests.cs` |
| GitHub PAT connectivity health check: valid PAT, invalid PAT, E2E placeholder skip | Unit | `tests/Infrastructure.Tests/.../GitHubPatConnectivityHealthCheckTests.cs` |
| Hosted sign-in mode skips PAT connectivity probe | Unit | `GitHubPatConnectivityHealthCheckTests.cs` |
| Health endpoints bypass hosted admission control middleware | Unit | `HostedAdmissionControlMiddlewareTests.cs` |
| E2E smoke: `/health` returns `Healthy` before browser tests run | E2E | `tests/E2E/tests/smoke.spec.ts`, `.github/workflows/playwright.yml` |

Hosting probe wiring (`.WithHttpHealthCheck("/health")` on the AppHost `app` resource) is validated by manual deploy verification and operator documentation in [docs/deployment.md](../docs/deployment.md). AppHost orchestration modelling is not unit-tested per repository policy.

`/health/github` is registered in `SoloDevBoard.App` for optional PAT connectivity monitoring. It is excluded from the default Azure Container Apps readiness probe so external GitHub outages do not trigger container restarts.

### Structured logging and telemetry ([#107](https://github.com/markheydon/solo-dev-board/issues/107))

| Scenario | Test layer | Location |
|---|---|---|
| OAuth callback URL query-string redaction | Unit | `tests/ServiceDefaults.Tests/.../TelemetryRedactionTests.cs` |
| Sensitive HTTP header tag stripping on spans | Unit | `TelemetryRedactionTests.cs` |
| Non-development environments configure JSON console logging | Unit | `StructuredLoggingConfigurationTests.cs` |

Exporter selection (OTLP for local Aspire, Application Insights when `APPLICATIONINSIGHTS_CONNECTION_STRING` is present) is configuration-driven and verified operationally after deploy. See [docs/observability.md](../docs/observability.md).

Health endpoint tracing exclusion is implemented in `SoloDevBoard.ServiceDefaults` and documented in the observability guide; it is not duplicated with brittle instrumentation assertions in unit tests.

---

## CI and Workflow Validation

The following automated checks are proportionate to current repository tooling and delivery cost:

| Check | Workflow / artefact | Purpose |
|---|---|---|
| `dotnet build` and `dotnet test` on every PR and push to `main` | `.github/workflows/ci.yml` (`build-and-test` job) | Regression gate for all unit and component tests, including operational hardening coverage above. |
| `dotnet format --verify-no-changes` | `ci.yml` | Prevents formatting drift in test and production code. |
| E2E `webServer` waits for `GET /health` before Playwright suite | `playwright.yml`, `tests/E2E/playwright.config.ts` | Confirms the app host starts and readiness endpoint responds in a realistic PAT-mode configuration. |
| Playwright smoke test for `/health` | `tests/E2E/tests/smoke.spec.ts` | Lightweight post-startup health assertion in the browser test harness. |
| Playwright critical user journey suite | `tests/E2E/CRITICAL_JOURNEYS.md`, `tests/E2E/tests/*.spec.ts`, `.github/workflows/playwright.yml` | Feature shell, navigation, and placeholder-auth error-state coverage for v1.0.0 release readiness (issue #255). |

Not automated in CI (manual or deploy-time verification only):

- Azure Container Apps probe configuration in the live subscription.
- Application Insights telemetry ingestion after `aspire deploy`.
- GitHub API rate-limit behaviour under production load.

---

## Related Decisions and Documentation

- [DEC-018: GitHub API response caching in Infrastructure](DECISIONS.md#dec-018-github-api-response-caching-in-infrastructure)
- [Deployment — Health checks and Container Apps probes](../docs/deployment.md#health-checks-and-container-apps-probes)
- [Observability and Telemetry](../docs/observability.md)
