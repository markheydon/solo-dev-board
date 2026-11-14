# ADR-0018: Aspire Azure Container Apps Deployment

**Date:** 2026-07-14
**Status:** Accepted

## Context

SoloDevBoard originally deployed to Azure App Service using hand-maintained Bicep templates in `infra/`, a PowerShell deployment script, and a GitHub Actions workflow that published a `dotnet publish` artefact to App Service. Since ADR-0016, .NET Aspire has been adopted for local orchestration through the AppHost.

The hand-maintained Bicep stack duplicated concerns that Aspire now models in the AppHost: compute hosting, container image build and push, environment configuration, and deployment automation. Maintaining two deployment paths increases operator burden and documentation drift.

The operator requirement is a cost-conscious hosted deployment that can scale to zero when idle.

## Decision

- Production deployment uses **Aspire `aspire deploy`** from [`SoloDevBoard.AppHost`](../SoloDevBoard.AppHost/AppHost.cs) to **Azure Container Apps**.
- The AppHost declares an Azure Container Apps environment via `AddAzureContainerAppEnvironment` and configures the web app with `PublishAsAzureContainerApp`, including **scale-to-zero** (`MinReplicas = 0`).
- GitHub Actions CD (`.github/workflows/cd.yml`) runs `aspire deploy --environment Production --non-interactive` with OIDC authentication to Azure.
- Application secrets for hosted deployments are supplied through **Aspire AppHost parameters** mapped from GitHub Environment secrets (`Parameters__*`), not through hand-maintained Key Vault references in repo Bicep.
- Hand-maintained Bicep templates and the App Service deployment script in `infra/` are removed. Aspire generates and applies deployment Bicep at deploy time.
- One-time GitHub Actions OIDC bootstrap for Azure is documented using **Azure CLI commands only** (no bootstrap Bicep or scripts in the repository).

## Rationale

- **Single deployment model:** Local development and production deployment both flow from the AppHost, reducing drift between environments.
- **Aspire-native Azure path:** Microsoft documents and supports `aspire add azure-appcontainers`, `aspire deploy`, and `aspire destroy` as the primary Azure Container Apps deployment workflow.
- **Cost control:** Azure Container Apps on the Consumption workload profile with `MinReplicas = 0` avoids always-on App Service Plan charges for a low-traffic solo-developer tool.
- **Simpler operator experience:** Self-hosters follow one deployment guide (`docs/deployment.md`) instead of separate Bicep, PowerShell, and zip-deploy instructions.
- **Security preserved:** GitHub Actions continues to use OIDC (no long-lived Azure client secrets). Secret parameters remain out of source control and are injected at deploy time.

## Consequences

- [`infra/main.bicep`](../infra/main.bicep), [`infra/modules/appservice.bicep`](../infra/modules/appservice.bicep), and [`infra/Deploy-SoloDevBoardInfra.ps1`](../infra/Deploy-SoloDevBoardInfra.ps1) are removed.
- ADR-0003 is superseded for production infrastructure definitions.
- ADR-0016 is updated: Aspire is now the local **and** production deployment path.
- Deployed resources include Azure Container Apps, Azure Container Registry, Log Analytics, and the Aspire dashboard (Aspire default for ACA environments).
- Blazor Server cold starts and SignalR reconnect behaviour must be expected after idle scale-down; operators should read updated cost and behaviour guidance in `docs/user-guide/azure-costs.md`.
- Runtime Key Vault integration is deferred; per-user vault naming from ADR-0007 Phase 5 can be revisited with Aspire Key Vault hosting integrations if required.

## Alternatives Considered

- **Retain Bicep App Service deployment:** Rejected because it duplicates Aspire deployment capabilities and prevents scale-to-zero without accepting F1 limitations.
- **Aspire Azure App Service target:** Rejected because the default App Service plan SKU is premium-oriented and does not meet the scale-to-zero cost goal.
- **Keep minimal Bicep for OIDC bootstrap:** Rejected per operator preference; Azure CLI documentation is sufficient for one-time federated credential setup.

## Supersedes

- Production hosting portions of [ADR-0003](0003-bicep-infrastructure.md).
- Production deployment deferral in [ADR-0016](0016-consider-aspire-for-local-orchestration-and-future-hosting.md).
