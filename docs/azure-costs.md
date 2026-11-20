> **Audience:** Developers and operators. This guide is repository documentation and is not part of the published end-user site in `website/`.

# Azure Deployment Costs for Solo Dev Board

Self-hosting Solo Dev Board on Azure incurs charges for the resources Aspire provisions at deploy time. With scale-to-zero enabled, compute charges apply mainly when the app is handling requests.

## Resources Deployed

Aspire deploys resources according to deploy posture (`hosted-sign-in-enabled`). See the [Deployment guide](deployment.md) (including [self-hoster PAT deploy](deployment.md#self-hoster-deployment-pat-only-local-trusted-mode)) and [Observability guide](observability.md).

[PAT-only self-host](deployment.md#self-hoster-deployment-pat-only-local-trusted-mode) and **hosted sign-in** Staging/Production use the same Azure resource set in AppHost today: shared resources in the table below (`web` only). Overnight inventory (SQL, Functions, storage) is not deployed ([ADR-0046](../adr/adr-0046-overnight-inventory-functions-and-sql.md) withdrawn).

| Resource | Purpose | Pricing model |
|---|---|---|
| Azure Container Apps environment | Hosts containerised workloads on the Consumption profile | Environment + per-use compute |
| Container App (`web`) | Solo Dev Board Blazor Server application | Consumption (vCPU-seconds, memory, requests) |
| Azure Container Registry (Basic) | Stores built container images when not using a shared registry ([DEC-025](../adr/adr-0025-optional-shared-azure-container-registry.md)) | Fixed monthly (Basic tier) when Aspire provisions one per deploy |
| Azure Key Vault (`auth-secrets`) | Auth secrets at deploy time ([ADR-0045](../adr/adr-0045-persistent-key-vault-dual-path-local-secrets.md)) | Operations + optional HSM SKUs; secret operations are low volume |
| Log Analytics workspace | Container platform logs and Application Insights backing store | Per GB ingested |
| Application Insights | Application logs, metrics, and traces | Per GB ingested (linked workspace) |
| Managed identities | Image pull and runtime auth | No direct charge |

## Cost profile with scale-to-zero

| Scenario | Approximate monthly cost (UK South, early 2026) |
|---|---|
| Idle / no traffic (lean or hosted sign-in deploy) | Low - ACR Basic (~£4–5) when Aspire provisions a registry, minimal Log Analytics ingestion |
| Light personal use | Typically lower than an always-on App Service B1 (~£11–13) |
| Sustained daily use | Consumption compute adds up; monitor in Azure Cost Management |

> **Note:** Figures are estimates. Use the [Azure Pricing Calculator](https://azure.microsoft.com/en-gb/pricing/calculator/) for up-to-date costs.

### Scale-to-zero trade-offs

- **Savings:** No always-on App Service Plan charge when idle.
- **Cold starts:** First request after idle may be slow while the container starts.
- **Blazor Server:** SignalR circuits may disconnect after scale-down; users may need to refresh or reconnect.

## Cost optimisation tips

- Scale-to-zero is enabled by default (`MinReplicas = 0`) in the AppHost.
- Share one existing ACR across Staging and Production when opting in — see [Optional shared Container Registry](deployment.md#optional-shared-container-registry).
- Delete unused deployments with `aspire destroy` when you no longer need a hosted instance.
- Review Log Analytics ingestion if log volume grows.
- Use dev/test pricing offers if eligible.

## Azure Pricing Calculator

For exact, up-to-date pricing, use the [Azure Pricing Calculator](https://azure.microsoft.com/en-gb/pricing/calculator/). Select UK South and add Container Apps, Container Registry (Basic), Key Vault, Application Insights, and Log Analytics for every deploy.

## Related documentation

- [Self-hoster deployment (PAT-only local trusted mode)](deployment.md#self-hoster-deployment-pat-only-local-trusted-mode) — personal Azure instance without a GitHub App.
- [Getting Started — PAT-only local trusted mode](getting-started.md#pat-only-local-trusted-mode) — local configuration and trust boundary.
- [Observability](observability.md) — Application Insights and operational diagnostics after deploy.

## Disclaimer

Prices shown are approximate and may change. Charges vary by region, usage, and SKU. Always check the Azure portal or pricing calculator before deploying SoloDevBoard.
