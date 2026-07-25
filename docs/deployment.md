---
layout: page
title: Deployment
nav_order: 3
---

# Deploying SoloDevBoard to Azure

SoloDevBoard deploys to **Azure Container Apps** via **Aspire** (`aspire deploy` from `src/SoloDevBoard.AppHost`). The AppHost is the single source of truth for local orchestration and production deployment.

Scale-to-zero is enabled (`MinReplicas = 0`) to minimise idle hosting costs. Expect cold starts and Blazor Server SignalR reconnects after idle periods.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Azure subscription | With permission to create resources in a resource group |
| [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) | Logged in with `az login` |
| GitHub repository admin access | To configure environments, secrets, and workflows |
| GitHub App (hosted sign-in) | Recommended for production; see [Hosted Authentication](user-guide/hosted-authentication.md) |

---

## One-time Azure setup

### 1. Create a resource group

```bash
az group create \
  --name rg-solodevboard-prod \
  --location uksouth
```

Replace the name and region as needed. Set `AZURE_RESOURCE_GROUP` in the GitHub `production` environment to this value.

### 2. Create a GitHub Actions OIDC identity

Aspire does not create the federated credential for GitHub Actions. Run these commands once per subscription and repository.

Set variables for your environment:

```bash
RESOURCE_GROUP="rg-solodevboard-prod"
LOCATION="uksouth"
GITHUB_ORG="markheydon"
GITHUB_REPO="solo-dev-board"
GITHUB_ENV="production"
IDENTITY_NAME="id-solodevboard-cd-prod"
```

Create a user-assigned managed identity:

```bash
az identity create \
  --name "$IDENTITY_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION"
```

Record the `clientId`, `principalId`, and `id` from the output.

Assign **Contributor** on the resource group:

```bash
PRINCIPAL_ID="$(az identity show --name "$IDENTITY_NAME" --resource-group "$RESOURCE_GROUP" --query principalId -o tsv)"

az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP"
```

Create a federated credential for the GitHub `production` environment:

```bash
CLIENT_ID="$(az identity show --name "$IDENTITY_NAME" --resource-group "$RESOURCE_GROUP" --query clientId -o tsv)"

az identity federated-credential create \
  --name "github-production" \
  --identity-name "$IDENTITY_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --issuer "https://token.actions.githubusercontent.com" \
  --subject "repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${GITHUB_ENV}" \
  --audiences "api://AzureADTokenExchange"
```

### 3. Configure the GitHub `production` environment

In **Settings → Environments → production**, add:

**Secrets**

| Secret | Value |
|---|---|
| `AZURE_CLIENT_ID` | Managed identity `clientId` from step 2 |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |
| `GH_PAT` | `-` for hosted sign-in, or a PAT for trusted self-hosted PAT mode |
| `GH_APP_CLIENT_SECRET` | GitHub App client secret for hosted sign-in |

Secret parameters are written to the Aspire-provisioned `auth-secrets` Key Vault at deploy time and referenced by the Container App. You do not create or manage Key Vault secrets manually for the default deployment path.

**Variables**

| Variable | Example | Purpose |
|---|---|---|
| `AZURE_LOCATION` | `uksouth` | Azure region for `aspire deploy` |
| `AZURE_RESOURCE_GROUP` | `rg-solodevboard-prod` | Target resource group |
| `HOSTED_SIGN_IN_ENABLED` | `true` | Enable hosted sign-in in production |
| `HOSTED_ADMISSION_ENABLED` | `true` | Deny-by-default admission control |
| `GH_APP_CLIENT_ID` | your client ID | GitHub App OAuth client ID |
| `ALLOWED_USER_LOGINS` | `your-login` | Comma-separated user allow-list |
| `ALLOWED_ORG_LOGINS` | `-` | Comma-separated org allow-list, or `-` |

Enable required reviewers on the `production` environment before granting deploy access.

---

## Deploy from GitHub Actions

The CD workflow (`.github/workflows/cd.yml`) runs `aspire deploy` with OIDC authentication.

1. Ensure steps above are complete.
2. Open **Actions → CD - Deploy to Azure → Run workflow**.
3. After a successful run, note the deployed Container App FQDN from the workflow output or Azure portal.
4. Register the GitHub App callback URL: `https://<fqdn>/auth/callback`.
5. Open the provisioned Application Insights resource to confirm telemetry is flowing (see [Observability guide](user-guide/observability.md)).

### Enable automatic deploys

After the first successful manual deploy, uncomment the `push: branches: [main]` trigger in `.github/workflows/cd.yml` to deploy on merge to `main`.

---

## Deploy locally (operator testing)

```bash
az login

export Azure__SubscriptionId="<subscription-id>"
export Azure__Location="uksouth"
export Azure__ResourceGroup="rg-solodevboard-prod"
export Parameters__hosted_sign_in_enabled="true"
export Parameters__gh_app_client_id="<client-id>"
export Parameters__allowed_user_logins="<your-login>"
# Set secret parameters via environment variables or aspire secret set — do not commit values.

dotnet build SoloDevBoard.slnx --configuration Release
aspire deploy \
  --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj \
  --environment Production \
  --non-interactive
```

Preview deployment steps without applying:

```bash
aspire deploy --list-steps \
  --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj \
  --environment Production \
  --non-interactive
```

---

## Resources provisioned by Aspire

Aspire generates and applies Bicep at deploy time. A typical deployment includes:

| Resource | Purpose |
|---|---|
| Azure Container Apps environment | Hosts the containerised app (Consumption profile) |
| Container App (`app`) | Runs SoloDevBoard (scale-to-zero enabled) |
| Azure Container Registry | Stores built container images |
| Azure Key Vault (`auth-secrets`) | Stores hosted auth secret parameters as Key Vault secrets |
| Application Insights | Application logs, metrics, and distributed traces |
| Log Analytics workspace | Container platform logs and Application Insights backing store |
| Aspire dashboard | Optional operational dashboard (Aspire default) |
| Managed identities | Image pull and runtime authentication |

See [Azure Deployment Costs](user-guide/azure-costs.md) for cost guidance.

---

## Health checks and Container Apps probes

SoloDevBoard exposes two HTTP health endpoints via `SoloDevBoard.ServiceDefaults`:

| Endpoint | Purpose | Checks included |
|---|---|---|
| `GET /health` | **Readiness** — the app can accept traffic after startup | All registered health checks |
| `GET /alive` | **Liveness** — the process is responsive | Checks tagged `live` only (the built-in `self` check) |

Both endpoints return `200 OK` with a `Healthy` body when checks pass. They are excluded from distributed tracing to reduce noise (see [Observability](user-guide/observability.md)).

### Liveness versus readiness design

| Probe type | Endpoint | Rationale |
|---|---|---|
| **Liveness** | `/alive` | Confirms the ASP.NET Core host is running. A failure here indicates the container should be restarted. |
| **Readiness** | `/health` | Confirms the app finished startup and can serve requests. Used by Azure Container Apps after scale-from-zero and during deployments. |

SoloDevBoard deliberately does **not** include GitHub API or other external dependency checks in health probes. External outages should surface as application errors and telemetry, not as container restarts or traffic withdrawal.

### Azure Container Apps configuration

The AppHost registers the readiness probe path for the `app` resource:

```csharp
.WithHttpHealthCheck("/health")
```

Aspire applies this as the Container App HTTP probe during `aspire deploy`. No manual Bicep or portal configuration is required for the default deployment path.

After deployment, verify probes from the Azure portal:

1. Open the Container App resource created by Aspire.
2. Select **Containers** → your revision → **Health probes**.
3. Confirm the HTTP probe targets `/health`.

To test manually against the deployed FQDN:

```bash
curl -sf "https://<container-app-fqdn>/health"
curl -sf "https://<container-app-fqdn>/alive"
```

The CD workflow and Playwright smoke tests also call `/health` during CI to confirm the app is ready before browser tests run.

---

## Teardown

To remove Aspire-managed resources:

```bash
aspire destroy \
  --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj \
  --environment Production \
  --yes \
  --non-interactive
```

Confirm the subscription, resource group, and environment before running destroy. Verify removal with `az resource list --resource-group <rg>`.

The OIDC managed identity is not removed by `aspire destroy`. Delete it manually if no longer needed:

```bash
az identity delete --name id-solodevboard-cd-prod --resource-group rg-solodevboard-prod
```

---

## Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| OIDC login fails in CD | Federated credential subject mismatch | Verify subject is `repo:<owner>/<repo>:environment:production` |
| Missing parameter prompt in CI | Secret not mapped | Add `Parameters__*` env vars to the deploy step |
| Cold start / SignalR disconnect | Scale-to-zero idle | Expected; refresh the page or wait for the container to warm up |
| 403 after sign-in | Allow-list | Update `ALLOWED_USER_LOGINS` or `ALLOWED_ORG_LOGINS` |
| Callback URL mismatch | Stale GitHub App setting | Update callback to `https://<aca-fqdn>/auth/callback` |
| Secret not visible in Container App settings | Key Vault reference | Expected — inspect the `auth-secrets` vault for secret names; values are resolved at runtime |

For local development, see [Getting Started](getting-started.md). For hosted authentication details, see [Hosted Authentication](user-guide/hosted-authentication.md). For the Key Vault pattern, see [plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md](../plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md).
