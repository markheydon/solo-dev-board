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
| A GitHub account | — | Required for GitHub API access |
| A GitHub Personal Access Token (PAT) **or** GitHub App | — | Required for API authentication (see below) |

### GitHub Authentication

SoloDevBoard supports two authentication modes:

- **Personal Access Token (PAT):** Simplest option for local development. Create a PAT at [GitHub → Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens). The token requires the following scopes.
  - `repo` (full control of private repositories)
  - `read:org` (read-only access to organisation data, if applicable)
  - `workflow` (to manage GitHub Actions workflows)

- **GitHub App:** Recommended for production deployments. Provides fine-grained permissions and does not expire. See the [GitHub Apps documentation](https://docs.github.com/en/apps) for setup instructions.

---

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

3. **Configure your GitHub token** (see [Configuration](#configuration) below).

4. **Run the application:**

   ```bash
   dotnet run --project src/App/SoloDevBoard.App
   ```

5. Open your browser and navigate to `https://localhost:5001`.

---

## Configuration

SoloDevBoard is configured via `appsettings.json` and environment variables. **Never commit secrets to source control.**

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

To set the token for local development using .NET User Secrets:

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
