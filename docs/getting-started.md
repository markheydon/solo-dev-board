---
layout: page
title: Getting Started
nav_order: 2
---

This guide walks you through the prerequisites and steps required to run SoloDevBoard locally and deploy it to Azure.

---

## Prerequisites

Before you begin, ensure you have the following installed:

| Prerequisite | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 or later | Required to build and run the application |
| Git | Any recent version | Required to clone the repository |
| Aspire CLI | Latest | Required for local orchestration via AppHost |
| A GitHub account | — | Required for GitHub API access |
| A GitHub Personal Access Token (PAT) **or** GitHub App | — | Required for API authentication (see below) |

### GitHub Authentication

SoloDevBoard supports two **mutually exclusive** authentication modes. Choose one before configuring AppHost parameters:

| Mode | When to use | What you configure |
|---|---|---|
| **PAT mode** (default) | Solo local development and trusted self-hosted use | `github-pat` only (your GitHub login is resolved automatically) |
| **Hosted sign-in** | Production deployments and local multi-tenant testing | `hosted-sign-in-enabled`, GitHub App OAuth credentials, and allow-lists |

Parameters for the mode you are **not** using keep their default placeholder (`__disabled__`) and can be ignored.

#### PAT mode

Create a PAT at [GitHub → Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens) with these scopes:

- `repo` (full control of private repositories)
- `read:org` (read-only access to organisation data, if applicable)
- `workflow` (to manage GitHub Actions workflows)
- `read:project` (read-only access to GitHub Projects; required for the Triage UI project board feature)

#### Hosted sign-in mode

Uses a GitHub App for OAuth sign-in at `/auth/sign-in`, with operator-managed allow-lists for users and organisations. Recommended for production and multi-tenant deployments. See [Hosted Authentication Guide](user-guide/hosted-authentication.md) for the full operator and local testing walkthrough.

#### OAuth App fallback

OAuth App fallback is supported but disabled by default. It is only used if enabled and the primary GitHub App authentication path is unavailable.

## Running Locally

1. **Clone the repository:**

   ```bash
   git clone https://github.com/<your-username>/solo-dev-board.git
   cd solo-dev-board
   ```

2. **Restore dependencies:**

   ```bash
   dotnet restore SoloDevBoard.slnx
   ```

3. **Configure GitHub authentication** for your chosen mode (see [Configuration](#configuration) below). You can set values **before** starting Aspire with `aspire secret set` and `appsettings.json`, or via the **Parameters** tab in the Aspire dashboard on first run.

4. **Start the application with Aspire (recommended):**

   ```bash
   aspire start --apphost SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
   ```

5. **Get the allocated endpoint from Aspire:**

   ```bash
   aspire describe
   ```

6. Open the `app` resource URL shown by `aspire describe`.

7. **Optional legacy run path (without Aspire orchestration):**

   ```bash
   dotnet run --project src/App/SoloDevBoard.App
   ```

8. For a worktree or Codespaces session, use isolation to avoid port and state clashes:

   ```bash
   aspire start --isolated --apphost SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
   ```

This Aspire setup currently exists to standardise local development across local machines, dev containers, and Codespaces. It does not change the production hosting path, and issue #171 remains open for future evaluation of broader Aspire adoption such as additional worker or service processes.

---

## Configuration

SoloDevBoard is configured via `appsettings.json` and environment variables. **Never commit secrets to source control.**

When running via Aspire (`aspire start`), GitHub auth and admission settings are modelled as **AppHost parameters** and injected into the `app` resource as environment variables. See also [`SoloDevBoard.AppHost/README.md`](../SoloDevBoard.AppHost/README.md) for a concise parameter cheat sheet.

### Choose your authentication mode

**PAT mode (default local development)** — leave `hosted-sign-in-enabled` as `false`. Set only `github-pat`. Your GitHub login is resolved automatically from the token at startup. Leave all other auth parameters at `__disabled__`.

**Hosted sign-in mode** — set `hosted-sign-in-enabled` to `true`, configure GitHub App OAuth credentials, and replace the allow-list placeholders with real logins. Leave `github-pat` at `__disabled__`.

### Aspire dashboard (first run)

On first `aspire start`, open the Aspire dashboard and go to **Resources → Parameters**. With the default PAT mode, you only need to set:

1. `github-pat` — your GitHub personal access token (secret)

Your GitHub login is resolved automatically from the PAT when the app starts. All other parameters can remain at `__disabled__`.

### AppHost parameters (Aspire)

| AppHost parameter | Secret | Default | App config key | Set in PAT mode | Set in hosted sign-in |
|---|---|---|---|---|---|
| `hosted-sign-in-enabled` | no | `false` | `GitHubAuth:HostedSignInEnabled` | leave `false` | set `true` |
| `github-pat` | yes | `__disabled__` | `GitHubAuth:PersonalAccessToken` | **your PAT** | leave default |
| `github-app-client-id` | no | `__disabled__` | `GitHubAuth:HostedGitHubAppClientId` | leave default | **client ID** |
| `github-app-client-secret` | yes | `__disabled__` | `GitHubAuth:HostedGitHubAppClientSecret` | leave default | **client secret** |
| `hosted-admission-enabled` | no | `true` | `HostedAdmissionControl:Enabled` | ignored | `true` (recommended) |
| `allowed-user-logins` | no | `__disabled__` | `HostedAdmissionControl:AllowedUserLogins` | ignored | **allow-list** |
| `allowed-org-logins` | no | `__disabled__` | `HostedAdmissionControl:AllowedOrganisationLogins` | ignored | optional |

`GitHubAuth:OwnerLogin` can still be set explicitly to override the login resolved from a PAT (for example on the legacy `dotnet run` path). When omitted, it is derived automatically from the token.

The hosted sign-in callback base URI is derived automatically from the app's Aspire HTTPS endpoint (`GitHubAuth:HostedSignInCallbackBaseUri`).

Non-secret defaults are in `SoloDevBoard.AppHost/appsettings.json`. Secret values are stored via `aspire secret set` or the AppHost user secrets store (`UserSecretsId` on `SoloDevBoard.AppHost.csproj`).

#### PAT mode setup

```bash
aspire secret set Parameters:github-pat "<your-token>"
aspire start --apphost SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
```

Your GitHub login is resolved automatically from the PAT at startup. You can also set the token via the Aspire dashboard **Parameters** tab on first run.

#### Hosted sign-in mode setup

1. **Create or reuse a GitHub App** at [GitHub → Settings → Developer settings → GitHub Apps](https://github.com/settings/apps). Note the **Client ID** and generate a **Client secret**.
2. **Start Aspire once** to allocate an HTTPS endpoint, then run `aspire describe` and note the `app` resource HTTPS URL.
3. **Register the callback URL** on your GitHub App: `{https-endpoint}/auth/callback` (for example, `https://localhost:17123/auth/callback`). SoloDevBoard sets `GitHubAuth:HostedSignInCallbackBaseUri` from the Aspire endpoint automatically.
4. **Install the GitHub App** on the users or organisations you want to test with.
5. **Configure AppHost parameters:**

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

6. **Restart Aspire** and open `/auth/sign-in` on the `app` URL.

See [Hosted Authentication Guide](user-guide/hosted-authentication.md) for operator expectations, admission control, and production deployment notes.

### `appsettings.json`

Located at `src/App/SoloDevBoard.App/appsettings.json`. The relevant sections are:

```json
{
   "GitHubAuth": {
      "OwnerLogin": "",
      "PersonalAccessToken": "",
      "GitHubAppId": "",
      "GitHubAppPrivateKey": "",
      "HostedSignInEnabled": false,
      "HostedOwnerLoginClaimType": "solo-dev-board.github.owner-login",
      "HostedAccessTokenClaimType": "solo-dev-board.github.access-token",
      "HostedInstallationIdClaimType": "solo-dev-board.github.installation-id",
      "HostedTokenExpiresAtClaimType": "solo-dev-board.github.token-expires-at",
      "HostedOAuthAppFallbackEnabled": false,
      "HostedGitHubAppClientId": "",
      "HostedGitHubAppClientSecret": "",
      "HostedSignInCallbackPath": "/auth/callback",
      "HostedGitHubAuthoriseEndpoint": "https://github.com/login/oauth/authorize",
      "HostedGitHubAccessTokenEndpoint": "https://github.com/login/oauth/access_token",
      "HostedSignInScopes": "read:user read:org"
   },
   "HostedAdmissionControl": {
      "Enabled": true,
      "AllowedUserLogins": [],
      "AllowedOrganisationLogins": [],
      "HostedOrganisationLoginsClaimType": "solo-dev-board.github.organisation-logins"
   }
}
```

**Key settings:**
- `HostedSignInEnabled`: Enables hosted sign-in and the per-request authentication boundary.
- `HostedOwnerLoginClaimType`: Claim type used to map the authenticated GitHub owner login.
- `HostedAccessTokenClaimType`: Claim type used to map the hosted GitHub access token.
- `HostedInstallationIdClaimType`: Claim type used to map the hosted GitHub installation identifier.
- `HostedTokenExpiresAtClaimType`: Claim type used to map hosted token expiry (UTC) for fail-fast token validation.
- `HostedOAuthAppFallbackEnabled`: Enables the OAuth App fallback compatibility boundary for hosted mode (disabled by default; only use if GitHub App auth is unavailable).
- `HostedGitHubAppClientId`: GitHub App client identifier used for hosted sign-in.
- `HostedGitHubAppClientSecret`: GitHub App client secret used for hosted sign-in.
- `HostedSignInCallbackPath`: Callback route used by the hosted sign-in handshake.
- `HostedGitHubAuthoriseEndpoint`: Authorisation endpoint used to start hosted sign-in.
- `HostedGitHubAccessTokenEndpoint`: Access-token endpoint used for hosted callback exchange.
- `HostedSignInScopes`: Space-separated scopes requested during hosted sign-in.
- `HostedAdmissionControl:Enabled`: Enables hosted admission control (deny-by-default; only allow users and organisations in allow-lists).
- `HostedAdmissionControl:AllowedUserLogins`: List of permitted GitHub user logins for hosted access.
- `HostedAdmissionControl:AllowedOrganisationLogins`: List of permitted GitHub organisation logins for hosted access.
- `HostedAdmissionControl:HostedOrganisationLoginsClaimType`: Claim type used to extract organisation logins from authentication claims.

Leave `PersonalAccessToken` empty in `appsettings.json` and supply it via an environment variable or user secrets instead.

### Environment Variables

| Variable | Description |
|---|---|
| `GitHubAuth__PersonalAccessToken` | Your GitHub Personal Access Token (for local trusted mode) |
| `GitHubAuth__GitHubAppId` | GitHub App ID (for hosted or local GitHub App mode) |
| `GitHubAuth__GitHubAppPrivateKey` | GitHub App private key in PEM format |
| `GitHubAuth__HostedSignInEnabled` | Set to `true` to enable hosted sign-in and per-request authentication |
| `GitHubAuth__HostedOwnerLoginClaimType` | Claim type for hosted owner login |
| `GitHubAuth__HostedAccessTokenClaimType` | Claim type for hosted access token |
| `GitHubAuth__HostedInstallationIdClaimType` | Claim type for hosted installation identifier |
| `GitHubAuth__HostedTokenExpiresAtClaimType` | Claim type for hosted token expiry (UTC) |
| `GitHubAuth__HostedOAuthAppFallbackEnabled` | Set to `true` to enable OAuth App fallback (disabled by default) |
| `GitHubAuth__HostedGitHubAppClientId` | GitHub App client identifier for hosted sign-in |
| `GitHubAuth__HostedGitHubAppClientSecret` | GitHub App client secret for hosted sign-in |
| `GitHubAuth__HostedSignInCallbackPath` | Callback path for hosted sign-in |
| `GitHubAuth__HostedGitHubAuthoriseEndpoint` | Authorisation endpoint used for hosted sign-in |
| `GitHubAuth__HostedGitHubAccessTokenEndpoint` | Access-token endpoint used for hosted sign-in |
| `GitHubAuth__HostedSignInScopes` | Space-separated scopes requested during hosted sign-in |
| `HostedAdmissionControl__Enabled` | Set to `true` to enable hosted admission control (deny-by-default) |
| `HostedAdmissionControl__AllowedUserLogins` | Comma-separated list of allowed GitHub user logins |
| `HostedAdmissionControl__AllowedOrganisationLogins` | Comma-separated list of allowed GitHub organisation logins |
| `HostedAdmissionControl__HostedOrganisationLoginsClaimType` | Claim type for organisation logins (string) |

To set PAT-only values for the **legacy `dotnet run` path** (without Aspire), use .NET User Secrets on the app project. Only the PAT is required; owner login is resolved automatically:

```bash
dotnet user-secrets set "GitHubAuth:PersonalAccessToken" "<your-token>" --project src/App/SoloDevBoard.App
```


### Hosted Admission Control and Fallback Behaviour

- Hosted sign-in mode requires `HostedGitHubAppClientId` and `HostedGitHubAppClientSecret` so the `/auth/sign-in` and `/auth/callback` handshake can establish a hosted session.
- When `HostedAdmissionControl:Enabled` is true, hosted deployments deny all access by default unless the authenticated user's login or organisation is explicitly listed in the allow-lists.
- All denied admission attempts are logged for operator review.
- The claim type for organisation logins can be set using `HostedOrganisationLoginsClaimType` to match your identity provider's claim mapping.
- OAuth App fallback is only used if `HostedOAuthAppFallbackEnabled` is true and the primary GitHub App authentication path is unavailable. This fallback is disabled by default for security.
- PAT-only local trusted mode is always available for local development and trusted self-hosted use, independent of hosted admission control or fallback settings.

### Azure Key Vault (Production)

In production, secrets are stored in Azure Key Vault. The application is configured to read secrets from Key Vault automatically when deployed to Azure App Service. See the [infrastructure README](../infra/README.md) for details.

---

## Deploying to Azure

SoloDevBoard includes Bicep templates for deploying to Azure App Service. See the [infra/ README](../infra/README.md) for full deployment instructions.

A high-level summary:

1. Ensure you have the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed and are logged in (`az login`).
2. Create a resource group:
   ```bash
   az group create --name rg-solodevboard-prod --location uksouth
   ```
3. Deploy the Bicep template:
   ```bash
   az deployment group create \
     --resource-group rg-solodevboard-prod \
     --template-file infra/main.bicep \
     --parameters environmentName=prod
   ```
4. Configure the GitHub token in Key Vault (see `infra/README.md`).
5. The CD pipeline (`.github/workflows/cd.yml`) handles subsequent deployments on push to `main`.
