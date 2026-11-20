# UK South tier relocation (operator runbook)

Historical operator checklist for moving **staging** and **production** app resource groups from **UK West** to **UK South** while keeping tier Key Vault names (`auth-secrets-staging`, `auth-secrets`). Development Path B (`rg-solodevboard-dev` / `kv-solodevboard-dev`) stays unchanged. Shared ACR (`rg-platform-shared` / `acrsolodevboard`, UK West) stays unchanged.

Azure cannot change a resource group or Key Vault region in place. Relocation is **delete → recreate** with secret backup and **re-grant** of CD identity RBAC on each new app resource group.

See [deployment.md — UK South tier relocation](../docs/deployment.md#uk-south-tier-relocation) for a summary and [Permissions after app RG recreate](../docs/deployment.md#permissions-after-app-rg-recreate).

## Preconditions

- `AZURE_LOCATION=uksouth` on both GitHub Environments (`staging`, `production`).
- Backup `gh-pat` and `gh-app-client-secret` from each tier vault offline.
- Communicate downtime for custom domains (`staging.solodevboard.app`, `my.solodevboard.app`).

## Phase 1 — Staging

1. `az group delete -n rg-solodevboard-stage --yes --no-wait` and wait until the group is gone.
2. If `auth-secrets-staging` is soft-deleted: `az keyvault purge --name auth-secrets-staging --location <former-vault-region>`.
3. `az group create -n rg-solodevboard-stage --location uksouth`.
4. Create vault with RBAC: `az keyvault create -n auth-secrets-staging -g rg-solodevboard-stage --location uksouth --enable-rbac-authorization true`.
5. Grant operator **Key Vault Secrets Officer** on the vault; restore both secrets.
6. Re-grant `id-solodevboard-cd` **Contributor** and **User Access Administrator** on `rg-solodevboard-stage` (assignments are lost when the old group is deleted). See [deployment.md](../docs/deployment.md#permissions-after-app-rg-recreate).
7. Set staging `CUSTOM_DOMAIN_CERTIFICATE_NAME` to `-` for the first deploy after ACA is recreated; restore after managed certificate exists.
8. Run **CD - Deploy to Azure** (`workflow_dispatch` or merge to `main`).
9. Smoke-test hosted sign-in when CD succeeds.

## Phase 2 — Production

Repeat Phase 1 for `rg-solodevboard-prod` and vault `auth-secrets`. Deploy production via push of a `v*` release tag.

## Partner / programme subscription — Azure SQL (historical)

> **Note:** Current AppHost CD does not provision Azure SQL ([ADR-0046](../adr/adr-0046-overnight-inventory-functions-and-sql.md) withdrawn). Keep this section for reference if a future ADR reintroduces an application database.

On some benefit-style subscriptions, **new Azure SQL logical servers may be blocked in UK South and UK West** (`RegionDoesNotAllowProvisioning` / `ProvisioningDisabled`). Other regions (for example **Sweden Central** or **Germany West Central**) may still accept servers in the same subscription.

When SQL was modelled in AppHost, CD could fail on `provision-sql-staging` even when the app resource group and Key Vault were correct in UK South. Open an Azure support request with issue type **Service and subscription limits** (SQL Database / region access), or plan a separate SQL region strategy (future AppHost work).

## Cleanup

- Delete empty `rg-aspire-solodevboardapphost-*` resource groups.
- Remove local `rm /tmp/solodevboard-kv-migration-*.env` backup files when finished.

## Shared platform (unchanged)

| Resource | Region | Role |
| --- | --- | --- |
| `rg-platform-shared` | UK West | CD identity, shared ACR |
| `acrsolodevboard` | UK West | Image push (CD) and pull (ACA) |
| `rg-solodevboard-dev` | UK South (typical) | Local Path B dev vault only |