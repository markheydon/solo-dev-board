# ADR-0043: Key Vault-backed hosted auth secrets

**Date:** 2026-07-24
**Status:** Active (deploy mechanics amended by [ADR-0045](adr-0045-persistent-key-vault-dual-path-local-secrets.md))

## Context

This record was migrated from legacy decision log entry DEC-017 during the repository workflow alignment chore (2026-11-14).

## Decision

Hosted authentication secret parameters (`gh-pat`, `gh-app-client-secret`) are read from Azure Key Vault and injected into Azure Container Apps as Key Vault references. Deploy uses persistent vaults (`PublishAsExisting`); secrets are not written from CI parameters. Local development continues to support Aspire user secrets (Path A) and optional dev vault fallback (Path B). Non-secret hosted auth configuration remains plain environment variables. Reject storing hosted auth secrets in container images, committed files, or plain-text Container App settings in production.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
