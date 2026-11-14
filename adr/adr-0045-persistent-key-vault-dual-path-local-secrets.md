# ADR-0045: Persistent Key Vault with dual-path local secrets

**Date:** 2026-11-14
**Status:** Accepted

## Context

Hosted tiers already store GitHub integration secrets in Azure Key Vault at deploy time ([ADR-0043](adr-0043-key-vault-backed-hosted-auth-secrets.md)), but local development relied on per-machine Aspire user secrets, which does not scale across devices, Dev Containers, and Codespaces. Solo developers and forks must continue to run locally with a single `aspire secret set` and no Azure subscription.

## Decision

- **Local run (`aspire start`):** Bind `gh-pat` and `gh-app-client-secret` from Aspire parameters when set (Path A). When a secret is not set locally and `auth-secrets-vault-name` plus resource group are configured, bind that secret from an **existing** dev Key Vault via AppHost `AsExisting` and `GetSecret` (Path B). Local parameter values override Key Vault when both are present.
- **Deploy (`aspire deploy`):** Always reference **persistent** tier vaults with `PublishAsExisting` and `GetSecret` only. Secrets are maintained in Key Vault out-of-band; deploy does not use `AddSecret` or pass `Parameters__gh_pat` / `Parameters__gh_app_client_secret` through CI after migration.
- **Application:** Continues to read `GitHubAuth__*` from configuration; no `SecretClient` in the web app unless a future issue requires it.

## Consequences

- Multi-device maintainers can omit local secrets and use a shared dev vault (`kv-solodevboard-dev` or operator-chosen name).
- Operators must populate staging and production vaults before CD; GitHub Environment secrets for `GH_PAT` / `GH_APP_CLIENT_SECRET` are no longer required for routine deploys.
- [ADR-0043](adr-0043-key-vault-backed-hosted-auth-secrets.md) deploy-time `AddSecret` behaviour is superseded for new deployments; local parameter binding from ADR-0043 remains Path A.

## References

- [plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md](../plan/HOSTED_AUTH_KEY_VAULT_PATTERN.md)
- [src/SoloDevBoard.AppHost/AppHost.cs](../src/SoloDevBoard.AppHost/AppHost.cs)