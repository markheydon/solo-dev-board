# Azure Key Vault setup for SoloDevBoard

> **Audience:** Operators and team maintainers. Solo local developers can skip this document if they use [Path A in Getting Started](getting-started.md#path-a-local-secrets-no-azure).

This guide creates and populates Key Vaults referenced by the AppHost. Secret values are never committed to the repository.

## Prerequisites

- Azure subscription with permission to create Key Vaults and assign RBAC
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (`az login`)

## 1. Development vault (optional Path B)

For maintainers who prefer shared secrets instead of per-machine `aspire secret set`:

```bash
DEV_RG="rg-solodevboard-dev"
DEV_LOCATION="uksouth"
DEV_VAULT="kv-solodevboard-dev"

az group create --name "$DEV_RG" --location "$DEV_LOCATION"

az keyvault create \
  --name "$DEV_VAULT" \
  --resource-group "$DEV_RG" \
  --location "$DEV_LOCATION" \
  --enable-rbac-authorization true \
  --retention-days 90
```

Populate secrets (replace placeholders):

```bash
az keyvault secret set --vault-name "$DEV_VAULT" --name "gh-pat" --value "<dev-pat-or-minus>"
az keyvault secret set --vault-name "$DEV_VAULT" --name "gh-app-client-secret" --value "<dev-client-secret-or-minus>"
```

Grant developers **Key Vault Secrets User** on this vault only:

```bash
DEVELOPER_OBJECT_ID="<entra-user-or-group-object-id>"
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee-object-id "$DEVELOPER_OBJECT_ID" \
  --assignee-principal-type User \
  --scope "$(az keyvault show --name "$DEV_VAULT" --resource-group "$DEV_RG" --query id -o tsv)"
```

Committing org defaults: set `Parameters:auth-secrets-vault-name` and `Parameters:auth-secrets-vault-resource-group` in `src/SoloDevBoard.AppHost/appsettings.Development.json` (names only).

## 2. Staging and production vaults

Use the vault names pinned in AppHost `appsettings.Staging.json` and `appsettings.Production.json` (`auth-secrets-staging` and `auth-secrets` by default) in each tier's app resource group, or adopt vaults already created by an earlier Aspire deploy.

Populate tier-specific `gh-pat` and `gh-app-client-secret` values before running CD without legacy GitHub deploy secrets. Hosted CD tiers typically use `-` for `gh-pat` and a real value for `gh-app-client-secret`.

Container App managed identity receives **Key Vault Secrets User** on deploy via AppHost `WithRoleAssignments`. Operators do not assign that role by hand unless troubleshooting.

## 3. Hardening (recommended)

Production vaults:

```bash
az keyvault update \
  --name "$VAULT_NAME" \
  --resource-group "$APP_RG" \
  --enable-purge-protection true
```

Enable diagnostic settings to Log Analytics per your subscription standards.

## 4. Rotation

1. Add a new secret version in the appropriate vault (`az keyvault secret set` with the same name).
2. Restart the Container App or run `aspire start` locally when using Path B.
3. Remove deploy-time copies from GitHub Environment secrets once migration is complete.

## Related documentation

- [Deploying to Azure](deployment.md)
- [Getting Started — authentication paths](getting-started.md)
- [Secret inventory](../plan/KEY_VAULT_SECRET_INVENTORY.md)