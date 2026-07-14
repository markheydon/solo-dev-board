# SoloDevBoard AppHost

Aspire orchestrates the SoloDevBoard web app for local development, dev containers, and Codespaces. GitHub authentication is configured through **AppHost parameters**, which are injected into the `app` resource as environment variables.

Use `-` on inactive parameters. Shipped defaults are in `SoloDevBoard.AppHost/appsettings.json`; values saved from the Aspire dashboard are stored in user secrets and **override** those defaults. Do not rely on AppHost code defaults for parameters you change in the dashboard.

## Choose your authentication mode

### PAT mode (default — solo local development)

Set `github-pat` to your token. Set all hosted-sign-in parameters to `-`:

| Parameter | Value |
|---|---|
| `hosted-sign-in-enabled` | `false` |
| `github-pat` | your PAT |
| `github-app-client-id` | `-` |
| `github-app-client-secret` | `-` |
| `allowed-user-logins` | `-` |
| `allowed-org-logins` | `-` |

### Hosted sign-in mode (GitHub App)

Set `github-pat` to `-`. Configure GitHub App credentials and allow-lists:

| Parameter | Value |
|---|---|
| `hosted-sign-in-enabled` | `true` |
| `github-pat` | `-` |
| `github-app-client-id` | your Client ID |
| `github-app-client-secret` | your client secret |
| `allowed-user-logins` | your login(s), or `-` |
| `allowed-org-logins` | org login(s), or `-` |

Register `{https-endpoint}/auth/callback` on your GitHub App. Run `aspire describe` for the current HTTPS URL.

See [docs/getting-started.md](../docs/getting-started.md) for full setup and [Switching between modes](../docs/getting-started.md#switching-between-pat-and-hosted-sign-in).

## Parameter reference

| Parameter | Secret | Default | PAT mode | Hosted sign-in |
|---|---|---|---|---|
| `hosted-sign-in-enabled` | no | `false` | `false` | `true` |
| `github-pat` | yes | *(none)* | **your PAT** | `-` (dashboard) |
| `github-app-client-id` | no | `-` | `-` | **client ID** |
| `github-app-client-secret` | yes | *(none)* | `-` (dashboard) | **client secret** |
| `hosted-admission-enabled` | no | `true` | ignored | `true` (recommended) |
| `allowed-user-logins` | no | `-` | `-` | logins or `-` |
| `allowed-org-logins` | no | `-` | `-` | logins or `-` |

Startup validation fails fast if required values are missing or inactive-mode parameters are not `-`.

## Start the app

```bash
aspire start --apphost SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
aspire describe
```

Open the `app` resource URL shown by `aspire describe`.
