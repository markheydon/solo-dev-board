# Azure Deployment Costs for SoloDevBoard

Self-hosting SoloDevBoard on Azure incurs charges for the resources Aspire provisions at deploy time. With scale-to-zero enabled, compute charges apply mainly when the app is handling requests.

## Resources Deployed

Aspire deploys the following Azure resources (see [Deployment guide](../deployment.md)):

| Resource | Purpose | Pricing model |
|---|---|---|
| Azure Container Apps environment | Hosts containerised workloads on the Consumption profile | Environment + per-use compute |
| Container App (`app`) | SoloDevBoard Blazor Server application | Consumption (vCPU-seconds, memory, requests) |
| Azure Container Registry (Basic) | Stores built container images | Fixed monthly (Basic tier) |
| Log Analytics workspace | Container and environment logs | Per GB ingested |
| Managed identities | Image pull and runtime auth | No direct charge |

## Cost profile with scale-to-zero

| Scenario | Approximate monthly cost (UK South, early 2026) |
|---|---|
| Idle / no traffic | Low — mainly ACR Basic (~£4–5) and minimal Log Analytics |
| Light personal use | Typically lower than an always-on App Service B1 (~£11–13) |
| Sustained daily use | Consumption compute adds up; monitor in Azure Cost Management |

> **Note:** Figures are estimates. Use the [Azure Pricing Calculator](https://azure.microsoft.com/en-gb/pricing/calculator/) for up-to-date costs.

### Scale-to-zero trade-offs

- **Savings:** No always-on App Service Plan charge when idle.
- **Cold starts:** First request after idle may be slow while the container starts.
- **Blazor Server:** SignalR circuits may disconnect after scale-down; users may need to refresh or reconnect.

## Cost optimisation tips

- Scale-to-zero is enabled by default (`MinReplicas = 0`) in the AppHost.
- Delete unused deployments with `aspire destroy` when you no longer need a hosted instance.
- Review Log Analytics ingestion if log volume grows.
- Use dev/test pricing offers if eligible.

## Azure Pricing Calculator

For exact, up-to-date pricing, use the [Azure Pricing Calculator](https://azure.microsoft.com/en-gb/pricing/calculator/). Select UK South and add Container Apps, Container Registry (Basic), and Log Analytics.

## Disclaimer

Prices shown are approximate and may change. Charges vary by region, usage, and SKU. Always check the Azure portal or pricing calculator before deploying SoloDevBoard.
