# Post-migration Azure cleanup (ADR-0045)

**Status:** Complete (2026-11-15). Executed after production CD on release tag `v1.3.0` and operator verification on staging, production, and local Path B. Retained as a historical checklist for subscriptions that still hold legacy Aspire vaults or empty `rg-aspire-*` groups.

Operator checklist for removing leftover resources after **persistent** tier Key Vaults (`auth-secrets-staging`, `auth-secrets`) are proven in CD. Do not run this immediately after merging ADR-0045; wait until a **production** release deploy succeeds.

## When to run

Complete this cleanup only when **all** of the following are true:

1. Staging CD has deployed successfully using `auth-secrets-staging` in the staging app resource group (`rg-solodevboard-stage` or your `AZURE_RESOURCE_GROUP` for the `staging` GitHub Environment).
2. A **production** CD run has completed successfully on a `v*` release tag using `auth-secrets` in the production app resource group.
3. Hosted sign-in works on staging and production (smoke test: sign in, board loads, `/health` OK).

## What the old resources were

Before [ADR-0045](../../adr/adr-0045-persistent-key-vault-dual-path-local-secrets.md), Aspire **provisioned** per-tier Key Vaults with random suffixes and stored `gh-pat` and `gh-app-client-secret` there. CD no longer creates those vaults; deploy uses `PublishAsExisting` on fixed names documented in [Azure Key Vault setup](../../docs/azure-key-vault-setup.md).

| Legacy resource | Typical location | Status after migration |
| --- | --- | --- |
| `authsecretsstaging-sv6q6` (name may vary) | Staging app RG | Superseded by `auth-secrets-staging` |
| `authsecrets-in6hthwvhses` (name may vary) | Production app RG | Superseded by `auth-secrets` |
| `rg-aspire-solodevboardapphost-*` (empty) | Often `uksouth` / `ukwest` | Aspire scaffolding shells, not used by current CD |

GitHub Environment secrets `GH_PAT` and `GH_APP_CLIENT_SECRET` are no longer required for routine CD once tier vaults are populated.

## Verify before delete

```bash
# Correct vaults exist and secrets are readable (enabled=true)
VAULT_NAME="auth-secrets-staging"   # staging
az keyvault secret show --vault-name "$VAULT_NAME" --name "gh-pat" --query attributes.enabled -o tsv
az keyvault secret show --vault-name "$VAULT_NAME" --name "gh-app-client-secret" --query attributes.enabled -o tsv

VAULT_NAME="auth-secrets"           # production
az keyvault secret show --vault-name "$VAULT_NAME" --name "gh-pat" --query attributes.enabled -o tsv
az keyvault secret show --vault-name "$VAULT_NAME" --name "gh-app-client-secret" --query attributes.enabled -o tsv
```

Confirm the Container App still references the **new** vault (Azure portal → Container App → environment variables / Key Vault references), not the legacy vault name.

List legacy vaults in the tier RGs (adjust RG names if yours differ):

```bash
az keyvault list -g rg-solodevboard-stage --query "[].name" -o tsv
az keyvault list -g rg-solodevboard-prod --query "[].name" -o tsv
```

Empty Aspire-managed resource groups:

```bash
az group list --query "[?tags.aspire=='true'].{name:name}" -o table
az resource list -g <aspire-rg-name> --query "length(@)"
```

Only proceed when `length` is `0` for each aspire-tagged group you plan to delete.

## Safe to remove (after production OK)

**Legacy Key Vaults** (only the old random-suffix names, not `auth-secrets` / `auth-secrets-staging`):

```bash
# Example names — confirm with `az keyvault list` first
az keyvault delete --name authsecretsstaging-sv6q6
az keyvault purge --name authsecretsstaging-sv6q6   # optional, after soft-delete retention

az keyvault delete --name authsecrets-in6hthwvhses
az keyvault purge --name authsecrets-in6hthwvhses
```

**Empty `rg-aspire-solodevboardapphost-*` resource groups** (no resources):

```bash
az group delete --name rg-aspire-solodevboardapphost-252c1565 --yes --no-wait
az group delete --name rg-aspire-solodevboardapphost-647adb88 --yes --no-wait
```

Replace names with those returned by `az group list` in your subscription.

## Do not remove

| Resource | Reason |
| --- | --- |
| `auth-secrets-staging`, `auth-secrets` | Active tier vaults ([KEY_VAULT_SECRET_INVENTORY.md](../KEY_VAULT_SECRET_INVENTORY.md)) |
| `rg-solodevboard-stage`, `rg-solodevboard-prod` | Tier app resource groups |
| `rg-solodevboard-dev`, `kv-solodevboard-dev` | Optional local Path B dev vault |
| `rg-platform-shared` (if used) | Shared ACR when `ACR_NAME` / `ACR_RESOURCE_GROUP` are set |
| Container Apps, ACA environment, Application Insights, shared ACR | Live deployment |

## References

- [docs/deployment.md](../../docs/deployment.md) — CD tiers and Key Vault migration
- [docs/azure-key-vault-setup.md](../../docs/azure-key-vault-setup.md) — populate and RBAC
- [ADR-0045](../../adr/adr-0045-persistent-key-vault-dual-path-local-secrets.md)