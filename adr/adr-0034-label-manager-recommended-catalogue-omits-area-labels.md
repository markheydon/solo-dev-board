# ADR-0034: Label Manager recommended catalogue omits `area/*` labels

**Date:** 2026-08-23
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-034 during the repository workflow alignment chore (2026-11-14).

## Decision

Label Manager's built-in recommended catalogue is the **portable** workflow set (`type/`, `priority/`, `status/`, `size/`). `area/*` names describe SoloDevBoard's own feature map and must not be created on other repositories by Recommended taxonomy or Synchronise. When **Remove labels outside taxonomy** is on, a nested **Keep `area/*` labels** control (default on) excludes those names from extra deletes; unchecking it treats them as genuine extras. Hard-code the `area/` prefix for `v1.1`; do not add app-settings ignore lists yet. Bulk delete on the Labels tab (#444) is a separate confirmation loop and does not add a protected allow-list. Reject exporting this repository's area map as product-default labels.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
