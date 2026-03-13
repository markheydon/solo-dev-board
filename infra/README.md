# SoloDevBoard — Azure Infrastructure

This directory contains the Bicep templates for deploying SoloDevBoard to Azure.

## Overview

The infrastructure deploys the following Azure resources:

| Resource | Purpose |
|----------|---------|
| **App Service Plan** | Hosts the Blazor Server application (Linux, .NET 10) |
| **App Service** | The SoloDevBoard web application |
| **Key Vault** | Stores the GitHub token and other secrets securely |
| **Key Vault RBAC assignment** | Grants the App Service managed identity permission to read Key Vault secrets |

## Baseline Assumptions

The templates in this directory are configured for a public-release-ready baseline:

- App Service uses a system-assigned managed identity and does not store GitHub credentials directly.
- Key Vault uses RBAC authorisation, soft delete retention, and purge protection by default.
- The GitHub token is referenced through a Key Vault secret (`GitHub--Token`) at runtime.
- The App Service managed identity is granted `Key Vault Secrets User` at Key Vault scope during deployment.
- Deployment authentication from GitHub Actions uses OIDC and repository environment protections.

## Prerequisites

Before deploying, ensure you have the following installed and configured:

| Tool | Version | Install |
|------|---------|---------|
| [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) | Latest | `brew install azure-cli` / apt / winget |
| Bicep CLI | Latest | `az bicep install` |
| An Azure subscription | — | [Create a free account](https://azure.microsoft.com/free/) |

Log in to Azure:

```bash
az login
```

## Deploying

### Automated deployment script (PowerShell)

For a guided deployment that also prints post-deployment next steps (local run configuration and GitHub Actions secrets), run:

```powershell
pwsh ./infra/Deploy-SoloDevBoardInfra.ps1 \
  -ResourceGroupName rg-solodevboard-prod \
  -Location uksouth \
  -EnvironmentName prod
```

The script deploys the Bicep template, prints deployment outputs, and then provides copy/paste commands for:

- Setting the GitHub token secret in Key Vault.
- Configuring local .NET user-secrets values.
- Configuring the `cd.yml` GitHub Actions repository secrets.
- Configuring OIDC federation and production environment protections.

### 1. Create a resource group

```bash
az group create \
  --name rg-solodevboard-prod \
  --location uksouth
```

### 2. Deploy the Bicep template

```bash
az deployment group create \
  --resource-group rg-solodevboard-prod \
  --template-file infra/main.bicep \
  --parameters environmentName=prod
```

### 3. Store the GitHub token in Key Vault

After deployment, retrieve the Key Vault name from the outputs:

```bash
az deployment group show \
  --resource-group rg-solodevboard-prod \
  --name main \
  --query properties.outputs.keyVaultName.value \
  --output tsv
```

Then store your GitHub Personal Access Token (or GitHub App private key):

```bash
az keyvault secret set \
  --vault-name <keyVaultName> \
  --name "GitHub--Token" \
  --value "<your-github-pat>"
```

### 4. Verify managed identity role assignment

The deployment creates an RBAC assignment that grants the App Service managed identity the `Key Vault Secrets User` role on the deployed Key Vault.

To verify after deployment:

```bash
az role assignment list \
  --scope "$(az keyvault show --name <keyVaultName> --query id -o tsv)" \
  --query "[?roleDefinitionName=='Key Vault Secrets User']"
```

### 5. Configure the CD pipeline (OIDC)

Configure the following GitHub repository secrets for the CD workflow (`.github/workflows/cd.yml`):

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | Azure AD application (service principal or user-assigned managed identity) client ID used by OIDC |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_WEBAPP_NAME` | The App Service name (from deployment outputs) |

### 6. Configure protected environment expectations

Create or update the `production` GitHub environment and apply the following protections:

- Required reviewers must be enabled for deployment approval.
- Deployment branch rules should allow only `main`.
- Environment secrets required by deployment should be defined at repository or environment scope.
- No long-lived Azure client secrets should be configured for deployment.

### 7. Configure Azure OIDC trust prerequisites

Configure federated credentials on the Azure AD application used by the workflow with these recommended claims:

- Issuer: `https://token.actions.githubusercontent.com`.
- Subject: `repo:markheydon/solo-dev-board:environment:production`.
- Audience: `api://AzureADTokenExchange`.

After configuring federated credentials, grant the deployment identity least-privilege RBAC roles required for deployment. As a baseline, this usually means:

- `Contributor` on the resource group that hosts SoloDevBoard infrastructure.
- `Web Plan Contributor` or equivalent if App Service plan operations are split to another scope.
- No subscription-wide `Owner` role assignment for CI/CD identities.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `environmentName` | `dev` | The environment name (e.g. dev, staging, prod). Used to name resources. |
| `location` | Resource group location | The Azure region for deployment. |
| `appServicePlanSku` | `B1` | The App Service Plan SKU. Use `B1` for development, `P1v3` for production. |
| `gitHubTokenSecretName` | `GitHub--Token` | The Key Vault secret name used for `GitHub__Token`. |
| `healthCheckPath` | `/` | The path used by App Service platform health checks. |
| `keyVaultSoftDeleteRetentionInDays` | `90` | Key Vault soft-delete retention window in days. |
| `keyVaultEnablePurgeProtection` | `true` | Whether purge protection is enabled for Key Vault. |
| `keyVaultPublicNetworkAccess` | `Enabled` | Controls whether public network access is enabled for Key Vault. |

## App Configuration

The following environment variables are set on the App Service and read from Key Vault at runtime:

| Variable | Source | Description |
|----------|--------|-------------|
| `GitHubAuth__PersonalAccessToken` | Key Vault secret `GitHub--Token` | GitHub Personal Access Token used by the current single-user runtime path |
| `ASPNETCORE_ENVIRONMENT` | App settings | ASP.NET Core hosting environment (`Production` for `prod`, otherwise `Development`) |

## Module Structure

```
infra/
├── main.bicep           # Entry point — creates resource group–level resources
└── modules/
    └── appservice.bicep # App Service Plan + App Service resources
```
