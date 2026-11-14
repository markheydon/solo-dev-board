# ADR-0037: One-Click Migration ignores source `area/*` labels by default

**Date:** 2026-08-30
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-037 during the repository workflow alignment chore (2026-11-14).

## Decision

When **Labels** is in scope, One-Click Migration must ignore source `area/*` labels by default for Skip, Merge, and Overwrite. Nested **Ignore `area/*` labels** (default on) excludes those names from create and update; preview summarises them as ignored. Unchecking restores ordinary copy behaviour. **Keep `area/*` labels** remains independent (Overwrite deletes only). Do not special-case the source repository name. Reuse `LabelTaxonomyPrefixes`. Reject app-settings ignore lists for this slice.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
