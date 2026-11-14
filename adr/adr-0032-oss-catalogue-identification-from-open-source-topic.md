# ADR-0032: OSS catalogue identification from the GitHub `open-source` topic

**Date:** 2026-08-23
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-032 during the repository workflow alignment chore (2026-11-14).

## Decision

A catalogue repository is open source when its GitHub `topics` include the slug `open-source` (GitHub stores topics in lowercase; matching is case-insensitive). Public visibility is not sufficient. The Repositories page exposes this rule as built-in exclusive filters **All** (default), **Open source**, and **Not open source**, computing the complement from the already-loaded catalogue rather than GitHub Search. Application code owns one classifier for this rule so overnight Community Standards / FUNDING scans and later repository groups reuse it. Reject treating the `oss` topic as equivalent, using Search as the primary catalogue path, persisting a SoloDevBoard-only OSS flag, or blocking this increment on group CRUD (#381).

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
