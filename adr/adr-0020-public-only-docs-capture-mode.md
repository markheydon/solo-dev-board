# ADR-0020: Public-only docs capture mode for documentation screenshots

**Date:** 2026-07-26
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-020 during the repository workflow alignment chore (2026-11-14).

## Decision

Local documentation screenshot capture uses a `DocsCapture:Enabled` flag that restricts repository and Projects v2 catalogues to public GitHub content only. Filtering is applied at the Infrastructure catalogue chokepoints (`GitHubService` repository lists and project board discovery/definition). The mode defaults to disabled, is enabled only via local user secrets or `DocsCapture__Enabled`, and is intentionally not an Aspire AppHost deploy parameter. This is screenshot hygiene for published user-guide images, not a security boundary, and does not block write operations. Reject treating the flag as access control or exposing it as a hosted deployment switch.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
