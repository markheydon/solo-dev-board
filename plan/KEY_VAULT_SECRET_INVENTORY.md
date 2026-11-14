# Key Vault secret inventory

Reference for operators migrating authentication secrets. Do not commit secret values.

## Application secrets (SoloDevBoard.App)

| Key Vault secret name | Configuration key | PAT mode | Hosted sign-in | Genuinely secret |
|----------------------|-------------------|----------|----------------|------------------|
| `gh-pat` | `GitHubAuth:PersonalAccessToken` | Required | `-` / omitted | Yes |
| `gh-app-client-secret` | `GitHubAuth:HostedGitHubAppClientSecret` | `-` / omitted | Required | Yes |

## Vault placement

| Vault (illustrative) | Resource group | Consumers |
|---------------------|----------------|-----------|
| `kv-solodevboard-dev` | `rg-solodevboard-dev` | Local Path B (`aspire start` fallback) |
| `auth-secrets-staging` | Operator app RG for staging | Staging Container App |
| `auth-secrets` | Operator app RG for production | Production Container App |

Aspire logical name remains `auth-secrets`; Azure resource names follow [ADR-0021](../adr/adr-0021-two-tier-cd-pipeline.md) staging suffix rules.

## Out of scope for app Key Vault

- `ROADMAP_PROJECT_TOKEN` (GitHub Actions only)
- Non-secret AppHost parameters (`gh-app-client-id`, allow-lists, feature toggles)
- `DocsCapture:Enabled` (local App user settings only)