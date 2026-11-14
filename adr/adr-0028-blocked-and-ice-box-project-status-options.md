# ADR-0028: Blocked and Ice Box project Status options

**Date:** 2026-08-18
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-028 during the repository workflow alignment chore (2026-11-14).

## Decision

Project #8 **Status** includes **Blocked** (`9796fb74`) and **Ice Box** (`1c235cb1`) alongside Todo, Up Next, In Progress, and Done. **Up Next** remains the only project-only status (no issue label). **Blocked** maps to `status/blocked` (external dependency); **Ice Box** maps to `status/ice-box` (shelved, not in the active queue). Roadmap Sync sets board Status from these labels and clears Focus Order when parking. Ice Box clears Start/Target dates; Blocked preserves dates if work had started. Parent Feature/Epic roll-up: In Progress beats Blocked; Blocked beats Ice Box; all open children Ice Box → parent Ice Box. Roadmap view filter includes Todo, Up Next, In Progress, Blocked, and Done; excludes Ice Box. Reject adding Up Next as an issue label or leaving blocked/ice-box issues on Todo because sync does not infer parked states without labels.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
