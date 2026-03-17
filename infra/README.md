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
| **GitHub OIDC user-assigned identity** | User-assigned managed identity used by GitHub Actions for OIDC deployment authentication |
| **Federated credential** | Trusts GitHub Actions tokens for the configured repository environment |
| **Contributor role assignment** | Grants the OIDC identity deployment rights on the resource group |

## Baseline Assumptions

The templates in this directory are configured for a public-release-ready baseline:

- App Service uses a system-assigned managed identity and does not store GitHub credentials directly.
- The deployment creates a user-assigned managed identity for GitHub Actions OIDC, including federated credential trust for the configured repository environment.
- Key Vault uses RBAC authorisation, soft delete retention, and purge protection by default.
- The GitHub token is referenced through a Key Vault secret at runtime. The default secret name is `GitHubAuth--PersonalAccessToken`, and you can override it with the `gitHubTokenSecretName` parameter.
- The App Service managed identity is granted `Key Vault Secrets User` at Key Vault scope during deployment.
- Deployment authentication from GitHub Actions uses OIDC and repository environment protections.

## Hosted Sign-In and Admission Control

For production deployments using GitHub App-first hosted authentication:

- Hosted sign-in should be enabled with `GitHubAuth__HostedSignInEnabled=true`.
- Operators should configure `HostedAdmissionControl__Enabled=true` and maintain explicit user and organisation allow-lists.
- Hosted access is deny-by-default when admission control is enabled; only configured allow-list identities are admitted.
- PAT-only local trusted mode remains available for local development and trusted self-hosted use.
- OAuth App fallback remains optional compatibility behaviour and is disabled by default.

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
pwsh ./infra/Deploy-SoloDevBoardInfra.ps1
```

Default behaviour:

- `EnvironmentName`: `prod`.
- `ResourceGroupName`: `rg-solodevboard-<environmentName>` (sanitised lowercase).
- `Location`: `uksouth`.
- `AppServicePlanSku`: `F1` (cost-saving default for development and validation).

For production-like hosting, override `AppServicePlanSku` to a paid tier such as `B1` or `P1v3`.

App Service inbound IP hardening is **disabled by default** — the app is publicly reachable unless you opt in to restrictions.

To enable IP hardening with your current public IPv4 auto-detected as a `/32` allow-list entry:

```powershell
pwsh ./infra/Deploy-SoloDevBoardInfra.ps1 -EnableAppServiceIpHardening
```

To supply an explicit CIDR allow list (which also activates hardening automatically):

```powershell
pwsh ./infra/Deploy-SoloDevBoardInfra.ps1 -AppServiceAllowedCidrs "203.0.113.10/32"
```

The `F1` plan does not support access restrictions; passing `-EnableAppServiceIpHardening` or `-AppServiceAllowedCidrs` on `F1` has no effect and the script will print a notice.

The GitHub repository for the OIDC federated credential subject is auto-detected from `git config remote.origin.url`. Override with `-GitHubRepository <owner>/<repo>` if the script is run outside the repository or if auto-detection fails.

A `-ResourceNameSuffix` parameter is available to append a short suffix to globally constrained resource names (App Service, Key Vault, OIDC identity) to avoid naming collisions if multiple independent deployments use the same subscription.

A `-IncludeNextSteps` switch prints post-deployment setup guidance (GitHub secrets, OIDC verification, environment protection steps). Omit it on repeat deployments when setup is already complete.

Optional overrides:

```powershell
pwsh ./infra/Deploy-SoloDevBoardInfra.ps1 \
  -EnvironmentName dev \
  -ResourceGroupName rg-solodevboard-dev \
  -Location uksouth \
  -AppServicePlanSku B1 \
  -EnableAppServiceIpHardening

# Supply explicit CIDRs instead of auto-detected IP.
pwsh ./infra/Deploy-SoloDevBoardInfra.ps1 -AppServiceAllowedCidrs "203.0.113.10/32","198.51.100.0/24"

# Add a suffix to avoid name collisions in shared subscriptions.
pwsh ./infra/Deploy-SoloDevBoardInfra.ps1 -ResourceNameSuffix mh01

# Print first-time setup guidance.
pwsh ./infra/Deploy-SoloDevBoardInfra.ps1 -IncludeNextSteps
```

The script deploys the Bicep template, prints deployment outputs, and (when `-IncludeNextSteps` is passed) provides copy/paste commands for:

- Configuring the `cd.yml` GitHub Actions environment secrets for `production`.
- Verifying OIDC federation and configuring environment protections.
- Optionally storing a GitHub PAT in Key Vault for PAT-based app auth.

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
  --name "GitHubAuth--PersonalAccessToken" \
  --value "<your-github-pat>"
```

If this command returns `Forbidden`, your user account does not have Key Vault data-plane permissions for secret write operations. You need one of these roles on the Key Vault scope:

- `Key Vault Secrets Officer`.
- `Key Vault Administrator`.

If you cannot assign roles yourself, ask a subscription or resource-group owner to grant one of the roles above and then retry.

If you can self-assign roles, you can grant temporary access and then remove it after writing the secret:

```bash
az role assignment create \
  --assignee-object-id "$(az ad signed-in-user show --query id -o tsv)" \
  --assignee-principal-type User \
  --role "Key Vault Secrets Officer" \
  --scope "$(az keyvault show --name <keyVaultName> --query id -o tsv)"

# Set the secret.
az keyvault secret set --vault-name <keyVaultName> --name "GitHubAuth--PersonalAccessToken" --value "<your-github-pat>"

# Remove temporary access.
az role assignment delete \
  --assignee-object-id "$(az ad signed-in-user show --query id -o tsv)" \
  --role "Key Vault Secrets Officer" \
  --scope "$(az keyvault show --name <keyVaultName> --query id -o tsv)"
```

Incremental Bicep deployments usually do not remove extra user role assignments automatically, so remove temporary access explicitly when finished.

### 4. Verify managed identity role assignment

The deployment creates an RBAC assignment that grants the App Service managed identity the `Key Vault Secrets User` role on the deployed Key Vault.

To verify after deployment:

```bash
az role assignment list \
  --scope "$(az keyvault show --name <keyVaultName> --query id -o tsv)" \
  --query "[?roleDefinitionName=='Key Vault Secrets User']"
```

### 5. Configure the CD pipeline (OIDC)

Configure the following GitHub environment secrets on the `production` environment for the CD workflow (`.github/workflows/cd.yml`):

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | Client ID of the GitHub OIDC deployment identity created by the Bicep deployment |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_WEBAPP_NAME` | The App Service name (from deployment outputs) |

The deployment script now prints `gh secret set --env production ...` commands for these values, including the exact `AZURE_CLIENT_ID` from deployment outputs.

### 6. Configure protected environment expectations

Create or update the `production` GitHub environment and apply the following protections:

- Required reviewers must be enabled for deployment approval.
- Deployment branch rules should allow only `main`.
- Environment secrets required by deployment should be defined on the `production` environment unless you intentionally change the workflow scope.
- No long-lived Azure client secrets should be configured for deployment.

### 7. Verify Azure OIDC trust prerequisites

The Bicep deployment creates federated credentials on the deployment identity with these claims:

- Issuer: `https://token.actions.githubusercontent.com`.
- Subject: `repo:<owner>/<repo>:environment:<environmentName>`.
- Audience: `api://AzureADTokenExchange`.

When using the deployment script, `<owner>/<repo>` comes from `-GitHubRepository` and `<environmentName>` comes from `-GitHubEnvironmentName`.

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
| `gitHubTokenSecretName` | `GitHubAuth--PersonalAccessToken` | The Key Vault secret name used by App Service setting `GitHubAuth__PersonalAccessToken`. |
| `healthCheckPath` | `/` | The path used by App Service platform health checks. |
| `keyVaultSoftDeleteRetentionInDays` | `90` | Key Vault soft-delete retention window in days. |
| `keyVaultEnablePurgeProtection` | `true` | Whether purge protection is enabled for Key Vault. |
| `keyVaultPublicNetworkAccess` | `Enabled` | Controls whether public network access is enabled for Key Vault. |
| `appServiceAllowedCidrs` | `[]` | Optional CIDR allow list for App Service inbound access. When set, unmatched traffic is denied. |
| `gitHubRepository` | _(required — no default)_ | GitHub repository in `<owner>/<repo>` format used for OIDC federation subject matching. Auto-detected from git remote by the deployment script. |
| `gitHubEnvironmentName` | `production` | GitHub environment name used in OIDC federated subject and secret scoping guidance. |
| `resourceNameSuffix` | _(empty)_ | Optional short suffix appended to globally constrained resource names (App Service, Key Vault, OIDC identity) to avoid naming collisions across subscriptions. |

When running `infra/Deploy-SoloDevBoardInfra.ps1`, the GitHub repository is auto-detected from `git config remote.origin.url`; override with `-GitHubRepository` if needed. IP hardening is opt-in; use `-EnableAppServiceIpHardening` to auto-allow your current public IPv4, or pass `-AppServiceAllowedCidrs` to specify explicit ranges.

## App Configuration

The following environment variables are configured on the App Service; the Source column indicates whether each value comes from a Key Vault reference or directly from app settings:

| Variable | Source | Description |
|----------|--------|-------------|
| `GitHubAuth__PersonalAccessToken` | Key Vault secret `GitHubAuth--PersonalAccessToken` | GitHub Personal Access Token used by the current single-user runtime path |
| `ASPNETCORE_ENVIRONMENT` | App settings | ASP.NET Core hosting environment (`Production` for `prod`, otherwise `Development`) |

## Module Structure

```
infra/
├── main.bicep           # Entry point — creates resource group–level resources
└── modules/
    └── appservice.bicep # App Service Plan + App Service resources
```
