# ADR-0031: GitHub comment workflow invocation

**Date:** 2026-08-20
**Status:** Active (amended 2026-11-14)

## Context

This record was migrated from legacy decision log entry DEC-031 during the repository workflow alignment chore (2026-11-14).

Cursor Cloud Agents can start from a GitHub Issue or Pull Request comment when the comment begins with `@cursor` or `@cursoragent`. Copilot-era slash workflows (`deliver-issue`, `implement-issue`, `verify-and-create-pr`, and related commands) were retired on 2026-11-14 in favour of slim `AGENTS.md`, repository skills, and [`.cursor/commands/loop-code-review.md`](../.cursor/commands/loop-code-review.md). Historical routing tables live in [`plan/archive/PM_RUNBOOK-2026-copilot-era.md`](../plan/archive/PM_RUNBOOK-2026-copilot-era.md).

## Decision

Agents started from a GitHub Issue or Pull Request comment must bind **this issue** or **this PR** to that thread without asking for an issue number the thread already implies.

- **Implementation work** on an issue: follow [`AGENTS.md`](../AGENTS.md), [`CONVENTIONS.md`](../CONVENTIONS.md), and the `implementation-guardrails` skill; open pull requests per [`plan/PULL_REQUEST_POLICY.md`](../plan/PULL_REQUEST_POLICY.md).
- **Pull request review** on a PR: use the `code-review` skill or [`loop-code-review`](../.cursor/commands/loop-code-review.md); reject treating bare "review this issue" on an issue thread as substitute PR code review when a linked PR exists.
- **GitHub graph operations** (sub-issues, blocking): use GitHub MCP or `gh api` as described in [`plan/PROJECT_MANAGEMENT.md`](../plan/PROJECT_MANAGEMENT.md).

Reject reintroducing retired slash-command orchestration as the canonical delivery path.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.