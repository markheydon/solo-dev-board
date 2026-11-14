# ADR-0031: GitHub comment workflow invocation

**Date:** 2026-08-20
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-031 during the repository workflow alignment chore (2026-11-14).

## Decision

Cursor Cloud Agents started from a GitHub Issue or Pull Request comment (`@cursor` or `@cursoragent` at the start of the comment) must bind "this issue" / "this PR" to that thread and route human-friendly phrasing through the workflow Invocation tables. Informal implement / fix / do / work on **this issue** on an Issue comment is **`deliver-issue`** (implement, verify, and open a PR). Chat `/implement-issue` remains the split path that does not open a PR. Reject treating GitHub "implement this issue" as implement-only, asking for an issue number the thread already implies, or using bare "review this issue" as code review.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
