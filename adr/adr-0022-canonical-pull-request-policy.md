# ADR-0022: Canonical pull request policy

**Date:** 2026-08-16
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-022 during the repository workflow alignment chore (2026-11-14).

## Decision

Pull request title, body, labels, linking, draft state, assignee, milestone, and branch conventions are defined in [`plan/PULL_REQUEST_POLICY.md`](PULL_REQUEST_POLICY.md). That file is the single source of truth for humans and AI agents. Titles use `[<Type>] <Imperative summary> (#N)` (not Conventional Commits). Bodies must keep the GitHub PR template headings. PRs require `type/` and `priority/` labels (plus `status/in-review` while open) and must not be added as standalone Project #8 cards. Ready-for-review is the default when Verify gates pass; vendor draft defaults must be overridden. Reject ad-hoc agent title styles, template-free bodies, and unlabelled agent PRs.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
