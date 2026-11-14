# ADR-0036: One-Click Migration label overwrite keeps `area/*` by default

**Date:** 2026-08-26
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-036 during the repository workflow alignment chore (2026-11-14).

## Decision

One-Click Migration **Overwrite** for labels must honour the same hard-coded `area/` keep rule as Label Manager extra deletes. Default **Keep `area/*` labels** on so target `area/*` names are excluded from overwrite deletes and listed as kept, not pending deletes. When the nested control is unchecked, those names are ordinary target-only deletes. Skip and Merge stay unchanged. Milestones and Projects v2 Status columns are out of scope. Reuse `LabelTaxonomyPrefixes` rather than duplicating prefix logic. Reject app-settings keep/ignore lists for this slice.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
