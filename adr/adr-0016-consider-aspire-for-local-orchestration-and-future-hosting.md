# ADR-0016: Consider .NET Aspire for Local Orchestration and Future Hosting Evolution

**Date:** 2026-05-03
**Status:** Accepted

## Context

.NET Aspire provides orchestration, service discovery, telemetry integration, and a development-first AppHost model for .NET applications. It is particularly attractive for containerised and Codespaces/dev-container workflows, and becomes more valuable as an application grows beyond a single process.

SoloDevBoard currently uses .NET 10 Blazor Server, Azure App Service, Bicep for infrastructure as code, Azure Key Vault for secrets, and GitHub Actions OIDC for CI/CD. The application remains a single web app, but local development now requires stronger cross-device consistency for dev container and Codespaces workflows.

Issue #171 tracks a future spike to revisit this decision when the trigger conditions below are met.

## Decision

- Aspire is adopted for local orchestration and production deployment through the AppHost.
- Existing app code remains intact, with onboarding focused on wiring the current web app into Aspire rather than restructuring the solution.
- Production deployment uses Aspire `aspire deploy` to Azure Container Apps with scale-to-zero (see ADR-0018).
- Direct `dotnet run` remains an optional local compatibility path for contributors who are not using Aspire.
- Issue #171 remains open to evaluate broader Aspire adoption separately from this local development onboarding decision.
- This ADR will be revisited if:
  - The production hosting model changes.
  - A second runtime or process is added (e.g., background worker, API, or service).
  - Additional Aspire resources are required beyond the current web app orchestration.

## Rationale

- Existing development already pre-dates Aspire, so onboarding is deliberately incremental and low risk.
- Aspire provides a consistent local orchestration entry point for local machines, dev containers, and Codespaces.
- Aspire now provides a single orchestration model for local development and Azure Container Apps deployment, reducing environment drift.
- Adding Aspire now allows gradual extension to future multi-process scenarios without requiring a disruptive migration later.

## Consequences

- Local development workflows now support Aspire-first startup through the AppHost.
- Contributors can still run the web app directly when needed.
- Production deployment uses Aspire Azure Container Apps deployment (ADR-0018); hand-maintained Bicep App Service templates are removed.
- Related tracking issue: #171 can be closed once broader multi-process Aspire adoption is no longer required.

## Alternatives Considered

- Defer Aspire indefinitely: Rejected because recurring cross-device local setup friction now justifies local orchestration adoption.
- Adopt Aspire for both local and production immediately: Rejected because production hosting decisions should remain decoupled from local onboarding.
