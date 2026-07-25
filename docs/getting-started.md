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
| **PAT-only local trusted mode** (default) | Solo local development and trusted personal self-hosting | `gh-pat` only (your GitHub login is resolved automatically) |
| **Hosted sign-in** | Multi-user / public production deployments and local multi-tenant testing | `hosted-sign-in-enabled`, GitHub App OAuth credentials, and allow-lists |

Parameters for the mode you are **not** using can be left unset (or set to `-` when Aspire requires a value).

#### How the modes differ

| | PAT-only local trusted mode | Hosted sign-in |
|---|---|---|
| **Who authenticates** | Nobody signs in to SoloDevBoard — the app uses one configured token for all GitHub API calls | Each visitor signs in with GitHub at `/auth/sign-in` (landing at `/welcome`) |
| **Identity** | Your login is resolved automatically from the PAT at startup | Per-user session claims (login, access token, installation, organisations) |
| **Admission control** | Not applicable — anyone who can reach the app acts as the PAT owner | Operator allow-lists (`allowed-user-logins` / `allowed-org-logins`); deny-by-default |
| **GitHub App required?** | No | Yes (OAuth App fallback exists but is disabled by default) |
| **Trust boundary** | Suitable only where you trust every person who can reach the deployment (localhost, private network, or a personal Azure instance you alone use) | Suitable for shared or public endpoints |
| **Connectivity UX** | App-bar **Connected as @login** chip; recovery at `/auth/connectivity-error` — see [PAT Connectivity](user-guide/pat-connectivity.md) | Session expiry and re-sign-in — see [Hosted Authentication](user-guide/hosted-authentication.md) |

> **Security note:** PAT-only mode does **not** provide multi-user isolation. Do not expose a PAT-mode instance on a public URL that others can reach. For shared or public hosting, use hosted sign-in with admission control.

#### PAT-only local trusted mode

This is the **default** authentication path. Set `hosted-sign-in-enabled` to `false` (the shipped default) and supply a personal access token via the AppHost parameter `gh-pat`. No GitHub App, OAuth client, or allow-list is required.

Create a PAT at [GitHub → Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens) with these scopes:

- `repo` (full control of private repositories)
- `read:org` (read-only access to organisation data, if applicable)
- `workflow` (to manage GitHub Actions workflows)
- `read:project` (read-only access to GitHub Projects; required for the Triage UI project board feature)

**Quick local setup (Aspire):**

```bash
aspire secret set Parameters:gh-pat "<your-token>"
aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
```

Your GitHub login is resolved automatically from the PAT. Full parameter tables and mode-switching steps are under [Configuration](#configuration) below. For runtime connectivity status and recovery, see [PAT Connectivity](user-guide/pat-connectivity.md). To run a **personal hosted instance** on your own Azure subscription with the same PAT mode, see [Self-hoster deployment (PAT mode)](deployment.md#self-hoster-deployment-pat-mode).

#### Hosted sign-in mode

Uses a GitHub App for OAuth sign-in at `/auth/sign-in`, with operator-managed allow-lists for users and organisations. Recommended for production and multi-tenant deployments. See [Hosted Authentication Guide](user-guide/hosted-authentication.md) for the full operator and local testing walkthrough.

#### OAuth App fallback

OAuth App fallback is supported but disabled by default. It is only used if enabled and the primary GitHub App authentication path is unavailable. It does not replace PAT-only local trusted mode.

## Running Locally

SoloDevBoard uses **Aspire for local orchestration**, which provides standardised behaviour across local machines, dev containers, and Codespaces. Aspire is the **recommended path** for all new development.

1. **Clone the repository:**

   ```bash
   git clone https://github.com/<your-username>/solo-dev-board.git
   cd solo-dev-board
   ```

2. **Restore dependencies:**

   ```bash
   dotnet restore SoloDevBoard.slnx
   ```

3. **Configure GitHub authentication** for your chosen mode (see [Authentication](#github-authentication) above). You can set values **before** starting Aspire with `aspire secret set` and `appsettings.json`, or via the **Parameters** tab in the Aspire dashboard on first run.

4. **Start the application with Aspire (recommended):**

   ```bash
   aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
   ```

5. **Get the allocated endpoint from Aspire:**

   ```bash
   aspire describe
   ```

6. Open the `app` resource URL shown by `aspire describe`.

7. **For a worktree or Codespaces session, use isolation to avoid port and state clashes:**

   ```bash
   aspire start --isolated --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
   ```

### Legacy run path (without Aspire orchestration)

If you prefer to run directly without Aspire:

```bash
# PAT mode
dotnet user-secrets set "GitHubAuth:PersonalAccessToken" "<your-pat>" --project src/App/SoloDevBoard.App
dotnet run --project src/App/SoloDevBoard.App

# Or hosted sign-in mode (requires GitHub App configuration)
```

> **Recommendation:** Aspire is the standardised path for local development. It ensures consistent behaviour across all environments and prepares you for production deployment to Azure Container Apps.

---

## Configuration

SoloDevBoard is configured via `appsettings.json` and environment variables. **Never commit secrets to source control.**

When running via Aspire (`aspire start`), GitHub auth and admission settings are modelled as **AppHost parameters** and injected into the `app` resource as environment variables. See also [`src/SoloDevBoard.AppHost/README.md`](../src/SoloDevBoard.AppHost/README.md) for a concise parameter cheat sheet.

### Choose your authentication mode

**PAT-only local trusted mode (default)** — leave `hosted-sign-in-enabled` as `false`. Set `gh-pat` to your token. Set all hosted-sign-in parameters to `-`. This is the path for local development and trusted personal self-hosting; it does not depend on hosted sign-in infrastructure.

**Hosted sign-in mode** — set `hosted-sign-in-enabled` to `true`, configure GitHub App OAuth credentials and allow-lists, and set `gh-pat` to `-`.

Values saved from the Aspire dashboard **Parameters** tab are stored in AppHost user secrets and persist across restarts.

Use `-` on inactive parameters. Shipped defaults are in `src/SoloDevBoard.AppHost/appsettings.json`; values saved from the Aspire dashboard override those defaults via user secrets.

### Switching between PAT and hosted sign-in

Use the Aspire dashboard **Parameters** tab (or `aspire secret set` for secrets). Restart Aspire after changing mode.

**Switch to PAT mode**

| Parameter | Action |
|---|---|
| `hosted-sign-in-enabled` | `false` |
| `gh-pat` | your PAT |
| `gh-app-client-id` | `-` |
| `gh-app-client-secret` | `-` (set in dashboard) |
| `allowed-user-logins` | `-` |
| `allowed-org-logins` | `-` |

**Switch to hosted sign-in**

| Parameter | Action |
|---|---|
| `hosted-sign-in-enabled` | `true` |
| `gh-pat` | `-` |
| `gh-app-client-id` | your Client ID |
| `gh-app-client-secret` | your client secret |
| `allowed-user-logins` | your login(s), or `-` if using org list only |
| `allowed-org-logins` | org login(s), or `-` if using user list only |

Also update your GitHub App callback URL to `{app-https-url}/auth/callback` after restarting Aspire.

### Startup validation

On startup, SoloDevBoard validates configuration for the **active mode** and fails fast if required settings for that mode are missing. Check the `app` resource logs in the Aspire dashboard or your IDE output.

| Active mode | Required settings |
|---|---|
| PAT | `gh-pat` |
| Hosted | `gh-app-client-id`, `gh-app-client-secret`, and at least one allow-list with real logins when admission control is enabled |

Setting inactive-mode parameters to `-` is still recommended when switching modes, but startup validation does not enforce it.

Example log on success:

- `GitHub auth: PAT mode is active. Owner login will be resolved from the personal access token when needed.`
- `GitHub auth: hosted sign-in mode is active. Admission control is enabled.`

On first `aspire start`, open the Aspire dashboard and go to **Resources → Parameters**. With the default PAT mode, you only need to set:

1. `gh-pat` — your GitHub personal access token (secret)

Your GitHub login is resolved automatically from the PAT when the app starts. Parameters you do not need for your chosen mode can be left unset.

### AppHost parameters (Aspire)

| AppHost parameter | Secret | Default | App config key | PAT mode | Hosted sign-in |
|---|---|---|---|---|---|
| `hosted-sign-in-enabled` | no | `false` | `GitHubAuth:HostedSignInEnabled` | leave `false` | set `true` |
| `gh-pat` | yes | *(none)* | `GitHubAuth:PersonalAccessToken` | **your PAT** | `-` (set in dashboard) |
| `gh-app-client-id` | no | `-` in appsettings | `GitHubAuth:HostedGitHubAppClientId` | `-` | **client ID** |
| `gh-app-client-secret` | yes | *(none)* | `GitHubAuth:HostedGitHubAppClientSecret` | `-` (set in dashboard) | **client secret** |
| `hosted-admission-enabled` | no | `true` | `HostedAdmissionControl:Enabled` | ignored | `true` (recommended) |
| `allowed-user-logins` | no | `-` | `HostedAdmissionControl:AllowedUserLogins` | ignored | **logins or `-`** |
| `allowed-org-logins` | no | `-` | `HostedAdmissionControl:AllowedOrganisationLogins` | ignored | **logins or `-`** |

#### What each parameter does

**`hosted-sign-in-enabled`** — Selects the authentication mode. When `false` (the default), SoloDevBoard runs in PAT mode: a single configured token is used for all GitHub API calls and your login is resolved automatically from that token. When `true`, SoloDevBoard uses GitHub App OAuth sign-in at `/auth/sign-in`; each user authenticates with their own GitHub identity and session.

**`gh-pat`** — Your GitHub personal access token for PAT mode. Set to `-` for hosted sign-in. SoloDevBoard uses this for every GitHub API request on your behalf in PAT mode. Required scopes: `repo`, `read:org`, `workflow`, and `read:project`. Your login is resolved automatically from the token.

**`gh-app-client-id`** — The OAuth Client ID from your GitHub App settings. Set to `-` for PAT mode. Required for hosted sign-in.

**`gh-app-client-secret`** — The OAuth Client secret from your GitHub App settings. Set to `-` for PAT mode. Required for hosted sign-in.

**`hosted-admission-enabled`** — Controls whether hosted sign-in enforces an allow-list. When `true` (the default), only GitHub users or organisations listed in the allow-list parameters can use the app after signing in; everyone else receives a 403. Only applies when hosted sign-in is enabled. Ignored in PAT mode.

**`allowed-user-logins`** — Comma-separated GitHub user logins permitted after hosted sign-in (for example, `markheydon`). When admission control is enabled, set this **or** `allowed-org-logins` with real logins. Use `-` on the parameter you are not using — Aspire requires a value for every parameter, and `-` means "this list is not in use". Unset or `-` on both lists means nobody is admitted. Ignored in PAT mode.

**`allowed-org-logins`** — Comma-separated GitHub organisation logins whose members are permitted after hosted sign-in (for example, `my-org`). Use `-` when you are only using `allowed-user-logins`. A user is admitted if their login or any organisation membership matches an active entry on either list. Ignored in PAT mode.

**Auto-derived (not an AppHost parameter):** `GitHubAuth:HostedSignInCallbackBaseUri` — Set automatically from the app's Aspire HTTPS endpoint. Your GitHub App callback URL must be `{this-url}/auth/callback`. Run `aspire describe` to find the current value after each start if the port changes.

`GitHubAuth:OwnerLogin` can still be set explicitly on the legacy `dotnet run` path to override the login resolved from a PAT. When omitted in PAT mode, it is derived automatically from the token.

Non-secret defaults are in `src/SoloDevBoard.AppHost/appsettings.json`. Secret values are stored via `aspire secret set` or the AppHost user secrets store (`UserSecretsId` on `src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj`).

#### PAT-only local trusted mode setup

```bash
aspire secret set Parameters:gh-pat "<your-token>"
aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
```

Your GitHub login is resolved automatically from the PAT at startup. You can also set the token via the Aspire dashboard **Parameters** tab on first run. After the app starts, confirm the shell shows **Connected as @login** — see [PAT Connectivity](user-guide/pat-connectivity.md).

#### Hosted sign-in mode setup

1. **Create or reuse a GitHub App** at [GitHub → Settings → Developer settings → GitHub Apps](https://github.com/settings/apps). Note the **Client ID** and generate a **Client secret**.
2. **Start Aspire once** to allocate an HTTPS endpoint, then run `aspire describe` and note the `app` resource HTTPS URL.
3. **Register the callback URL** on your GitHub App: `{https-endpoint}/auth/callback` (for example, `https://localhost:17123/auth/callback`). SoloDevBoard sets `GitHubAuth:HostedSignInCallbackBaseUri` from the Aspire endpoint automatically.
4. **Install the GitHub App** on the users or organisations you want to test with.
5. **Configure AppHost parameters:**

```bash
aspire secret set Parameters:gh-app-client-secret "<client-secret>"
```

Set non-secret values via the dashboard, `appsettings.json`, or user secrets:

```json
"Parameters": {
  "hosted-sign-in-enabled": "true",
  "gh-app-client-id": "<client-id>",
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
      "AllowedUserLogins": "",
      "AllowedOrganisationLogins": "",
      "HostedOrganisationLoginsClaimType": "solo-dev-board.github.organisation-logins"
   },
   "GitHub": {
      "Cache": {
         "RepositoriesTtlSeconds": 60,
         "LabelsTtlSeconds": 300,
         "MilestonesTtlSeconds": 300
      }
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
- `HostedAdmissionControl:AllowedUserLogins`: Comma-separated permitted GitHub user logins for hosted access.
- `HostedAdmissionControl:AllowedOrganisationLogins`: Comma-separated permitted GitHub organisation logins for hosted access.
- `HostedAdmissionControl:HostedOrganisationLoginsClaimType`: Claim type used to extract organisation logins from authentication claims.
- `GitHub:Cache:RepositoriesTtlSeconds`: Absolute cache lifetime in seconds for repository catalogue responses (default `60`).
- `GitHub:Cache:LabelsTtlSeconds`: Absolute cache lifetime in seconds for label catalogue responses (default `300`).
- `GitHub:Cache:MilestonesTtlSeconds`: Absolute cache lifetime in seconds for milestone catalogue responses (default `300`).

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
| `GitHub__Cache__RepositoriesTtlSeconds` | Cache lifetime in seconds for repository catalogue responses |
| `GitHub__Cache__LabelsTtlSeconds` | Cache lifetime in seconds for label catalogue responses |
| `GitHub__Cache__MilestonesTtlSeconds` | Cache lifetime in seconds for milestone catalogue responses |

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
- PAT-only local trusted mode is always available for local development and trusted self-hosted use, independent of hosted admission control or fallback settings. See [PAT-only local trusted mode](#pat-only-local-trusted-mode) above and [Self-hoster deployment (PAT mode)](deployment.md#self-hoster-deployment-pat-mode) for Azure.

### Production secrets (GitHub Actions)

In production, secret AppHost parameters are supplied from the GitHub `production` environment during `aspire deploy`. See [Deployment](deployment.md) for the full secret and variable mapping.

---

## Deploying to Azure

SoloDevBoard deploys to Azure Container Apps via Aspire. See the [Deployment guide](deployment.md) for prerequisites, one-time OIDC bootstrap, GitHub Environment configuration, and first deploy steps. For a **personal instance with a PAT** (no GitHub App), start with [Self-hoster deployment (PAT mode)](deployment.md#self-hoster-deployment-pat-mode). For logs, metrics, and traces after deployment, see the [Observability guide](user-guide/observability.md).

Summary:

1. Install the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) and log in (`az login`).
2. Create a resource group (and GitHub Actions OIDC identity if you want CI/CD — Azure CLI commands in [deployment.md](deployment.md)).
3. Configure authentication for your chosen mode: PAT self-hoster secrets/vars, or hosted sign-in GitHub App credentials.
4. Deploy with local `aspire deploy` or run the CD workflow via **Actions → CD - Deploy to Azure → Run workflow**.
5. After a successful hosted-sign-in deploy, update your GitHub App callback URL to the deployed Container App FQDN.
