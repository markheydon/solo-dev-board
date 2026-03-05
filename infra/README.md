# SoloDevBoard — Azure Infrastructure

This directory contains the Bicep templates for deploying SoloDevBoard to Azure.

## Overview

The infrastructure deploys the following Azure resources:

| Resource | Purpose |
|----------|---------|
| **App Service Plan** | Hosts the Blazor Server application (Linux, .NET 10) |
| **App Service** | The SoloDevBoard web application |
| **Key Vault** | Stores the GitHub token and other secrets securely |

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

### 4. Configure the CD pipeline

Update the following GitHub repository secrets for the CD workflow (`.github/workflows/cd.yml`):

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | Service principal or managed identity client ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_WEBAPP_NAME` | The App Service name (from deployment outputs) |

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `environmentName` | `dev` | The environment name (e.g. dev, staging, prod). Used to name resources. |
| `location` | Resource group location | The Azure region for deployment. |
| `appServicePlanSku` | `B1` | The App Service Plan SKU. Use `B1` for development, `P1v3` for production. |

## App Configuration

The following environment variables are set on the App Service and read from Key Vault at runtime:

| Variable | Source | Description |
|----------|--------|-------------|
| `GitHub__Token` | Key Vault secret `GitHub--Token` | GitHub Personal Access Token or GitHub App token |
| `GitHub__BaseUrl` | App settings | GitHub API base URL (default: `https://api.github.com`) |

## Module Structure

```
infra/
├── main.bicep           # Entry point — creates resource group–level resources
└── modules/
    └── appservice.bicep # App Service Plan + App Service resources
```
