# ADR-0029: Cross-Repo Planning board selection and local settings

**Date:** 2026-08-18
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-029 during the repository workflow alignment chore (2026-11-14).

## Decision

Cross-Repo Planning ([#272](https://github.com/markheydon/solo-dev-board/issues/272)) uses a **user-selected Projects v2 board** as the planning board (discovered the same way as Triage/Board Rules). Do not hard-code SoloDevBoard Roadmap (Project #8) field ids in application code. PM settings (selected board node id, excluded `owner/name` repositories, capacity limit, stall and neglect day thresholds) persist in **browser localStorage**, following the theme-preference pattern, because the solution has no application database. Board-state counts include all cards on the selected board; Daily Focus recommendations, Backlog Review, Planning candidates, and per-repo summaries honour exclusions. Hosted GitHub App sign-in still cannot read some private user-owned boards ([#293](https://github.com/markheydon/solo-dev-board/issues/293)); reuse the existing inaccessible-board warning. Reject storing PATs in localStorage, treating Project #8 as the only supported board, or adding EF Core solely for these preferences.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
