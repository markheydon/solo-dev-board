> **Audience:** Developers and operators. This guide is repository documentation and is not part of the published end-user site in `user-docs/`.

# Deploying SoloDevBoard to Azure

SoloDevBoard deploys to **Azure Container Apps** via **Aspire** (`aspire deploy` from `src/SoloDevBoard.AppHost`). The AppHost is the single source of truth for local orchestration and production deployment.

Scale-to-zero is enabled (`MinReplicas = 0`) to minimise idle hosting costs. Expect cold starts and Blazor Server SignalR reconnects after idle periods.

Choose an authentication mode **before** you deploy:

| Path | Authentication | Best for |
|---|---|---|
| **[Self-hoster (PAT mode)](#self-hoster-deployment-pat-mode)** | One personal access token; no GitHub App | A personal instance on your own Azure subscription |
| **[Hosted sign-in](#deploy-from-github-actions)** | GitHub App OAuth + allow-lists | Shared or public production deployments |

Both paths use the same Aspire / Azure Container Apps stack. Only the AppHost parameters and GitHub Environment secrets differ.

## CD pipeline tiers (DEC-021)

GitHub Actions CD (`.github/workflows/cd.yml`) deploys to two hosted tiers that share one Azure resource group. Aspire environment suffixes distinguish resources within that group. Both tiers use GitHub App hosted sign-in. PAT-only mode is for local development and personal self-hosting via local `aspire deploy` only — not as a hosted CD tier.

| Tier | Trigger | GitHub Environment | Aspire `--environment` | Authentication |
|---|---|---|---|---|
| **Staging** | Merge to `main`, or **Actions → CD - Deploy to Azure → Run workflow** (manual staging deploy) | `staging` | `Staging` | GitHub App hosted sign-in |
| **Production** | Push tag `v*` | `production` | `Production` | GitHub App hosted sign-in |

End-user documentation in `user-docs/` is published to GitHub Pages on `v*` release tags only. Pull requests validate Hugo builds via `hugo-ci.yml` without publishing.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Azure subscription | With permission to create resources in a resource group |
| [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) | Logged in with `az login` |
| GitHub repository admin access | To configure environments, secrets, and workflows (skip if you only deploy with local `aspire deploy`) |
| GitHub Personal Access Token **or** GitHub App | PAT for [self-hoster mode](#self-hoster-deployment-pat-mode); GitHub App for [hosted sign-in](hosted-authentication.md) |

---

## Self-hoster deployment (PAT mode)

Use this path when you want a **personal** SoloDevBoard instance on your own Azure subscription without setting up a GitHub App. Authentication stays in **PAT-only local trusted mode**: the Container App uses one configured PAT for all GitHub API calls, and there is no `/auth/sign-in` flow.

> **Trust boundary:** Anyone who can reach the Container App URL acts as the PAT owner. Prefer a private network, IP restrictions, or a URL you alone use. For shared or public endpoints, use [hosted sign-in](hosted-authentication.md) instead. Local PAT setup is documented in [Getting Started — PAT-only local trusted mode](getting-started.md#pat-only-local-trusted-mode).

### What you need

1. An Azure subscription and a resource group (step 1 under [One-time Azure setup](#one-time-azure-setup)).
2. A GitHub PAT with scopes `repo`, `read:org`, `workflow`, and `read:project` (same as local development).
3. **Local `aspire deploy`** (no GitHub Actions OIDC required).

### Deploy locally with `aspire deploy` (recommended for personal instances)

1. Complete [Create a resource group](#1-create-a-resource-group) (OIDC identity is optional for this option).
2. Log in and export Azure + PAT-mode parameters:

```bash
az login

export Azure__SubscriptionId="$(az account show --query id -o tsv)"
export Azure__Location="uksouth"
export Azure__ResourceGroup="rg-solodevboard-prod"

# PAT-only local trusted mode — no GitHub App
export Parameters__hosted_sign_in_enabled="false"
export Parameters__hosted_admission_enabled="true"   # ignored in PAT mode; any value is fine
export Parameters__gh_pat="<your-github-pat>"
export Parameters__gh_app_client_id="-"
export Parameters__gh_app_client_secret="-"
export Parameters__allowed_user_logins="-"
export Parameters__allowed_org_logins="-"
```

On Windows PowerShell, use `$env:Azure__SubscriptionId = ...` (and the same pattern for the other variables) instead of `export`.

3. Preview, then deploy:

```bash
dotnet build SoloDevBoard.slnx --configuration Release

aspire deploy --list-steps \
  --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj \
  --environment Production \
  --non-interactive

aspire deploy \
  --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj \
  --environment Production \
  --non-interactive
```

4. Open the Container App FQDN from the Aspire output or Azure portal. Expect the dashboard shell with a **Connected as @login** chip — not the `/welcome` hosted landing page.
5. Optionally verify PAT connectivity: `curl -sf "https://<fqdn>/health/github"`.

Secret parameters (`gh-pat`, and `gh-app-client-secret` when used) are written to the Aspire-provisioned `auth-secrets` Key Vault and referenced by the Container App. You do not create Key Vault secrets by hand.

### PAT-mode parameter and environment variable cheat sheet

| AppHost parameter | CD / local env var | Self-hoster (PAT) value |
|---|---|---|
| `hosted-sign-in-enabled` | `Parameters__hosted_sign_in_enabled` / `HOSTED_SIGN_IN_ENABLED` | `false` |
| `gh-pat` | `Parameters__gh_pat` / `GH_PAT` | **your PAT** (secret) |
| `gh-app-client-id` | `Parameters__gh_app_client_id` / `GH_APP_CLIENT_ID` | `-` |
| `gh-app-client-secret` | `Parameters__gh_app_client_secret` / `GH_APP_CLIENT_SECRET` | `-` |
| `hosted-admission-enabled` | `Parameters__hosted_admission_enabled` / `HOSTED_ADMISSION_ENABLED` | ignored |
| `allowed-user-logins` | `Parameters__allowed_user_logins` / `ALLOWED_USER_LOGINS` | `-` |
| `allowed-org-logins` | `Parameters__allowed_org_logins` / `ALLOWED_ORG_LOGINS` | `-` |

`DocsCapture__Enabled` is a **local-only** application setting for documentation screenshots. It is intentionally **not** an AppHost parameter and must not be enabled on hosted deployments. See [Docs capture mode](getting-started.md#docs-capture-mode) and [DEC-020](../plan/DECISIONS.md#dec-020-public-only-docs-capture-mode-for-documentation-screenshots).

See [`src/SoloDevBoard.AppHost/README.md`](../src/SoloDevBoard.AppHost/README.md) for the full parameter reference, [Azure Deployment Costs](azure-costs.md) for cost guidance, and [PAT Connectivity](pat-connectivity.md) for shell status and `/health/github`.

---

## One-time Azure setup

The steps below are required for **GitHub Actions CD**. For a personal first deploy with local `aspire deploy` only, create the resource group (step 1) and skip OIDC (step 2) until you want CI/CD.

### 1. Create a resource group

```bash
az group create \
  --name rg-solodevboard-prod \
  --location uksouth
```

Replace the name and region as needed. Set `AZURE_RESOURCE_GROUP` in each GitHub Environment (`staging`, `production`) to this same value.

### 2. Create a GitHub Actions OIDC identity

Aspire does not create the federated credential for GitHub Actions. Run these commands once per subscription and repository.

Set variables for your environment:

```bash
RESOURCE_GROUP="rg-solodevboard-prod"
LOCATION="uksouth"
GITHUB_ORG="markheydon"
GITHUB_REPO="solo-dev-board"
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

Aspire deploy also creates managed-identity role assignments (for example Key Vault and Container Registry access). Grant **User Access Administrator** on the same resource group scope so the CD identity can create those assignments:

```bash
az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "User Access Administrator" \
  --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP"
```

Create a federated credential for each GitHub Environment (`staging`, `production`):

```bash
CLIENT_ID="$(az identity show --name "$IDENTITY_NAME" --resource-group "$RESOURCE_GROUP" --query clientId -o tsv)"

for GITHUB_ENV in staging production; do
  az identity federated-credential create \
    --name "github-${GITHUB_ENV}" \
    --identity-name "$IDENTITY_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --issuer "https://token.actions.githubusercontent.com" \
    --subject "repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${GITHUB_ENV}" \
    --audiences "api://AzureADTokenExchange"
done
```

### 3. Configure GitHub Environments

Create two GitHub Environments in **Settings → Environments**: `staging` and `production`. Each uses the same Azure OIDC secrets and `AZURE_RESOURCE_GROUP` variable. Both use GitHub App hosted sign-in (see table below).

**Shared Azure secrets (both environments)**

| Secret | Value |
|---|---|
| `AZURE_CLIENT_ID` | Managed identity `clientId` from step 2 |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |

**Hosted sign-in (`staging` and `production` environments)**

| Secret / variable | Value |
|---|---|
| Secret `GH_PAT` | `-` |
| Secret `GH_APP_CLIENT_SECRET` | GitHub App client secret |
| Variable `HOSTED_SIGN_IN_ENABLED` | `true` |
| Variable `HOSTED_ADMISSION_ENABLED` | `true` |
| Variable `GH_APP_CLIENT_ID` | your client ID |
| Variable `ALLOWED_USER_LOGINS` | your login(s), or `-` |
| Variable `ALLOWED_ORG_LOGINS` | org login(s), or `-` |

Secret parameters are written to the Aspire-provisioned `auth-secrets` Key Vault at deploy time and referenced by the Container App. You do not create or manage Key Vault secrets manually for the default deployment path.

**Shared Azure variables (both environments)**

| Variable | Example | Purpose |
|---|---|---|
| `AZURE_LOCATION` | `uksouth` | Azure region for `aspire deploy` |
| `AZURE_RESOURCE_GROUP` | `rg-solodevboard-prod` | Target resource group (shared across tiers) |
| `SHARED_ACR_NAME` | *(unset)* or `acrmyorgprod` | Optional shared Container Registry name; leave unset for Aspire-provisioned per-deployment registry |
| `SHARED_ACR_RESOURCE_GROUP` | *(unset)* or `rg-platform-shared` | Resource group containing the shared registry; required when `SHARED_ACR_NAME` is set |
| `HOSTED_CALLBACK_BASE_URI` | *(unset)* or `https://staging.solodevboard.app` | Optional public HTTPS origin for GitHub App OAuth callbacks when using a custom domain |

Enable required reviewers on the `production` environment before granting production deploy access. Staging deploys automatically on merge to `main`; production deploys on `v*` release tags.

### Optional shared Container Registry

By default, Aspire provisions a Basic Azure Container Registry in the app resource group for each Aspire deploy environment (`Staging`, `Production`). That is convenient for contributors but can multiply fixed ACR costs across tiers and projects.

To use one central registry in a dedicated platform resource group instead, set `SHARED_ACR_NAME` and `SHARED_ACR_RESOURCE_GROUP` on the GitHub Environment (or export the matching `Parameters__*` values for local deploys). When both are set, the AppHost references the existing registry via `PublishAsExisting` and attaches it to the Container Apps environment. When unset, behaviour is unchanged.

**One-time platform setup**

1. Create the shared ACR once in your platform resource group (for example `rg-platform-shared`). Basic tier is sufficient unless you need geo-replication or advanced features.
2. Grant the CD deploy managed identity (`AZURE_CLIENT_ID`) **AcrPush** on the shared ACR. Contributor on the app resource group remains unchanged.
3. After the first shared deploy, verify Aspire assigned **AcrPull** to the Container Apps environment managed identity on the shared registry (Azure portal → Access control (IAM)).

**Migration from per-deployment registries**

After a successful deploy using the shared registry, manually delete orphaned `Microsoft.ContainerRegistry/registries` resources in the app resource group. Aspire does not remove them automatically.

| AppHost parameter | CD / local env var | Shared ACR value |
|---|---|---|
| `shared-acr-name` | `Parameters__shared_acr_name` / `SHARED_ACR_NAME` | registry name (for example `acrmyorgprod`) |
| `shared-acr-resource-group` | `Parameters__shared_acr_resource_group` / `SHARED_ACR_RESOURCE_GROUP` | platform resource group (for example `rg-platform-shared`) |

Leave both unset (or set to `-` locally) to keep the default Aspire-provisioned registry. See [DEC-022](../plan/DECISIONS.md#dec-022-optional-shared-azure-container-registry) and [Azure Deployment Costs](azure-costs.md#shared-container-registry).

---

## Deploy from GitHub Actions

The CD workflow (`.github/workflows/cd.yml`) runs `aspire deploy` with OIDC authentication across two hosted tiers (see [CD pipeline tiers](#cd-pipeline-tiers-dec-021)).

| Tier | How to trigger |
|---|---|
| **Staging** | Merge to `main`, or **Actions → CD - Deploy to Azure → Run workflow** (manual) |
| **Production** | Push a `v*` release tag (requires `production` environment approval if reviewers are configured) |

After a successful deploy:

1. Note the deployed Container App FQDN from the workflow output or Azure portal.
2. **Hosted sign-in only:** register the GitHub App callback URL: `https://<fqdn>/auth/callback` (staging and production each have their own FQDN). When using a custom domain, set `HOSTED_CALLBACK_BASE_URI` on the GitHub Environment to the public HTTPS origin (for example `https://staging.solodevboard.app`) and register `https://<custom-domain>/auth/callback` on the GitHub App.
3. Open the provisioned Application Insights resource to confirm telemetry is flowing (see [Observability guide](observability.md)).
4. **Hosted sign-in smoke checks** (run in a private browser window with no existing cookies):
   - `GET https://<fqdn>/` redirects to `/welcome` (not the Home dashboard shell).
   - Sign in with an allow-listed GitHub account completes and returns to the app.
   - One feature page (for example **Repositories**) loads GitHub data without errors.
   - Sign out returns to `/welcome`.
5. **Container App environment verification:** confirm `GitHubAuth__HostedGitHubAppClientId` and `HostedAdmissionControl__AllowedUserLogins` on the active revision show real values (not `-` placeholders from `appsettings.Staging.json`).

## Deploy locally (operator testing)

### Hosted sign-in

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

### PAT self-hoster

Use the environment exports and commands under [Deploy locally with aspire deploy](#deploy-locally-with-aspire-deploy-recommended-for-personal-instances).

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
| Azure Container Registry | Stores built container images (Aspire-provisioned per deployment, or optional shared registry — see [Optional shared Container Registry](#optional-shared-container-registry)) |
| Azure Key Vault (`auth-secrets`) | Stores hosted auth secret parameters as Key Vault secrets |
| Application Insights | Application logs, metrics, and distributed traces |
| Log Analytics workspace | Container platform logs and Application Insights backing store |
| Aspire dashboard | Optional operational dashboard (Aspire default) |
| Managed identities | Image pull and runtime authentication |

See [Azure Deployment Costs](azure-costs.md) for cost guidance.

---

## Health checks and Container Apps probes

SoloDevBoard exposes two HTTP health endpoints via `SoloDevBoard.ServiceDefaults`:

| Endpoint | Purpose | Checks included |
|---|---|---|
| `GET /health` | **Readiness** — the app can accept traffic after startup | All registered health checks |
| `GET /alive` | **Liveness** — the process is responsive | Checks tagged `live` only (the built-in `self` check) |

Both endpoints return `200 OK` with a `Healthy` body when checks pass. They are excluded from distributed tracing to reduce noise (see [Observability](observability.md)).

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

Aspire applies this as the Container App HTTP probe during `aspire deploy`. No manual Bicep or portal configuration is required for the default deployment path. Aspire wires `/health` as the platform probe; `/alive` remains available for manual liveness checks but is not configured as a separate ACA probe in the default AppHost model.

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

To remove Aspire-managed resources for a specific tier, pass the matching Aspire environment name:

```bash
# Example: tear down production resources only
aspire destroy \
  --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj \
  --environment Production \
  --yes \
  --non-interactive
```

Use `Staging` or `Production` to target the corresponding hosted tier. All tiers share the same resource group, so confirm the subscription, resource group, and environment before running destroy. Verify removal with `az resource list --resource-group <rg>`.

The OIDC managed identity is not removed by `aspire destroy`. Delete it manually if no longer needed:

```bash
az identity delete --name id-solodevboard-cd-prod --resource-group rg-solodevboard-prod
```

---

## Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| OIDC login fails in CD | Federated credential subject mismatch | Verify subject is `repo:<owner>/<repo>:environment:<staging|production>` |
| Missing parameter prompt in CI | Secret not mapped | Add `Parameters__*` env vars to the deploy step |
| Cold start / SignalR disconnect | Scale-to-zero idle | Expected; refresh the page or wait for the container to warm up |
| 403 after sign-in | Allow-list | Update `ALLOWED_USER_LOGINS` or `ALLOWED_ORG_LOGINS` |
| Callback URL mismatch | Stale GitHub App setting | Update callback to `https://<aca-fqdn>/auth/callback` (hosted sign-in only) |
| Secret not visible in Container App settings | Key Vault reference | Expected — inspect the `auth-secrets` vault for secret names; values are resolved at runtime |
| Home dashboard shown without sign-in | Missing login gate or placeholder deploy parameters | Confirm `/` redirects to `/welcome`; redeploy with `Parameters__*` env vars mapped in CD; verify Container App env shows real client ID and allow-list values. CI `e2e-hosted` job asserts this gate with placeholder credentials. |
| PAT mode shows `/welcome` or requires sign-in | `hosted-sign-in-enabled` still `true` | Set `HOSTED_SIGN_IN_ENABLED` / `Parameters__hosted_sign_in_enabled` to `false` and redeploy |
| PAT mode starts but GitHub calls fail | Missing or invalid `GH_PAT` | Set a real PAT with required scopes; confirm `/health/github` and the **Connected as @login** chip |
| Image pull fails after enabling shared ACR | Missing AcrPush or AcrPull on shared registry | Grant AcrPush to the CD identity and verify AcrPull on the Container Apps managed identity |
| Deploy fails with shared ACR parameter error | `SHARED_ACR_NAME` set without resource group | Set both `SHARED_ACR_NAME` and `SHARED_ACR_RESOURCE_GROUP` |

For local development, see [Getting Started](getting-started.md) (including [PAT-only local trusted mode](getting-started.md#pat-only-local-trusted-mode)). For hosted authentication details, see [Hosted Authentication](hosted-authentication.md). For the Key Vault pattern, see [plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md](../plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md).
