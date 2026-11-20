# Azure Key Vault setup for Solo Dev Board

> **Audience:** Operators and team maintainers. Solo local developers can skip this document if they use [Path A in Getting Started](getting-started.md#path-a-local-secrets-no-azure).

This guide creates and populates Key Vaults referenced by the AppHost. Secret values are never committed to the repository.

Path B uses Key Vault only for the two **secret** parameters (`gh-pat` and `gh-app-client-secret`). Authentication mode, GitHub App client ID, and admission allow-lists stay on the AppHost (see [Local configuration after the vault](#local-configuration-after-the-vault-path-b)). That is why `aspire secret set` alone is not enough for hosted sign-in: you still configure non-secret parameters locally even when secrets live in the vault.

## What goes where

| Material | Key Vault | Local (Path B) |
| --- | --- | --- |
| `gh-pat` | Secret name `gh-pat` | Omit or `-` when the vault supplies PAT |
| `gh-app-client-secret` | Secret name `gh-app-client-secret` | Omit or `-` when the vault supplies the client secret |
| `hosted-sign-in-enabled`, `gh-app-client-id`, allow-lists, `hosted-admission-enabled` | No | `appsettings.Development.json`, dashboard, or `Parameters__*` env vars |

Full parameter reference: [Getting Started — AppHost parameters](getting-started.md#apphost-parameters-aspire). Secret naming: [Key Vault secret inventory](../plan/KEY_VAULT_SECRET_INVENTORY.md).

## Prerequisites

- Azure subscription with permission to create Key Vaults and assign RBAC
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (`az login`)
- For local Path B: Aspire CLI aligned with the AppHost SDK (see [AppHost README](../src/SoloDevBoard.AppHost/README.md))

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

With `--enable-rbac-authorization true`, creating the vault does **not** grant data-plane access. `ForbiddenByRbac` on `secret set` means your identity has no role that includes `setSecret` (read-only **Key Vault Secrets User** is not enough).

### RBAC on the dev vault

| Role | Who | Use |
| --- | --- | --- |
| **Key Vault Secrets Officer** | Operator / maintainer who populates or rotates secrets | `az keyvault secret set`, delete, list |
| **Key Vault Secrets User** | Developers running Path B locally | Read secrets for `aspire start` (`DefaultAzureCredential`) |

Assign **Secrets Officer** on the vault **before** populating secrets. Assign **Secrets User** to developers who only consume secrets (you can hold both roles on the same vault if you operate it and run locally).

You need **Owner** or **User Access Administrator** on the subscription or resource group to create role assignments. Resource group **Contributor** can create the vault but cannot assign these roles by itself.

**Operator (populate and rotate)** — use your Entra user object ID:

```bash
VAULT_SCOPE="$(az keyvault show --name "$DEV_VAULT" --resource-group "$DEV_RG" --query id -o tsv)"
OPERATOR_OBJECT_ID="$(az ad signed-in-user show --query id -o tsv)"

az role assignment create \
  --role "Key Vault Secrets Officer" \
  --assignee-object-id "$OPERATOR_OBJECT_ID" \
  --assignee-principal-type User \
  --scope "$VAULT_SCOPE"
```

After a new assignment, wait one to five minutes for propagation, then run `az keyvault secret set` again.

**Developers (Path B read only)**

```bash
DEVELOPER_OBJECT_ID="<entra-user-or-group-object-id>"
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee-object-id "$DEVELOPER_OBJECT_ID" \
  --assignee-principal-type User \
  --scope "$VAULT_SCOPE"
```

Find a user object ID: `az ad signed-in-user show --query id -o tsv`.

### Populate vault secrets (match your auth mode)

Both secret names should exist in every vault. Set the **inactive** mode's secret to a single hyphen (`-`), matching the AppHost parameter convention.

**PAT-only local trusted mode (default)**

```bash
az keyvault secret set --vault-name "$DEV_VAULT" --name "gh-pat" --value "<your-github-pat>"
az keyvault secret set --vault-name "$DEV_VAULT" --name "gh-app-client-secret" --value "-"
```

**Hosted sign-in (GitHub App)**

```bash
az keyvault secret set --vault-name "$DEV_VAULT" --name "gh-pat" --value "-"
az keyvault secret set --vault-name "$DEV_VAULT" --name "gh-app-client-secret" --value "<github-app-client-secret>"
```

Staging and production CD tiers use hosted sign-in: store a real `gh-app-client-secret` and `-` for `gh-pat` unless you intentionally run PAT on a self-hosted deploy (see [deployment — PAT self-hoster](deployment.md#self-hoster-deployment-pat-only-local-trusted-mode)).

### Aspire local prerequisites (Path B)

Key Vault in the portal does not by itself satisfy Aspire. Before `aspire start` (or `aspire run`), configure **both** of the following.

**1. Azure subscription context for the AppHost**

Aspire’s `AsExisting` Key Vault reference needs subscription metadata in the **same shell** as Aspire:

```bash
export Azure__SubscriptionId="$(az account show --query id -o tsv)"
export Azure__Location="uksouth"   # match the vault region
```

Use the subscription where `kv-solodevboard-dev` lives. Without `Azure__SubscriptionId`, dashboard state shows `auth-secrets` as **Missing subscription configuration** and secret parameters stay **ValueMissing**, which blocks startup.

**2. Placeholder values for secret AppHost parameters**

Aspire still requires every `secret: true` parameter to have a stored value, even when Path B reads the real secret from Key Vault. Set both to a single hyphen (inactive parameter convention):

```bash
aspire secret set Parameters:gh-pat "-"
aspire secret set Parameters:gh-app-client-secret "-"
```

The vault supplies the real `gh-app-client-secret` for hosted sign-in (or `gh-pat` for PAT-only local trusted mode). These placeholders only satisfy Aspire’s parameter resources; they are not a second copy of your GitHub credentials.

To confirm parameters are ready: `aspire describe` should show `gh-pat` and `gh-app-client-secret` as **Running**, not **ValueMissing**.

**3. `az login`**

Same Entra user as portal, with **Key Vault Secrets User** (or Officer) on the dev vault.

### Reading the Aspire dashboard (Path B)

Use `aspire describe` or the dashboard **Resources** list. Judge success by **`web`**, not every Azure row.

| Resource | Healthy local Path B | Notes |
| --- | --- | --- |
| `web` | **Running** + HTTPS URL | Open this URL for SoloDevBoard. |
| `gh-pat` / `gh-app-client-secret` | **Running** (not ValueMissing) | Placeholders `-` in user secrets; real values from Key Vault. |
| `auth-secrets` | May show provisioning briefly | `AsExisting` reference to your dev vault. |
| `azure-environment` | May show **Failed** if a child Azure step failed earlier | Often harmless when `web` is healthy. |

In **`web` console logs**, expect: `GitHub auth: hosted sign-in is active` (hosted) or the PAT-only local trusted mode message. If `web` is **Starting** forever, open logs for `Waiting for value from Aspire.Hosting.Azure.AzureKeyVaultSecretReference` (check `az login` and **Secrets User**).

If parameters are **ValueMissing**, complete steps 1–2 above before chasing Azure resources.

### Local configuration after the vault (Path B)

`src/SoloDevBoard.AppHost/appsettings.Development.json` is gitignored (machine-specific). It is not in the clone by design. Copy the committed example and edit vault names only, or create the file manually.

```bash
cp src/SoloDevBoard.AppHost/appsettings.Development.json.example \
   src/SoloDevBoard.AppHost/appsettings.Development.json
```

**PAT-only local trusted mode — minimal file** (other defaults come from `appsettings.json`):

```json
{
  "Parameters": {
    "auth-secrets-vault-name": "kv-solodevboard-dev",
    "auth-secrets-vault-resource-group": "rg-solodevboard-dev"
  }
}
```

**Hosted sign-in — full non-secret Parameters block** (client secret is **not** here; it comes from the vault):

```json
{
  "Parameters": {
    "hosted-sign-in-enabled": "true",
    "hosted-admission-enabled": "true",
    "gh-app-client-id": "<github-app-client-id>",
    "allowed-user-logins": "<your-login>",
    "allowed-org-logins": "-",
    "auth-secrets-vault-name": "kv-solodevboard-dev",
    "auth-secrets-vault-resource-group": "rg-solodevboard-dev"
  }
}
```

Use a real allow-list: set `allowed-user-logins` and/or `allowed-org-logins` with comma-separated logins; set the unused list to `-`. Do **not** put `gh-app-client-secret` or `gh-pat` in this file when the vault owns them.

Equivalent environment variables (optional instead of the JSON file):

```bash
export Parameters__auth_secrets_vault_name="kv-solodevboard-dev"
export Parameters__auth_secrets_vault_resource_group="rg-solodevboard-dev"
# Hosted only — examples:
export Parameters__hosted_sign_in_enabled="true"
export Parameters__gh_app_client_id="<github-app-client-id>"
export Parameters__allowed_user_logins="<your-login>"
export Parameters__allowed_org_logins="-"
```

### Path B walkthrough (checklist)

1. Create the dev vault and assign **Key Vault Secrets Officer** (populate) and **Secrets User** (developers) as in [RBAC on the dev vault](#rbac-on-the-dev-vault).
2. Populate secrets for your mode ([above](#populate-vault-secrets-match-your-auth-mode)).
3. `az login` (same tenant as the vault).
4. Create `appsettings.Development.json` from the [examples](#local-configuration-after-the-vault-path-b).
5. Set [Aspire placeholders and Azure subscription context](#aspire-local-prerequisites-path-b) (`Parameters:gh-pat` and `Parameters:gh-app-client-secret` to `-`, plus `Azure__SubscriptionId`). Do not put the real GitHub client secret or PAT in `aspire secret set` when the vault owns them; local **non-placeholder** values override Key Vault.
6. For hosted sign-in: create or reuse a GitHub App, register `{app-https-url}/auth/callback` after the first start (`aspire describe`). See [Hosted sign-in mode setup](getting-started.md#hosted-sign-in-mode-setup).
7. Start the app:

```bash
aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
```

8. Confirm `web` logs: PAT-only local trusted mode or hosted sign-in active (see [Startup validation](getting-started.md#startup-validation)).

Quick vault checks before `aspire start` (use the same shell where you created the vault, or re-export names):

```bash
export DEV_RG="rg-solodevboard-dev"
export DEV_VAULT="kv-solodevboard-dev"

# Vault exists in the current subscription?
az keyvault show --name "$DEV_VAULT" --resource-group "$DEV_RG" --query name -o tsv

# Both secret names present? (empty table = nothing was set, or wrong vault / subscription)
az keyvault secret list --vault-name "$DEV_VAULT" --query "[].{Name:name, Updated:attributes.updated}" -o table
```

You should see **two** rows: `gh-pat` and `gh-app-client-secret`. Hosted mode still needs both; inactive `gh-pat` should be stored as `-`.

If a name is missing, run the [hosted](#hosted-sign-in-mode-github-app) or [PAT](#pat-mode-default-local-trusted-mode) `secret set` block again (requires **Key Vault Secrets Officer**).

Confirm read access without printing values:

```bash
az keyvault secret show --vault-name "$DEV_VAULT" --name "gh-pat" --query "attributes.enabled" -o tsv
az keyvault secret show --vault-name "$DEV_VAULT" --name "gh-app-client-secret" --query "attributes.enabled" -o tsv
```

Each command should print `true`. Errors such as `SecretNotFound` or `ForbiddenByRbac` go to stderr. If stdout is empty with exit code 0, run one `show` without `--query` and inspect JSON:

```bash
az keyvault secret show --vault-name "$DEV_VAULT" --name "gh-app-client-secret" -o jsonc
```

### Path B versus `aspire secret set` (Path A)

| Goal | Path A | Path B |
| --- | --- | --- |
| PAT secret | `aspire secret set Parameters:gh-pat "…"` | Vault `gh-pat`; leave local secret unset |
| Hosted client secret | `aspire secret set Parameters:gh-app-client-secret "…"` | Vault `gh-app-client-secret`; leave local secret unset |
| Mode + client ID + allow-lists | Dashboard, JSON, or env | Same (vault does not replace these) |

## 2. Staging and production vaults

Use the vault names pinned in AppHost `appsettings.Staging.json` and `appsettings.Production.json` (`auth-secrets-staging` and `auth-secrets` by default) in each tier's app resource group, or adopt vaults already created by an earlier Aspire deploy.

Populate tier secrets before running CD without legacy GitHub deploy secrets:

```bash
# Typical staging / production (hosted sign-in)
az keyvault secret set --vault-name "$VAULT_NAME" --name "gh-pat" --value "-"
az keyvault secret set --vault-name "$VAULT_NAME" --name "gh-app-client-secret" --value "<tier-client-secret>"
```

Container App managed identity receives **Key Vault Secrets User** on deploy via AppHost `WithRoleAssignments`. Operators do not assign that role by hand unless troubleshooting.

Non-secret hosted parameters for CD come from GitHub Environment variables (`Parameters__*`), not from Key Vault. See [deployment](deployment.md) and [AppHost README — GitHub Actions parameter mapping](../src/SoloDevBoard.AppHost/README.md#github-actions-parameter-mapping).

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
- [Hosted auth Key Vault pattern](../plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md)
- [Post-migration Azure cleanup (archived)](../plan/archive/POST_KEY_VAULT_MIGRATION_CLEANUP.md) — historical operator checklist (complete 2026-11-15)