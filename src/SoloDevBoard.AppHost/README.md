# SoloDevBoard AppHost

Aspire orchestrates the SoloDevBoard web app for local development, dev containers, Codespaces, and **production deployment to Azure Container Apps** ([DEC-015](../../plan/DECISIONS.md#dec-015-aspire-azure-container-apps-deployment)).

GitHub authentication is configured through **AppHost parameters**. In local development they are injected into the `app` resource as environment variables. In deploy mode, secret parameters are persisted in Azure Key Vault and referenced by the Container App.

Use `-` on inactive parameters. Shipped defaults are in `src/SoloDevBoard.AppHost/appsettings.json`; staging and production non-secret defaults are in `appsettings.Staging.json` and `appsettings.Production.json` respectively. Values saved from the Aspire dashboard are stored in user secrets and **override** those defaults.

## Choose your authentication mode

### PAT-only local trusted mode (default — solo local development and personal self-hosting)

Set `gh-pat` to your token. Set all hosted-sign-in parameters to `-`:

| Parameter | Value |
|---|---|
| `hosted-sign-in-enabled` | `false` |
| `gh-pat` | your PAT |
| `gh-app-client-id` | `-` |
| `gh-app-client-secret` | `-` |
| `allowed-user-logins` | `-` |
| `allowed-org-logins` | `-` |

See [docs/getting-started.md — PAT-only local trusted mode](../docs/getting-started.md#pat-only-local-trusted-mode) for the mode comparison and local setup. For a personal Azure instance with the same mode, see [Self-hoster deployment (PAT mode)](../docs/deployment.md#self-hoster-deployment-pat-mode).

### Hosted sign-in mode (GitHub App)

Set `gh-pat` to `-`. Configure GitHub App credentials and allow-lists:

| Parameter | Value |
|---|---|
| `hosted-sign-in-enabled` | `true` |
| `gh-pat` | `-` |
| `gh-app-client-id` | your Client ID |
| `gh-app-client-secret` | your client secret |
| `allowed-user-logins` | your login(s), or `-` |
| `allowed-org-logins` | org login(s), or `-` |

Register `{https-endpoint}/auth/callback` on your GitHub App. Run `aspire describe` for the current HTTPS URL.

See [docs/getting-started.md](../docs/getting-started.md) for full setup and [Switching between modes](../docs/getting-started.md#switching-between-pat-and-hosted-sign-in).

## Parameter reference

| Parameter | Secret | Default | PAT mode | Hosted sign-in |
|---|---|---|---|---|
| `hosted-sign-in-enabled` | no | `false` | `false` | `true` |
| `gh-pat` | yes | *(none)* | **your PAT** | `-` (dashboard) |
| `gh-app-client-id` | no | `-` | `-` | **client ID** |
| `gh-app-client-secret` | yes | *(none)* | `-` (dashboard) | **client secret** |
| `hosted-admission-enabled` | no | `true` | ignored | `true` (recommended) |
| `allowed-user-logins` | no | `-` | `-` | logins or `-` |
| `allowed-org-logins` | no | `-` | `-` | logins or `-` |

Startup validation fails fast if required values for the active authentication mode are missing. Setting inactive-mode parameters to `-` is recommended when switching modes but is not enforced at startup.

## Local development

```bash
aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
aspire describe
```

Open the `app` resource URL shown by `aspire describe`.

Application Insights is deploy-time only. Local telemetry uses the Aspire dashboard via OTLP; no Azure subscription or deployment metadata is required.

## CD pipeline tiers

GitHub Actions CD deploys to two hosted tiers sharing one Azure resource group (see [DEC-021](../../plan/DECISIONS.md#dec-021-two-tier-cd-pipeline-with-shared-azure-resource-group) and [docs/deployment.md](../../docs/deployment.md#cd-pipeline-tiers-dec-021)). PAT mode is for local development and personal self-hosting via local `aspire deploy` only.

| Tier | Aspire `--environment` | Authentication |
|---|---|---|
| Staging (push to `main`) | `Staging` | GitHub App hosted sign-in |
| Production (push tag `v*`) | `Production` | GitHub App hosted sign-in |

## Production deployment

Deployment uses `aspire deploy` to Azure Container Apps with scale-to-zero. Application Insights and structured logging are provisioned automatically. See [docs/deployment.md](../docs/deployment.md) and [docs/observability.md](../docs/observability.md) for the full operator guide.

### Health probes

The `app` resource registers `GET /health` as the Container App readiness probe (`.WithHttpHealthCheck("/health")`). A separate `GET /alive` liveness endpoint is also available for manual checks but is not wired as an additional ACA probe in the default AppHost model. Neither probe calls GitHub or other external dependencies — see [Health checks and Container Apps probes](../docs/deployment.md#health-checks-and-container-apps-probes) for the design rationale.

### Key Vault-backed auth secrets

In publish/deploy mode, hosted authentication secrets are persisted in an Aspire-provisioned `auth-secrets` Key Vault and injected into the Container App as Key Vault references — not plain-text app settings. This applies to:

| Secret parameter | Key Vault secret name | App configuration key |
|---|---|---|
| `gh-pat` | `gh-pat` | `GitHubAuth__PersonalAccessToken` |
| `gh-app-client-secret` | `gh-app-client-secret` | `GitHubAuth__HostedGitHubAppClientSecret` |

Local `aspire start` continues to bind these parameters directly from user secrets or the Aspire dashboard. Deploy-time input is unchanged: supply `Parameters__gh_pat` and `Parameters__gh_app_client_secret` as before.

See [plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md](../../plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md) for the full pattern.

### Optional shared Container Registry

Deploy-only parameters `shared-acr-name` and `shared-acr-resource-group` default to `-`. When both are set at deploy time, the AppHost references an existing Azure Container Registry in a platform resource group instead of letting Aspire provision a per-deployment registry. Leave unset for the default stress-free path. See [docs/deployment.md — Optional shared Container Registry](../../docs/deployment.md#optional-shared-container-registry) and [DEC-022](../../plan/DECISIONS.md#dec-022-optional-shared-azure-container-registry).

Preview deployment steps:

```bash
aspire deploy --list-steps \
  --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj \
  --environment Production \
  --non-interactive
```

### GitHub Actions parameter mapping

AppHost parameters map to workflow environment variables with underscores instead of dashes:

| AppHost parameter | CD environment variable |
|---|---|
| `hosted-sign-in-enabled` | `Parameters__hosted_sign_in_enabled` |
| `gh-pat` | `Parameters__gh_pat` |
| `gh-app-client-id` | `Parameters__gh_app_client_id` |
| `gh-app-client-secret` | `Parameters__gh_app_client_secret` |
| `hosted-admission-enabled` | `Parameters__hosted_admission_enabled` |
| `allowed-user-logins` | `Parameters__allowed_user_logins` |
| `allowed-org-logins` | `Parameters__allowed_org_logins` |
| `shared-acr-name` | `Parameters__shared_acr_name` / `SHARED_ACR_NAME` |
| `shared-acr-resource-group` | `Parameters__shared_acr_resource_group` / `SHARED_ACR_RESOURCE_GROUP` |

Azure deployment settings:

| Setting | Environment variable |
|---|---|
| Subscription | `Azure__SubscriptionId` |
| Region | `Azure__Location` |
| Resource group | `Azure__ResourceGroup` |

### Local-only application settings (not AppHost parameters)

| Setting | Environment variable | Notes |
|---|---|---|
| Docs capture mode | `DocsCapture__Enabled` | Local screenshot hygiene only. Defaults to `false`. Restricts repository and project board catalogues to public GitHub content. **Not** wired as an AppHost parameter and must stay disabled on hosted deployments. See [Docs capture mode](../../docs/getting-started.md#docs-capture-mode) and [DEC-020](../../plan/DECISIONS.md#dec-020-public-only-docs-capture-mode-for-documentation-screenshots). |
