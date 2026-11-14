# ADR-0026: Project #8 archive rule via Roadmap Sync

**Date:** 2026-08-18
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-026 during the repository workflow alignment chore (2026-11-14).

## Decision

Closed, non-duplicate issues on Project #8 are archived by the Roadmap Sync bridge **14 calendar days after `closed_at`**, using `archiveProjectV2Item`. GitHub **Auto-archive items** stays off because its `updated:` filter tracks project-card activity, not issue closure, and bulk field writes reset that clock. Reopened issues are unarchived; duplicates are removed from the project, not archived. Eligible cards are archived without a preceding field-sync pass to limit API churn on catch-up runs. Reject enabling GitHub Auto-archive alongside Roadmap Sync or archiving by card `updated` timestamp.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
