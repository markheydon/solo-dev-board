---
layout: default
title: Azure Deployment Costs
nav_order: 10
---

# Azure Deployment Costs for SoloDevBoard

Self-hosting SoloDevBoard on Azure incurs real charges for the resources provisioned. The Free tier (F1) is a zero-cost starting point for evaluation, but it has significant limitations. For production workloads, you will need to select a paid App Service Plan SKU and understand the associated costs before deploying.

## Resources Deployed

The Bicep template provisions the following Azure resources:

| Resource              | Purpose                                                      | Pricing Model           |
|-----------------------|--------------------------------------------------------------|-------------------------|
| App Service Plan      | Hosts the Blazor Server application (Linux, .NET 10)         | Fixed monthly (SKU-based) |
| App Service           | The SoloDevBoard web application                             | Included in App Service Plan |
| Key Vault             | Stores GitHub token and other secrets securely               | Consumption (per secret op) |
| Key Vault RBAC        | Grants managed identity permission to read Key Vault secrets | No direct charge        |

## App Service Plan SKU Options and Estimated Costs

The App Service Plan is the main cost driver. Below are common SKUs and their approximate monthly costs for the UK South region (as of early 2026):

| SKU     | Features                          | Approx. Monthly Cost (GBP) |
|---------|------------------------------------|---------------------------|
| F1      | Free tier, 60 CPU-min/day, no Always On, no custom domains, no access restrictions | £0                      |
| B1      | Basic, Always On, custom domains, access restrictions, TLS | £11–13                 |
| B2      | More CPU/RAM, Always On, custom domains, access restrictions, TLS | £40–45                 |
| P0v3    | Premium, enhanced scaling, Always On, custom domains, access restrictions, TLS | £60–65                 |
| P1v3    | Premium, more resources, enhanced scaling, Always On, custom domains, access restrictions, TLS | £120–130               |

> **Note:** These figures are estimates. Actual prices may vary by region and over time. Use the [Azure Pricing Calculator](https://azure.microsoft.com/en-gb/pricing/calculator/) for up-to-date costs.

### Key Vault Costs

Key Vault incurs negligible charges at this scale. Secret operations are fractions of a penny, and most users will not notice any cost unless performing thousands of operations per month.

## Free Tier Limitations

- The F1 tier does not support Always On, so the application may experience cold starts and slow response times.
- Custom domains and TLS certificates are not available on F1; you must use the default Azure-provided domain.
- Access restrictions (CIDR/IP allow lists) are not supported on F1. The application is open to the public internet.
- The F1 tier is capped at 60 CPU-minutes per day, which is insufficient for sustained workloads.

## Recommended Starting Point

For production use, the minimum recommended tier is B1, which costs approximately £11–13 per month in UK South. B1 provides Always On, custom domains with TLS, and access restrictions for improved security.

## Cost Optimisation Tips

- Stop or deallocate the App Service when not in use to avoid unnecessary charges.
- Use dev/test pricing if eligible (see Azure portal for details).
- Consider running development environments on F1 and production on B1 or higher.
- Monitor usage and scale up or down as needed to match workload requirements.

## Azure Pricing Calculator

For exact, up-to-date pricing, use the [Azure Pricing Calculator](https://azure.microsoft.com/en-gb/pricing/calculator/). Select the UK South region and the relevant SKUs to see current costs.

## Disclaimer

Prices shown are approximate and may change. Charges vary by region, SKU, and usage. Always check the Azure portal or pricing calculator for the latest figures before deploying SoloDevBoard.
