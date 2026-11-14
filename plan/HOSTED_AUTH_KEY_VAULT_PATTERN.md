# Hosted Authentication Key Vault Pattern

Date: 2026-07-24 (updated 2026-11-14).  
Applies to: [ADR-0043](../adr/adr-0043-key-vault-backed-hosted-auth-secrets.md), [ADR-0045](../adr/adr-0045-persistent-key-vault-dual-path-local-secrets.md).

## Purpose

Define how GitHub integration secrets reach `SoloDevBoard.App` via Azure Key Vault for hosted tiers and optional local fallback, while preserving **Path A** local parameters for solo developers.

## Scope

| Material | Deploy (`aspire deploy`) | Local Path A | Local Path B |
|---|---|---|---|
| `gh-app-client-secret` | Key Vault `gh-app-client-secret` (`PublishAsExisting`) | `aspire secret set` / dashboard | Dev vault `GetSecret` |
| `gh-pat` | Key Vault `gh-pat` | `aspire secret set` | Dev vault `GetSecret` |
| Non-secret auth parameters | Container App env vars | AppHost parameters | AppHost parameters |

## Pattern

1. **Deploy:** `PublishAsExisting` on the tier vault; `GetSecret` maps to `GitHubAuth__*`; no `AddSecret` and no secret `Parameters__*` via CI after migration.
2. **Local Path A:** Direct `WithEnvironment` from secret parameters (default for forks).
3. **Local Path B:** When a secret is unset locally and vault name plus resource group are active, `AsExisting` + `GetSecret` + developer `DefaultAzureCredential`.
4. **Local overrides Key Vault** when both parameter and vault are configured.
5. **Application code unchanged** — configuration keys and validation only gain dual-path error text.

## Operator flow (deploy)

```mermaid
flowchart LR
    A[Secrets in tier Key Vault] --> B[AppHost PublishAsExisting]
    B --> C[GetSecret refs]
    C --> D[Container App secretRef]
    D --> E[GitHubAuth__* env vars]
    E --> F[SoloDevBoard.App]
```

Populate vaults before CD; see [docs/azure-key-vault-setup.md](../docs/azure-key-vault-setup.md).

## References

- [AppHost](../src/SoloDevBoard.AppHost/AppHost.cs)
- [Secret inventory](KEY_VAULT_SECRET_INVENTORY.md)
- [Deployment guide](../docs/deployment.md)
