> **Audience:** Developers and operators. This guide is repository documentation and is not part of the published end-user site in `website/`.

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

GitHub Actions CD (`.github/workflows/cd.yml`) deploys to two hosted tiers. Aspire `--environment` does **not** suffix Azure resource names on its own; the AppHost suffixes Staging resources (`aca-staging`, `app-staging`, and so on) so they do not overwrite Production when both tiers target the **same** app resource group. Production keeps the original names (`aca`, `app`) so an existing production Container App is not recreated. The **app resource group** is an operator choice per GitHub Environment — use one RG for both tiers or separate RGs. Both tiers use GitHub App hosted sign-in. PAT-only mode is for local development and personal self-hosting via local `aspire deploy` only — not as a hosted CD tier.

| Tier | Trigger | GitHub Environment | Aspire `--environment` | Authentication |
|---|---|---|---|---|
| **Staging** | Merge to `main`, or **Actions → CD - Deploy to Azure → Run workflow** (manual staging deploy) | `staging` | `Staging` | GitHub App hosted sign-in |
| **Production** | Push tag `v*` | `production` | `Production` | GitHub App hosted sign-in |

The public product site in `website/` is published to GitHub Pages on `v*` release tags only. Pull requests validate Hugo builds via `hugo-validate.yml` without publishing. Canonical URL: `https://solodevboard.com/` (see [website/README.md](../website/README.md#custom-domain-solodevboardcom)).

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
2. A GitHub PAT with scopes `repo`, `read:org`, `workflow`, `read:project`, and `project` when applying One-Click Migration project board columns (same as local development).
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

4. Open the Container App FQDN from the Aspire output or Azure portal. Expect the dashboard shell with a **@login** chip — not the `/welcome` hosted landing page.
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

Choose an OIDC layout:

| Path | When to use |
|---|---|
| **[Default (no shared ACR)](#2-create-a-github-actions-oidc-identity-default)** | Forks, first-time self-hosters, or anyone who omits `ACR_NAME` / `ACR_RESOURCE_GROUP`. The CD identity lives in the app resource group. |
| **[Shared ACR (opt-in)](#2-create-a-github-actions-oidc-identity-shared-acr)** | Hosted Staging and Production share one existing registry (see [DEC-025](../plan/DECISIONS.md#dec-025-optional-shared-azure-container-registry)). The CD identity lives in the **ACR resource group**. |

### 1. Create app resource group(s)

Create one or two resource groups for Container Apps, Key Vault, and Application Insights. Set `AZURE_RESOURCE_GROUP` on each GitHub Environment (`staging`, `production`) to the app RG for that tier.

```bash
az group create \
  --name rg-solodevboard-staging \
  --location uksouth

az group create \
  --name rg-solodevboard-prod \
  --location uksouth
```

Use one RG for both tiers or separate RGs — your choice. Replace names and region as needed.

### 2. Create a GitHub Actions OIDC identity (default)

Aspire does **not** create the federated credential for GitHub Actions. Run these commands once per subscription and repository when you are **not** using a shared ACR.

Set variables for your environment:

```bash
RESOURCE_GROUP="rg-solodevboard-prod"
LOCATION="uksouth"
GITHUB_ORG="markheydon"
GITHUB_REPO="solo-dev-board"
IDENTITY_NAME="id-solodevboard-cd"
```

Create a user-assigned managed identity in the **app** resource group:

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

Repeat the Contributor and User Access Administrator assignments for each **app** resource group if Staging and Production use different groups.

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

### 2. Create a GitHub Actions OIDC identity (shared ACR)

When using an existing shared Azure Container Registry (see [Optional shared Container Registry](#optional-shared-container-registry)), create the CD identity in the **ACR resource group** so tearing down an app RG does not remove OIDC.

```bash
ACR_RG="<shared-acr-resource-group>"
ACR_NAME="<acr-name>"
LOCATION="uksouth"
GITHUB_ORG="markheydon"
GITHUB_REPO="solo-dev-board"
IDENTITY_NAME="id-solodevboard-cd"
APP_RG_STAGING="rg-solodevboard-staging"
APP_RG_PROD="rg-solodevboard-prod"

az identity create --name "$IDENTITY_NAME" --resource-group "$ACR_RG" --location "$LOCATION"
PRINCIPAL_ID="$(az identity show --name "$IDENTITY_NAME" --resource-group "$ACR_RG" --query principalId -o tsv)"
ACR_ID="$(az acr show --name "$ACR_NAME" --resource-group "$ACR_RG" --query id -o tsv)"
SUBSCRIPTION_SCOPE="/subscriptions/$(az account show --query id -o tsv)"

for APP_RG in "$APP_RG_STAGING" "$APP_RG_PROD"; do
  az role assignment create \
    --assignee-object-id "$PRINCIPAL_ID" \
    --assignee-principal-type ServicePrincipal \
    --role Contributor \
    --scope "${SUBSCRIPTION_SCOPE}/resourceGroups/${APP_RG}"
  az role assignment create \
    --assignee-object-id "$PRINCIPAL_ID" \
    --assignee-principal-type ServicePrincipal \
    --role "User Access Administrator" \
    --scope "${SUBSCRIPTION_SCOPE}/resourceGroups/${APP_RG}"
done

az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role AcrPush \
  --scope "$ACR_ID"

az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "User Access Administrator" \
  --scope "$ACR_ID"
```

Then create federated credentials as in the [default path](#2-create-a-github-actions-oidc-identity-default), using `$ACR_RG` as the identity resource group.

Two identities participate at deploy time:

| Principal | Created by | Needs on shared ACR | Needs on app RG |
|---|---|---|---|
| CD user-assigned MI | You (`az identity create`) | AcrPush + User Access Administrator (registry scope) | Contributor + User Access Administrator |
| ACA `acr-pull` user-assigned MI | Aspire during `aspire deploy` (`WithAcrPullIdentity`) | AcrPull (provisioned in a separate Bicep module) | Lives in the app RG |

The CD identity pushes images. The Container Apps environment identity pulls them at runtime. Do not grant AcrPush to the running Container App.

### 3. Configure GitHub Environments

Create two GitHub Environments in **Settings → Environments**: `staging` and `production`. Each uses the same Azure OIDC secrets. Set `AZURE_RESOURCE_GROUP` per Environment to that tier's app resource group. Both use GitHub App hosted sign-in (see table below).

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
| `AZURE_RESOURCE_GROUP` | `rg-solodevboard-staging` / `rg-solodevboard-prod` | App resource group for this Environment (may differ per tier) |
| `ACR_NAME` | *(unset)* or `acrmyshared` | Optional existing ACR resource name — set with `ACR_RESOURCE_GROUP` or omit both |
| `ACR_RESOURCE_GROUP` | *(unset)* or `rg-shared-acr` | Resource group that owns the existing ACR — same value on both Environments when opting in |
| `HOSTED_CALLBACK_BASE_URI` | *(unset)* or `https://staging.example.com` | Public HTTPS origin for GitHub App OAuth callbacks when using a custom domain; omit to use the Aspire-provisioned FQDN |
| `CUSTOM_DOMAIN` | *(unset)* or `staging.example.com` | Optional Container App custom hostname; must match DNS and managed certificate |
| `CUSTOM_DOMAIN_CERTIFICATE_NAME` | *(unset)* or `staging-example-com` | Managed certificate name in the Container Apps environment; leave unset on first deploy before the certificate exists |

Prefer **repository-level** variables for `ACR_NAME` and `ACR_RESOURCE_GROUP` so Staging and Production cannot drift onto different registries.

Enable required reviewers on the `production` environment before granting production deploy access. Staging deploys automatically on merge to `main`; production deploys on `v*` release tags.

### Fork and independent operators

Shipped `appsettings.Staging.json` and `appsettings.Production.json` contain **tier defaults only** (for example `hosted-sign-in-enabled: true` for CD tiers). They do not contain operator-specific hostnames, OAuth callback URLs, custom domains, or GitHub allow-lists.

Configure **your** instance via GitHub Environment secrets and variables (for CD) or `Parameters__*` environment variables (for local `aspire deploy`). Do not commit operator-specific values into appsettings files in a fork.

| You must set (hosted CD) | Purpose |
|---|---|
| `GH_APP_CLIENT_ID`, `GH_APP_CLIENT_SECRET` | GitHub App OAuth |
| `ALLOWED_USER_LOGINS` and/or `ALLOWED_ORG_LOGINS` | Admission allow-list |
| `AZURE_*` secrets and `AZURE_LOCATION`, `AZURE_RESOURCE_GROUP` | Azure deploy target |

| Set when using a custom domain | Purpose |
|---|---|
| `HOSTED_CALLBACK_BASE_URI` | Public HTTPS origin for OAuth callbacks (`https://<hostname>`) |
| `CUSTOM_DOMAIN` | Container App hostname |
| `CUSTOM_DOMAIN_CERTIFICATE_NAME` | Managed certificate name in the ACA environment (after the cert exists) |

Without a custom domain, leave callback and domain variables unset; the AppHost uses the Aspire-provisioned `*.azurecontainerapps.io` FQDN for OAuth callbacks.

### Custom domain (Container Apps)

Aspire can persist a custom hostname and managed certificate binding across `aspire deploy` runs via `ConfigureCustomDomain` in the AppHost. Without this, redeployments can remove the custom domain from the Container App.

**One-time DNS and certificate setup (per hostname)**

1. Add a **CNAME** from your hostname to the Container App default FQDN (for example `staging` → `app.<env>.<region>.azurecontainerapps.io`).
2. Add the **TXT** validation record shown by Azure (`asuid.<hostname>`).
3. Add the hostname: `az containerapp hostname add --hostname <hostname> --name app --resource-group <rg>`.
4. Create a managed certificate in the Container Apps environment:
   `az containerapp env certificate create --name <aca-env> --resource-group <rg> --hostname <hostname> --validation-method CNAME --certificate-name <cert-name>`.
5. Bind the certificate: `az containerapp hostname bind --hostname <hostname> --name app --resource-group <rg> --environment <aca-env> --certificate <cert-name>`.

Leave `CUSTOM_DOMAIN` and `CUSTOM_DOMAIN_CERTIFICATE_NAME` unset (or `-`) until the managed certificate exists in that tier's Container Apps environment. Set both via GitHub Environment variables or `Parameters__*` environment variables only — not in shipped appsettings files. Shipping a hostname without a matching certificate causes `CertificateNotFound` during deploy.

**AppHost and CD configuration**

Set both parameters so subsequent deploys preserve the binding. The AppHost calls `ConfigureCustomDomain` only when **both** are active:

| AppHost parameter | CD / local env var | Example |
|---|---|---|
| `custom-domain` | `Parameters__custom_domain` / `CUSTOM_DOMAIN` | `staging.example.com` |
| `custom-domain-certificate-name` | `Parameters__custom_domain_certificate_name` / `CUSTOM_DOMAIN_CERTIFICATE_NAME` | `staging-example-com` |

When using a custom domain for hosted sign-in, also set `HOSTED_CALLBACK_BASE_URI` to the public HTTPS origin and register `https://<hostname>/auth/callback` on the GitHub App.

Leave both domain parameters unset to use the Aspire-provisioned FQDN only. On the first deploy with a new hostname, leave `CUSTOM_DOMAIN_CERTIFICATE_NAME` unset until the managed certificate exists; set it on subsequent deploys.

---

## Deploy from GitHub Actions

The CD workflow (`.github/workflows/cd.yml`) runs `aspire deploy` with OIDC authentication across two hosted tiers (see [CD pipeline tiers](#cd-pipeline-tiers-dec-021)).

Deploy jobs check out the repository with `fetch-depth: 0` so [MinVer](https://github.com/adamralph/minver) can stamp version metadata from git tags and commit history into the built application. See [Release Plan — Build-time versioning](../plan/RELEASE_PLAN.md#build-time-versioning-minver) for expected version shapes on staging and production.

| Tier | How to trigger |
|---|---|
| **Staging** | Merge to `main`, or **Actions → CD - Deploy to Azure → Run workflow** (manual) |
| **Production** | Push a `v*` release tag (requires `production` environment approval if reviewers are configured) |

After a successful deploy:

1. Note the deployed Container App FQDN from the workflow output or Azure portal.
2. **Hosted sign-in only:** register the GitHub App callback URL: `https://<fqdn>/auth/callback` (staging and production each have their own FQDN). When using a custom domain, set `HOSTED_CALLBACK_BASE_URI` on the GitHub Environment to the public HTTPS origin (for example `https://staging.example.com`) and register `https://<custom-domain>/auth/callback` on the GitHub App. Polish the app listing (logo, description, permissions) using [GitHub App listing](github-app.md).
3. Open the provisioned Application Insights resource to confirm telemetry is flowing (see [Observability guide](observability.md)).
4. **Hosted sign-in smoke checks** (run in a private browser window with no existing cookies):
   - `GET https://<fqdn>/` redirects to `/welcome` (not the Home dashboard shell).
   - Sign in with an allow-listed GitHub account completes and returns to the app.
   - One feature page (for example **Repositories**) loads GitHub data without errors.
   - Sign out returns to `/welcome`.
5. **Deployment version check:** open **More options → About**. Staging should show a version with a `staging` pre-release suffix (for example `1.0.1-staging.0.42`); production should show a clean SemVer matching the release tag (for example `1.0.0`). The **Build** line links to the deployed commit — compare it with the commit SHA from the GitHub Actions deploy run or the tip of the deployed branch/tag.
6. **Container App environment verification:** confirm `GitHubAuth__HostedGitHubAppClientId` and `HostedAdmissionControl__AllowedUserLogins` on the active revision show real values (not `-` placeholders). If they still show `-`, the GitHub Environment variables were not applied — see [Fork and independent operators](#fork-and-independent-operators).

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
| Azure Key Vault (`auth-secrets`, or `auth-secrets-staging`) | Stores hosted auth secret parameters as Key Vault secrets |
| Application Insights | Application logs, metrics, and distributed traces |
| Log Analytics workspace | Container platform logs and Application Insights backing store |
| Aspire dashboard | Optional operational dashboard (Aspire default) |
| Managed identities | Image pull and runtime authentication |

See [Azure Deployment Costs](azure-costs.md) for cost guidance.

---

## Optional shared Container Registry

Leave `ACR_NAME` and `ACR_RESOURCE_GROUP` **unset** unless you already manage a registry you want Staging and Production to share. When both are omitted, Aspire provisions a registry in the app resource group — the default for forks and self-hosters.

When both are set (repository-level GitHub variables recommended), the AppHost uses `PublishAsExisting`, `WithAzureContainerRegistry`, and `WithAcrPullIdentity` so `aspire deploy` pushes to your existing ACR instead of creating one per tier. The `acr-pull` user-assigned identity (or `acr-pull-staging` on Staging) receives `AcrPull` on the shared registry in a separate Bicep module, which avoids a cross-resource-group Bicep scope error ([Aspire #11256](https://github.com/dotnet/aspire/issues/11256)). Image repositories remain distinct (`app` vs `app-staging` from the AppHost `AzureName` suffix).

| AppHost parameter | CD / local env var | When opting in |
|---|---|---|
| `acr-name` | `Parameters__acr_name` / `ACR_NAME` | Azure resource name of the registry (not the login server) |
| `acr-resource-group` | `Parameters__acr_resource_group` / `ACR_RESOURCE_GROUP` | Resource group that owns the ACR |

Set both or neither. Setting exactly one fails the deploy with a clear error.

Operator bootstrap and RBAC for a shared registry are in [One-time Azure setup — shared ACR](#2-create-a-github-actions-oidc-identity-shared-acr). `aspire destroy` does **not** delete an existing shared registry. Delete leftover per-tier ACRs Aspire previously created in an app RG after the first successful shared-ACR deploy.

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

Use `Staging` or `Production` to target the corresponding hosted tier. Confirm the subscription, app resource group (`AZURE_RESOURCE_GROUP` for that Environment), and environment before running destroy. Verify removal with `az resource list --resource-group <rg>`.

The OIDC managed identity is not removed by `aspire destroy`. Delete it manually if no longer needed. When using a shared ACR, the identity lives in the ACR resource group:

```bash
az identity delete --name id-solodevboard-cd --resource-group <acr-resource-group>
```

---

## Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| OIDC login fails in CD | Stale `AZURE_CLIENT_ID` / tenant, or federated credential subject mismatch | CD logs in with OIDC immediately after checkout. Confirm secrets match the CD managed identity and that a federated credential exists for `repo:<owner>/<repo>:environment:<staging\|production>`. `AADSTS700016` usually means the client ID is not in that tenant. |
| ACR push or pull fails on shared registry | CD identity missing AcrPush or User Access Administrator on the registry | Grant both roles on the **ACR resource** scope. App-RG Contributor does not cover a registry in another group. See [shared ACR OIDC setup](#2-create-a-github-actions-oidc-identity-shared-acr). |
| Deploy fails with BCP139 on `aca` / `aca-staging` Bicep | Shared ACR in another resource group on an unfixed AppHost | Upgrade to a build that uses `WithAcrPullIdentity` for shared ACR (see [Optional shared Container Registry](#optional-shared-container-registry)). Do not rely on default `WithAzureContainerRegistry` alone for cross-RG registries. |
| Deploy fails: acr-name / acr-resource-group mismatch | Only one ACR parameter set | Set both `ACR_NAME` and `ACR_RESOURCE_GROUP`, or omit both for Aspire's default registry. |
| Aspire deploy fails with `CertificateNotFound` | `CUSTOM_DOMAIN` set but `CUSTOM_DOMAIN_CERTIFICATE_NAME` does not exist in that Container Apps environment | List certificates on the **production** (or staging) ACA environment and set the variable to the certificate **name**, not the hostname. Leave the cert name unset until the managed certificate exists. |
| Staging disappears after a Production deploy | Both tiers targeted Container App `app` in the shared resource group | Do not re-run staging CD on unfixed `main`. Deploy Staging only after the AppHost `-staging` resource-name suffix is merged, so it provisions `app-staging`. Production stays on `app`. |
| Missing parameter prompt in CI | Secret not mapped | Add `Parameters__*` env vars to the deploy step |
| Cold start / SignalR disconnect | Scale-to-zero idle | Expected; refresh the page or wait for the container to warm up |
| 403 after sign-in | Allow-list | Update `ALLOWED_USER_LOGINS` or `ALLOWED_ORG_LOGINS` |
| Callback URL mismatch | Stale GitHub App setting | Update callback to `https://<aca-fqdn>/auth/callback` (hosted sign-in only) |
| Secret not visible in Container App settings | Key Vault reference | Expected — inspect the `auth-secrets` vault for secret names; values are resolved at runtime |
| Home dashboard shown without sign-in | Missing login gate or placeholder deploy parameters | Confirm `/` redirects to `/welcome`; redeploy with `Parameters__*` env vars mapped in CD; verify Container App env shows real client ID and allow-list values. CI Playwright `hosted` job asserts this gate with placeholder credentials. |
| PAT mode shows `/welcome` or requires sign-in | `hosted-sign-in-enabled` still `true` | Set `HOSTED_SIGN_IN_ENABLED` / `Parameters__hosted_sign_in_enabled` to `false` and redeploy |
| PAT mode starts but GitHub calls fail | Missing or invalid `GH_PAT` | Set a real PAT with required scopes; confirm `/health/github` and the **@login** chip |

For local development, see [Getting Started](getting-started.md) (including [PAT-only local trusted mode](getting-started.md#pat-only-local-trusted-mode)). For hosted authentication details, see [Hosted Authentication](hosted-authentication.md). For the Key Vault pattern, see [plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md](../plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md).
