# Key Vault secret inventory

Reference for operators migrating authentication secrets. Do not commit secret values.

## Application secrets (SoloDevBoard.Web)

| Key Vault secret name | Configuration key | PAT-only local trusted mode | Hosted sign-in | Genuinely secret |
|----------------------|-------------------|----------|----------------|------------------|
| `gh-pat` | `GitHubAuth:PersonalAccessToken` | Required | `-` / omitted | Yes |
| `gh-app-client-secret` | `GitHubAuth:HostedGitHubAppClientSecret` | `-` / omitted | Required | Yes |

## Vault placement

| Vault (illustrative) | Resource group | Consumers |
|---------------------|----------------|-----------|
| `kv-solodevboard-dev` | `rg-solodevboard-dev` | Local Path B (`aspire start` fallback) |
| `auth-secrets-staging` | Operator app RG for staging | Staging Container App (`app`); Functions worker (`functions`) |
| `auth-secrets` | Operator app RG for production | Production Container App (`app`); Functions worker (`functions`) |

Tier vaults must be created in the **same Azure region** as `AZURE_LOCATION` / `Azure__Location` for that GitHub Environment. Recreating a vault after relocation is described in [UK_SOUTH_TIER_MIGRATION.md](UK_SOUTH_TIER_MIGRATION.md).

Aspire logical name remains `auth-secrets`; Azure resource names follow [ADR-0021](../adr/adr-0021-two-tier-cd-pipeline.md) staging suffix rules.

## Out of scope for app Key Vault

- `ROADMAP_PROJECT_TOKEN` (GitHub Actions only)
- Non-secret AppHost parameters (`gh-app-client-id`, allow-lists, feature toggles)
- `DocsCapture:Enabled` (local App user settings only)

## References

- [ADR-0043](../adr/adr-0043-key-vault-backed-hosted-auth-secrets.md) – hosted auth secrets in Key Vault
- [ADR-0045](../adr/adr-0045-persistent-key-vault-dual-path-local-secrets.md) – persistent tier vault and local dual path
- [Azure Key Vault setup](../docs/azure-key-vault-setup.md) – operator migration steps