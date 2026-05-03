# ADR-0016: Consider .NET Aspire for Local Orchestration and Future Hosting Evolution

**Date:** 2026-05-03
**Status:** Proposed

## Context

.NET Aspire provides orchestration, service discovery, telemetry integration, and a development-first AppHost model for .NET applications. It is particularly attractive for containerised and Codespaces/dev-container workflows, and becomes more valuable as an application grows beyond a single process.

SoloDevBoard currently uses .NET 10 Blazor Server, Azure App Service, Bicep for infrastructure as code, Azure Key Vault for secrets, and GitHub Actions OIDC for CI/CD. The application is still a single web app, is not yet deployed to Azure, and is not currently structured in a way that benefits materially from Aspire.

Issue #171 tracks a future spike to revisit this decision when the trigger conditions below are met.

## Decision

- Aspire is being considered for future adoption, but is not adopted at this time.
- The immediate recommendation is to defer Aspire adoption until the application is deployed and/or distributed.
- Any future Aspire trial should begin as a small spike focused on local orchestration and AppHost, not as a production deployment rewrite.
- Existing Azure Bicep/App Service deployment remains the accepted and supported path for production.
- Codespaces and dev-container compatibility is a motivating factor for future Aspire evaluation.
- This ADR will be revisited if:
  - The application is deployed to Azure.
  - A second runtime or process is added (e.g., background worker, API, or service).
  - There is a need for local orchestration, telemetry, or service discovery.
  - Repeated developer environment friction is observed.

## Rationale

- SoloDevBoard is currently a single Blazor Server application, so Aspire would add orchestration overhead before the project clearly needs it.
- The current Azure deployment path is already defined through Bicep, App Service, Key Vault, and GitHub Actions OIDC, and should remain stable until the application is live.
- Aspire's Codespaces and dev-container compatibility makes it a worthwhile future option for improving local developer experience.
- A limited future spike would allow the project to assess AppHost, local orchestration, telemetry, and service discovery without prematurely rewriting production deployment.
- Recording the decision now avoids premature platform churn while preserving the option to adopt Aspire later on a deliberate, low-risk basis.

## Consequences

- No immediate changes to deployment or local development workflows.
- Maintainers and contributors should continue to use the current Azure Bicep/App Service approach.
- Aspire will be tracked as a future consideration and re-evaluated when trigger conditions are met.
- Related tracking issue: #171, "[Chore] Evaluate .NET Aspire as a deferred local orchestration spike".

## Alternatives Considered

- Adopt Aspire immediately: Rejected as premature given current project maturity and deployment status.
- Remain on current path: Accepted for now, with Aspire as a deferred evaluation.
