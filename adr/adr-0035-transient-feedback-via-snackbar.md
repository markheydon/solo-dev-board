# ADR-0035: Transient feedback via snackbar

**Date:** 2026-08-25
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-035 during the repository workflow alignment chore (2026-11-14).

## Decision

SoloDevBoard uses **MudBlazor snackbars** for brief, non-blocking operation outcomes (success, warning, error, validation) and **inline `MudAlert` / status regions** for persistent page-local state (load errors with retry, empty states, setup guidance, preview panels, multi-item batch results). Do not show the same message in both channels. Do not use snackbars for in-progress feedback when inline progress indicators already communicate work in flight. Error snackbars use a six-second visibility duration; other severities use five seconds. Operation-complete feedback on long pages must not require scrolling on a typical viewport; prefer snackbars for those outcomes. Global defaults remain in `Program.cs` (bottom-right, outlined). Reject bottom-of-page inline alerts for transient CRUD or apply summaries when a snackbar suffices.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
