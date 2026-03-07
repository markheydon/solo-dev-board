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

- **Personal Access Token (PAT):** Simplest option for local development. Create a PAT at [GitHub → Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens). The token requires the following scopes:
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

Located at `src/App/SoloDevBoard.App/appsettings.json`. The relevant section is:

```json
{
  "GitHubAuth": {
    "PersonalAccessToken": "",
    "GitHubAppId": "",
    "GitHubAppPrivateKey": "",
    "UseGitHubApp": false
  }
}
```

Leave `PersonalAccessToken` empty in `appsettings.json` and supply it via an environment variable or user secrets instead.

### Environment Variables

| Variable | Description |
|---|---|
| `GitHubAuth__PersonalAccessToken` | Your GitHub Personal Access Token |
| `GitHubAuth__GitHubAppId` | GitHub App ID (when using GitHub App mode) |
| `GitHubAuth__GitHubAppPrivateKey` | GitHub App private key in PEM format |
| `GitHubAuth__UseGitHubApp` | Set to `true` to use GitHub App authentication |

To set the token for local development using .NET User Secrets:

```bash
dotnet user-secrets set "GitHubAuth:PersonalAccessToken" "<your-token>" --project src/App/SoloDevBoard.App
```

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
