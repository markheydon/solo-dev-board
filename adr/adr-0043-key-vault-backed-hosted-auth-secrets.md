# ADR-0043: Key Vault-backed hosted auth secrets

**Date:** 2026-07-24
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-017 during the repository workflow alignment chore (2026-11-14).

## Decision

Hosted authentication secret parameters (`gh-pat`, `gh-app-client-secret`) are persisted in an Aspire-provisioned Azure Key Vault at deploy time and injected into Azure Container Apps as Key Vault references. Local development continues to bind secret parameters directly from Aspire user secrets. Non-secret hosted auth configuration remains plain environment variables. Reject storing hosted auth secrets in container images, committed files, or plain-text Container App settings in production.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
