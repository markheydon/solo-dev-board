# SoloDevBoard AppHost

Aspire orchestrates the SoloDevBoard web app for local development, dev containers, and Codespaces. GitHub authentication is configured through **AppHost parameters**, which are injected into the `app` resource as environment variables.

## Choose your authentication mode

SoloDevBoard supports two mutually exclusive modes. Configure only the parameters for the mode you are using; the other parameters keep their default placeholder (`__disabled__`).

### PAT mode (default — solo local development)

Leave `hosted-sign-in-enabled` as `false`. Set your GitHub personal access token:

```bash
aspire secret set Parameters:github-pat "<your-pat>"
aspire start --apphost SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
```

Your GitHub login is resolved automatically from the PAT at startup. You do not need to configure `github-owner-login`.

### Hosted sign-in mode (GitHub App — multi-tenant / production-like)

Set `hosted-sign-in-enabled` to `true` and configure the GitHub App OAuth credentials:

```bash
aspire secret set Parameters:github-app-client-secret "<client-secret>"
```

Set non-secret values via the dashboard, `appsettings.json`, or user secrets:

```json
"Parameters": {
  "hosted-sign-in-enabled": "true",
  "github-app-client-id": "<client-id>",
  "allowed-user-logins": "<login1>,<login2>",
  "allowed-org-logins": "<org1>,<org2>"
}
```

Leave `github-pat` at `__disabled__`.

The callback base URI (`GitHubAuth__HostedSignInCallbackBaseUri`) is derived automatically from the app's Aspire HTTPS endpoint. Register `{https-endpoint}/auth/callback` as the callback URL on your GitHub App.

See [docs/getting-started.md](../docs/getting-started.md) and [docs/user-guide/hosted-authentication.md](../docs/user-guide/hosted-authentication.md) for full setup instructions.

## Parameter reference

| Parameter | Secret | Default | PAT mode | Hosted sign-in |
|---|---|---|---|---|
| `hosted-sign-in-enabled` | no | `false` | `false` | `true` |
| `github-pat` | yes | `__disabled__` | **Set your PAT** | leave default |
| `github-app-client-id` | no | `__disabled__` | leave default | **Set client ID** |
| `github-app-client-secret` | yes | `__disabled__` | leave default | **Set client secret** |
| `hosted-admission-enabled` | no | `true` | ignored | `true` (recommended) |
| `allowed-user-logins` | no | `__disabled__` | ignored | **Set allow-list** |
| `allowed-org-logins` | no | `__disabled__` | ignored | optional |

Non-secret defaults live in [`appsettings.json`](appsettings.json). Secret values are stored via `aspire secret set` or the AppHost user secrets store (`UserSecretsId` in `SoloDevBoard.AppHost.csproj`).

## Start the app

```bash
aspire start --apphost SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
aspire describe
```

Open the `app` resource URL shown by `aspire describe`.
