---
name: Review Agent
description: Creates PR, validates quality gates, verifies documentation sync, runs tests, and closes issues after approval. Ensures release readiness before marking work complete.
model: Claude Haiku 4.5 (copilot)
argument-hint: Specify 'review issue #X' or 'create PR and close issue #Y'
---

# Review (Copilot adapter)

Before acting, read and follow the canonical agent definition in [`.agents/agents/review.md`](../../.agents/agents/review.md) in full.

Apply all project standards from [`AGENTS.md`](../../AGENTS.md).
