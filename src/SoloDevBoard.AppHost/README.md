# SoloDevBoard AppHost

Aspire orchestrates the SoloDevBoard web app for local development, dev containers, Codespaces, and **production deployment to Azure Container Apps** ([DEC-015](../../plan/DECISIONS.md#dec-015-aspire-azure-container-apps-deployment)).

GitHub authentication is configured through **AppHost parameters**, which are injected into the `app` resource as environment variables.

Use `-` on inactive parameters. Shipped defaults are in `src/SoloDevBoard.AppHost/appsettings.json`; production non-secret defaults are in `src/SoloDevBoard.AppHost/appsettings.Production.json`. Values saved from the Aspire dashboard are stored in user secrets and **override** those defaults.

## Choose your authentication mode

### PAT mode (default — solo local development)

Set `gh-pat` to your token. Set all hosted-sign-in parameters to `-`:

| Parameter | Value |
|---|---|
| `hosted-sign-in-enabled` | `false` |
| `gh-pat` | your PAT |
| `gh-app-client-id` | `-` |
| `gh-app-client-secret` | `-` |
| `allowed-user-logins` | `-` |
| `allowed-org-logins` | `-` |

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

## Production deployment

Deployment uses `aspire deploy` to Azure Container Apps with scale-to-zero. Application Insights and structured logging are provisioned automatically. See [docs/deployment.md](../docs/deployment.md) and [docs/user-guide/observability.md](../docs/user-guide/observability.md) for the full operator guide.

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

Azure deployment settings:

| Setting | Environment variable |
|---|---|
| Subscription | `Azure__SubscriptionId` |
| Region | `Azure__Location` |
| Resource group | `Azure__ResourceGroup` |
