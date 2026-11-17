> **Audience:** Developers and operators. This guide is repository documentation and is not part of the published end-user site in `website/`.

# Azure Deployment Costs for Solo Dev Board

Self-hosting Solo Dev Board on Azure incurs charges for the resources Aspire provisions at deploy time. With scale-to-zero enabled, compute charges apply mainly when the app is handling requests.

## Resources Deployed

Aspire deploys the following Azure resources (see [Deployment guide](deployment.md), including the [self-hoster PAT path](deployment.md#self-hoster-deployment-pat-mode) for a personal instance, and [Observability guide](observability.md)):

| Resource | Purpose | Pricing model |
|---|---|---|
| Azure Container Apps environment | Hosts containerised workloads on the Consumption profile | Environment + per-use compute |
| Container App (`app`) | Solo Dev Board Blazor Server application | Consumption (vCPU-seconds, memory, requests) |
| Azure Functions (`functions`) | Overnight inventory placeholder (timer); future scanners ([ADR-0046](../adr/adr-0046-overnight-inventory-functions-and-sql.md)) | Per execution + linked storage; **full experience deploy only** |
| Azure SQL (`sql` / database `inventory`) | Derived repository snapshot store for catalogue roll-ups | Serverless GP tier; Aspire default includes free-limit auto-pause; **full experience deploy only** |
| Azure Storage (`storage` / `storagestaging` on Staging) | Functions host storage (not product data) | Account + transactions; **full experience deploy only** |

[PAT-only self-host](deployment.md#self-hoster-deployment-pat-mode) deploy uses the lean graph: Container App, Key Vault, telemetry, and registry only (lower idle cost than hosted sign-in tiers).
| Azure Container Registry (Basic) | Stores built container images when not using a shared registry ([DEC-025](../adr/adr-0025-optional-shared-azure-container-registry.md)) | Fixed monthly (Basic tier) when Aspire provisions one per deploy |
| Log Analytics workspace | Container platform logs and Application Insights backing store | Per GB ingested |
| Application Insights | Application logs, metrics, and traces | Per GB ingested (linked workspace) |
| Managed identities | Image pull and runtime auth | No direct charge |

## Cost profile with scale-to-zero

| Scenario | Approximate monthly cost (UK South, early 2026) |
|---|---|
| Idle / no traffic | Low to moderate — ACR Basic (~£4–5) per Aspire-provisioned registry, minimal Log Analytics, plus always-on storage account and SQL free tier while within limits |
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

For exact, up-to-date pricing, use the [Azure Pricing Calculator](https://azure.microsoft.com/en-gb/pricing/calculator/). Select UK South and add Container Apps, Azure SQL Database (serverless), Storage, Functions, Container Registry (Basic), and Log Analytics.

## Related documentation

- [Self-hoster deployment (PAT mode)](deployment.md#self-hoster-deployment-pat-mode) — personal Azure instance without a GitHub App.
- [Getting Started — PAT-only local trusted mode](getting-started.md#pat-only-local-trusted-mode) — local configuration and trust boundary.
- [Observability](observability.md) — Application Insights and operational diagnostics after deploy.

## Disclaimer

Prices shown are approximate and may change. Charges vary by region, usage, and SKU. Always check the Azure portal or pricing calculator before deploying SoloDevBoard.
