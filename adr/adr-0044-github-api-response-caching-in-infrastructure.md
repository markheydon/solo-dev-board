# ADR-0044: GitHub API response caching in Infrastructure

**Date:** 2026-07-25
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-018 during the repository workflow alignment chore (2026-11-14).

## Decision

Cache read-heavy GitHub catalogue responses (repositories, labels, milestones) in Infrastructure using scoped `IMemoryCache` keys tied to the current user's owner login. Invalidate label and milestone catalogue entries on corresponding CRUD mutations. Reject Application-layer DTO caching, distributed cache for this tranche, and caching issues, pull requests, workflow runs, or GraphQL project board queries until a follow-up performance pass.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
