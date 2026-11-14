# ADR-0039: MudBlazor visual language for page chrome

**Date:** 2026-09-09
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-039 during the repository workflow alignment chore (2026-11-14).

## Decision

Shipped pages must share one visual language: page header plus purpose line, toolbar for commands, `MudPaper`/`MudCard` sections at elevation 1, `MudSkeleton` for content-shaped loading, `MudAlert` for empty and error, snackbars for transient outcomes (DEC-035). Agents choose components via the `mudblazor` skill (baseline version recorded only in that skill). Do not use `MudChat`, FABs, carousels, or charts on operational pages. Custom CSS remains exceptional. Product logo and brand assets stay a separate ice-box feature ([#397](https://github.com/markheydon/solo-dev-board/issues/397)). The same chrome must remain usable in mobile browsers (phone and compact tablet such as iPad mini); that is a responsive-web pass ([#411](https://github.com/markheydon/solo-dev-board/issues/411)), not a native app.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
